using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Приглушает ВСЕ звуки сцены (эмбиент локации, разовые триггеры, музыку),
/// пока хоррор-ивент со щупальцем активен: щупальце вылезло — локация затихает,
/// спряталось — звуки плавно возвращаются.
///
/// Не приглушаются:
///  - звуки игрока (шаги, инструменты) — флаг Exclude Player;
///  - звук самого щупальца (ивент передаёт свою иерархию в Activate());
///  - источники из списка Exclusions (интерфейс и т.п.).
/// Музыка приглушается через MusicManager.SetDuck() (флаг Duck Music).
///
/// Вешается на отдельный объект (обычно ребёнок POINT_HORROR). Включается кодом:
/// HorrorTentacleEvent вызывает Activate() при вылезании щупальца и
/// Deactivate(), когда оно спряталось.
/// </summary>
[DisallowMultipleComponent]
public sealed class HorrorSoundDucker : MonoBehaviour
{
    [Header("Приглушение")]
    [Tooltip("До какой громкости приглушать звуки локации (доля от исходной громкости каждого источника). 0 — полная тишина.")]
    [Range(0f, 1f)] [SerializeField] private float duckedVolume = 0.1f;

    [Tooltip("Плавность приглушения и возврата, сек.")]
    [Min(0.05f)] [SerializeField] private float fadeTime = 0.8f;

    [Header("Что приглушать")]
    [Tooltip("Приглушать и музыку (MusicManager через SetDuck).")]
    [SerializeField] private bool duckMusic = true;

    [Tooltip("Не приглушать звуки игрока (шаги, инструменты).")]
    [SerializeField] private bool excludePlayer = true;

    [Tooltip("AudioSource, которые не приглушать (интерфейс, свои эффекты и т.п.).")]
    [SerializeField] private List<AudioSource> exclusions = new();

    private readonly Dictionary<AudioSource, float> originalVolumes = new();
    private Transform playerRoot;
    private Transform ownHierarchy;
    private Coroutine duckRoutine;

    /// <summary>Начать приглушение (щупальце вылезло). ownHierarchy — иерархия щупальца, её звук не глушим.</summary>
    public void Activate(Transform ownHierarchyToExclude = null)
    {
        ownHierarchy = ownHierarchyToExclude;
        if (duckRoutine != null)
            StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckLoop());

        if (duckMusic && MusicManager.Instance != null)
            MusicManager.Instance.SetDuck(duckedVolume);
    }

    /// <summary>Вернуть все звуки (щупальце спряталось).</summary>
    public void Deactivate()
    {
        if (duckRoutine != null)
        {
            StopCoroutine(duckRoutine);
            duckRoutine = null;
        }
        duckRoutine = StartCoroutine(RestoreLoop());

        if (duckMusic && MusicManager.Instance != null)
            MusicManager.Instance.SetDuck(1f);
    }

    private IEnumerator DuckLoop()
    {
        float scanTimer = 0f;
        while (true)
        {
            // Периодически досматриваем сцену: новые источники (разовые триггеры и т.п.)
            // тоже приглушаются, пока ивент активен.
            if (scanTimer <= 0f)
            {
                CollectSources();
                scanTimer = 0.5f;
            }
            scanTimer -= Time.deltaTime;

            foreach (KeyValuePair<AudioSource, float> pair in originalVolumes)
            {
                AudioSource src = pair.Key;
                if (src == null)
                    continue;
                float target = pair.Value * duckedVolume;
                float step = Mathf.Abs(pair.Value - target) / fadeTime * Time.deltaTime;
                src.volume = Mathf.MoveTowards(src.volume, target, step);
            }
            yield return null;
        }
    }

    private IEnumerator RestoreLoop()
    {
        // Снимок текущих (приглушенных) громкостей → плавно к оригиналам.
        var snapshot = new Dictionary<AudioSource, float>();
        foreach (KeyValuePair<AudioSource, float> pair in originalVolumes)
            if (pair.Key != null)
                snapshot[pair.Key] = pair.Key.volume;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            foreach (KeyValuePair<AudioSource, float> pair in snapshot)
            {
                if (pair.Key == null)
                    continue;
                pair.Key.volume = Mathf.Lerp(pair.Value, originalVolumes[pair.Key], k);
            }
            yield return null;
        }

        foreach (KeyValuePair<AudioSource, float> pair in originalVolumes)
            if (pair.Key != null)
                pair.Key.volume = pair.Value;

        originalVolumes.Clear();
        duckRoutine = null;
    }

    private void CollectSources()
    {
        if (playerRoot == null && excludePlayer)
        {
            PlayerMovements pm = FindFirstObjectByType<PlayerMovements>();
            if (pm != null)
                playerRoot = pm.transform;
        }

        MusicManager music = MusicManager.Instance;

        foreach (AudioSource src in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (src == null || originalVolumes.ContainsKey(src))
                continue;
            if (exclusions.Contains(src))
                continue;
            if (ownHierarchy != null && src.transform.IsChildOf(ownHierarchy))
                continue;
            if (src.transform.IsChildOf(transform))
                continue;
            if (excludePlayer && playerRoot != null && src.transform.IsChildOf(playerRoot))
                continue;
            // Музыка управляется отдельно через MusicManager.SetDuck — свои источники не трогаем.
            if (music != null && src.transform.IsChildOf(music.transform))
                continue;

            originalVolumes[src] = src.volume;
        }
    }
}
