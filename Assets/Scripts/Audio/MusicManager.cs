using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер главной игровой музыки. Живёт на объекте MusicManager в сцене
/// (рядом с дочерним MainSource). Единственная точка управления фоновым треком:
/// зоны MusicZoneTrigger (или любой другой код) просят переключить трек —
/// менеджер плавно кроссфейдит между ними.
///
/// Как пользоваться:
///  1. Повесь скрипт на объект MusicManager (тот самый Empty с MainSource).
///  2. В списке Tracks задай треки: имя (для зон и отладки), клип, громкость.
///     Первый трек — Default Track, он играет со старта сцены.
///  3. Второй AudioSource (для кроссфейда) создаётся/находится сам как ребёнок
///     "FadeSource"; его настройки копируются с MainSource.
///  4. Из кода: MusicManager.Instance.PlayTrack("Horror"), MusicManager.Instance.PlayDefault().
///
/// Важные детали:
///  - fadeSeconds: 0 = "взять Default Fade Time", отрицательное = мгновенно без фейда, >0 = своя длительность.
///  - Зоны передают себя как "владельца" музыки: если игрок зашёл в новую зону, а потом
///    в том же кадре вышла старая — старая НЕ вернёт дефолт и не перобьёт новую.
///  - Фейд идёт по unscaled-времени, поэтому не замирает на паузах (Time.timeScale = 0).
///
/// Скрипт не перетирает настройки MainSource — он лишь запускает/глушит проигрывание.
/// </summary>
public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicTrack
    {
        [Tooltip("Имя трека — по нему зоны находят музыку (например \"Main\", \"Horror\").")]
        public string name;
        [Tooltip("Аудиоклип (зацикленная музыка).")]
        public AudioClip clip;
        [Tooltip("Громкость этого трека (0..1). Итог = Track Volume * Master Volume.")]
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Треки")]
    [Tooltip("Список треков. Первый считается основным (Default) и играет со старта сцены.")]
    [SerializeField] private List<MusicTrack> tracks = new List<MusicTrack>();

    [Header("Основной источник")]
    [Tooltip("Главный AudioSource. Если пусто — берётся у детей (MainSource).")]
    [SerializeField] private AudioSource mainSource;

    [Header("Переходы")]
    [Tooltip("Длительность кроссфейда между треками по умолчанию (сек). Значение 0 в зонах означает «взять это».")]
    [Min(0.01f)] [SerializeField] private float defaultFadeTime = 2f;

    [Tooltip("Общая громкость музыки (удобно для настроек звука).")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = false;

    /// <summary>Глобальный доступ для триггеров и кода.</summary>
    public static MusicManager Instance { get; private set; }

    /// <summary>Трек, который должен играть по умолчанию (первый в списке).</summary>
    public MusicTrack DefaultTrack => tracks.Count > 0 ? tracks[0] : null;

    /// <summary>Трек, играющий прямо сейчас.</summary>
    public MusicTrack CurrentTrack { get; private set; }

    /// <summary>Имена всех треков — для выпадающего списка в инспекторе зон.</summary>
    public List<string> GetTrackNames()
    {
        List<string> names = new List<string>();
        foreach (MusicTrack t in tracks)
        {
            if (t != null) names.Add(string.IsNullOrEmpty(t.name) ? "(без имени)" : t.name);
        }
        return names;
    }

    private AudioSource fadeSource;      // второй источник для кроссфейда
    private Coroutine fadeRoutine;
    private Object currentRequester;     // кто сейчас "держит" музыку (зона/код); null — внешний код

    private void Awake()
    {
        // Простой синглтон на сцену: второй MusicManager в сцене отключаем
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[MusicManager] В сцене уже есть MusicManager — отключаю дубликат на '{name}'.", this);
            enabled = false;
            return;
        }
        Instance = this;

        if (mainSource == null)
        {
            mainSource = GetComponentInChildren<AudioSource>();
            if (mainSource == null)
                Debug.LogWarning("[MusicManager] Не найден AudioSource (MainSource) — назначь вручную.", this);
        }

        SetupSources();

        // Стартуем дефолтный трек сразу и без фейда (музыка должна играть с первого кадра).
        // Гасим возможный автозапуск из инспектора (Play On Awake со старым клипом),
        // чтобы не звучали два клипа одновременно.
        if (mainSource != null && mainSource.isPlaying) mainSource.Stop();

        MusicTrack def = DefaultTrack;
        if (def != null && def.clip != null && mainSource != null)
        {
            mainSource.clip = def.clip;
            mainSource.volume = def.volume * masterVolume;
            mainSource.loop = true;
            mainSource.Play();
            CurrentTrack = def;
            if (debugLog) Debug.Log($"[MusicManager] Стартовый трек: '{def.name}'.", this);
        }
    }

    private void SetupSources()
    {
        if (mainSource == null) return;
        mainSource.loop = true;
        mainSource.playOnAwake = false;

        // Второй источник — для кроссфейда: пока в одном затухаем, во втором нарастаем.
        // Ищем строго по имени "FadeSource", чтобы не схватить случайный AudioSource-ребёнок.
        Transform existing = transform.Find("FadeSource");
        fadeSource = existing != null ? existing.GetComponent<AudioSource>() : null;

        if (fadeSource == null)
        {
            GameObject go = new GameObject("FadeSource");
            go.transform.SetParent(mainSource.transform.parent != null ? mainSource.transform.parent : transform, false);
            fadeSource = go.AddComponent<AudioSource>();
            if (debugLog) Debug.Log("[MusicManager] Создан FadeSource для кроссфейда.", this);
        }

        // Копируем важные настройки звука с MainSource, чтобы кроссфейд звучал одинаково
        fadeSource.playOnAwake = false;
        fadeSource.loop = true;
        fadeSource.spatialBlend = 0f; // музыка всегда 2D
        fadeSource.outputAudioMixerGroup = mainSource.outputAudioMixerGroup;
        fadeSource.pitch = mainSource.pitch;
        fadeSource.priority = mainSource.priority;
        fadeSource.bypassEffects = mainSource.bypassEffects;
        fadeSource.bypassListenerEffects = mainSource.bypassListenerEffects;
        fadeSource.bypassReverbZones = mainSource.bypassReverbZones;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Переключить трек по имени из списка Tracks. Если такой трек уже играет — он просто
    /// продолжает играть (без рестарта и обрыва), если restartIfSame = false.
    /// </summary>
    /// <param name="trackName">Имя трека из списка Tracks.</param>
    /// <param name="fadeSeconds">0 = Default Fade Time из менеджера; меньше 0 = мгновенно; больше 0 = своя длительность (сек).</param>
    /// <param name="requester">Зона/компонент-владелец (для корректной работы пересекающихся зон).</param>
    /// <param name="restartIfSame">Если true — перезапустить трек с начала, даже если он уже играет.</param>
    public void PlayTrack(string trackName, float fadeSeconds = -1f, Object requester = null, bool restartIfSame = false)
    {
        MusicTrack track = FindTrack(trackName);
        if (track == null)
        {
            Debug.LogWarning($"[MusicManager] Трек '{trackName}' не найден в списке Tracks.", this);
            return;
        }
        CrossfadeTo(track, fadeSeconds, requester, restartIfSame);
    }

    /// <summary>Переключить трек по индексу в списке Tracks.</summary>
    public void PlayTrack(int index, float fadeSeconds = -1f, Object requester = null, bool restartIfSame = false)
    {
        if (index < 0 || index >= tracks.Count)
        {
            Debug.LogWarning($"[MusicManager] Индекс трека {index} вне списка Tracks.", this);
            return;
        }
        CrossfadeTo(tracks[index], fadeSeconds, requester, restartIfSame);
    }

    /// <summary>Вернуть основной (первый в списке) трек.</summary>
    /// <param name="requester">Если задан, сработает только если музыка сейчас "держится" этим же владельцем
    /// (или никем) — так выход из старой зоны не перебивает трек только что вошедшей.</param>
    /// <param name="restartIfSame">Если true — перезапустить трек с начала, даже если он уже играет.</param>
    public void PlayDefault(float fadeSeconds = -1f, Object requester = null, bool restartIfSame = false)
    {
        MusicTrack def = DefaultTrack;
        if (def == null) return;

        // Выход из старой зоны не должен перебивать трек новой зоны, вошедшей в том же кадре
        if (requester != null && currentRequester != null && currentRequester != requester)
            return;

        if (requester != null) currentRequester = null;
        CrossfadeTo(def, fadeSeconds, requester, restartIfSame);
    }

    /// <summary>Остановить музыку (с фейдом в тишину).</summary>
    public void Stop(float fadeSeconds = -1f)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine(ResolveFade(fadeSeconds)));
    }

    private MusicTrack FindTrack(string trackName)
    {
        foreach (MusicTrack t in tracks)
        {
            if (t != null && t.name == trackName) return t;
        }
        return null;
    }

    /// <summary>
    /// 0 = «использовать дефолтную длительность»;
    /// отрицательное значение = мгновенно, без фейда;
    /// больше 0 = своя длительность.
    /// </summary>
    private float ResolveFade(float fadeSeconds)
    {
        if (fadeSeconds < 0f) return 0f;           // мгновенно
        if (fadeSeconds == 0f) return defaultFadeTime; // из менеджера
        return fadeSeconds;                        // своё значение
    }

    private void CrossfadeTo(MusicTrack track, float fadeSeconds, Object requester, bool restartIfSame)
    {
        // Тот же трек (по ссылке или по клипу — если в списке дубли с разными именами):
        // не обрываем и не рестартуем — музыка просто продолжает играть.
        bool sameTrack = track == CurrentTrack ||
                         (CurrentTrack != null && CurrentTrack.clip != null && CurrentTrack.clip == track.clip);
        if (sameTrack && !restartIfSame)
        {
            if (requester != null) currentRequester = requester;
            if (debugLog) Debug.Log($"[MusicManager] '{track.name}' уже играет — продолжаем без изменений.", this);
            return;
        }

        if (requester != null) currentRequester = requester;

        float t = ResolveFade(fadeSeconds);
        string fadeInfo = t > 0.0001f ? $"фейд {t:0.##} сек" : "мгновенно";
        if (debugLog) Debug.Log($"[MusicManager] '{(CurrentTrack != null ? CurrentTrack.name : "тишина")}' → '{track.name}' ({fadeInfo}).", this);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(track, t));
    }

    private IEnumerator FadeRoutine(MusicTrack target, float fadeTime)
    {
        AudioSource from = mainSource;
        AudioSource to = fadeSource;
        if (from == null || to == null) yield break;

        float targetVolume = target.volume * masterVolume;
        float fromStartVolume = from.isPlaying ? from.volume : 0f;

        // Новый трек всегда стартует во "втором" источнике с нулевой громкости
        to.volume = 0f;
        to.clip = target.clip;
        to.Play();
        CurrentTrack = target;

        // Мгновенное переключение — без фейда
        if (fadeTime <= 0.0001f)
        {
            from.Stop();
            from.volume = 0f;
            to.volume = targetVolume;
            mainSource = to;
            fadeSource = from;
            fadeRoutine = null;
            yield break;
        }

        if (!from.isPlaying)
        {
            // Из ничего не фейдим — только плавный вход нового трека
            float time = 0f;
            while (time < fadeTime)
            {
                time += Time.unscaledDeltaTime;
                to.volume = targetVolume * Mathf.Clamp01(time / fadeTime);
                yield return null;
            }
        }
        else
        {
            // Классический кроссфейд: громкости, зафиксированные на старте, движутся линейно
            float time = 0f;
            while (time < fadeTime)
            {
                time += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(time / fadeTime);
                from.volume = fromStartVolume * (1f - k);
                to.volume = targetVolume * k;
                yield return null;
            }
            from.Stop();
        }

        from.volume = 0f;
        to.volume = targetVolume;

        // Меняем источники местами: main всегда держит текущий звук
        mainSource = to;
        fadeSource = from;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float fadeTime)
    {
        AudioSource from = mainSource;
        if (from == null || !from.isPlaying) yield break;

        float startVolume = from.volume;
        float time = 0f;
        while (time < fadeTime)
        {
            time += Time.unscaledDeltaTime;
            from.volume = startVolume * (1f - Mathf.Clamp01(time / fadeTime));
            yield return null;
        }
        from.Stop();
        from.volume = 0f;
        CurrentTrack = null;
        currentRequester = null;
        fadeRoutine = null;
    }

    /// <summary>Сменить общую громкость на лету (для настроек звука).</summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (mainSource != null && CurrentTrack != null && mainSource.isPlaying)
            mainSource.volume = CurrentTrack.volume * masterVolume;
    }

    private void OnValidate()
    {
        defaultFadeTime = Mathf.Max(0.01f, defaultFadeTime);
        // Имена треков не должны быть пустыми — зоны ищут по имени
        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] != null && string.IsNullOrEmpty(tracks[i].name))
                tracks[i].name = $"Track {i}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        AudioSource src = mainSource != null ? mainSource : GetComponentInChildren<AudioSource>();
        if (src != null)
        {
            Gizmos.color = new Color(0.8f, 0.3f, 1f, 0.9f);
            Gizmos.DrawSphere(src.transform.position, 0.3f);
        }
    }
}
