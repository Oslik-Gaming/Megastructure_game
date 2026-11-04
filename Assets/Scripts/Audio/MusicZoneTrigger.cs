using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Зона-триггер для ГЛАВНОЙ музыки: при входе игрока просит MusicManager переключить трек,
/// при выходе (опционально) возвращает основной трек. Так делается музыка "по локациям":
/// у каждого помещения своя зона со своим именем трека.
///
/// Как пользоваться:
///  1. На объекте MusicManager должен быть MusicManager со списком Tracks
///     (имена треков — то, что ты пишешь в поле Track Name здесь; есть выпадающий список).
///  2. Повесь скрипт на Empty в локации (или ПКМ в иерархии → "Звук" → "Music Zone Trigger") —
///     коллайдер-триггер настроится сам через Reset().
///  3. Укажи Track Name (например "Horror"). Фейд на вход и на выход настраивается ОТДЕЛЬНО:
///     - "From Manager" — взять Default Fade Time из MusicManager (плавность задаётся в одном месте);
///     - "Instant" — сменить трек мгновенно, без фейда (для скримеров/резких переходов);
///     - "Custom" — своё значение в секундах только для этой зоны.
///
/// Поведение:
///  - Если игрок входит в другую зону — играет её трек (последняя вошедшая зона выигрывает,
///    даже если старая зона вышла в том же кадре).
///  - При выходе: Restore Default On Exit вернёт первый трек из списка, а если задан
///    Exit Track Name — вместо дефолта заиграет он (например "Calm" после "Horror").
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class MusicZoneTrigger : MonoBehaviour
{
    public enum ZoneFadeMode
    {
        FromManager, // взять Default Fade Time из MusicManager
        Instant,     // сменить трек мгновенно, без фейда
        Custom       // своё значение в секундах
    }

    public enum SameTrackPolicy
    {
        KeepPlaying,          // трек уже играет — продолжаем, не обрываем (по умолчанию)
        RestartFromBeginning  // перезапустить трек с начала
    }

    [Header("Трек")]
    [Tooltip("Имя трека из списка Tracks у MusicManager, который включить при входе в зону.")]
    [SerializeField] private string trackName;

    [Header("Переход при входе в зону")]
    [Tooltip("Как переключать музыку, когда игрок вошёл в зону.")]
    [SerializeField] private ZoneFadeMode enterFadeMode = ZoneFadeMode.FromManager;

    [FormerlySerializedAs("fadeTime")]
    [Tooltip("Длительность фейда при входе (сек). Используется только при режиме Custom.")]
    [Min(0.01f)] [SerializeField] private float enterFadeTime = 2f;

    [Header("При выходе из зоны")]
    [Tooltip("При выходе игрока из зоны вернуть основной (первый) трек из списка MusicManager.")]
    [SerializeField] private bool restoreDefaultOnExit = true;

    [Tooltip("Если задано — при выходе играть этот трек (вместо дефолтного). Например 'Calm' после хоррор-локи.")]
    [SerializeField] private string exitTrackName;

    [Tooltip("Как переключать музыку, когда игрок вышел из зоны.")]
    [SerializeField] private ZoneFadeMode exitFadeMode = ZoneFadeMode.FromManager;

    [Tooltip("Длительность фейда при выходе (сек). Используется только при режиме Custom.")]
    [Min(0.01f)] [SerializeField] private float exitFadeTime = 2f;

    [Header("Если трек уже играет")]
    [Tooltip("Когда запрошенный трек (на входе или на выходе) уже играет: продолжать его без изменений или перезапустить с начала.")]
    [SerializeField] private SameTrackPolicy ifAlreadyPlaying = SameTrackPolicy.KeepPlaying;

    [Header("Правила срабатывания")]
    [Tooltip("Сработать только один раз за игровую сессию (для зон-событий; для обычных локаций выключи).")]
    [SerializeField] private bool triggerOnce = false;

    [Tooltip("После респавна игрока снова разрешить срабатывание (и переприменить трек, если игрок в зоне).")]
    [SerializeField] private bool rearmOnRespawn = true;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = false;

    /// <summary>Срабатывала ли зона (с учётом реарм-ов).</summary>
    public bool HasTriggered { get; private set; }

    /// <summary>Игрок сейчас внутри зоны (в play mode).</summary>
    public bool PlayerInside { get; private set; }

    private BoxCollider col;

    private void Reset()
    {
        BoxCollider c = GetComponent<BoxCollider>();
        c.isTrigger = true;
        if (c.size == Vector3.one) // дефолтный размер — ставим разумный для зоны-локации
            c.size = new Vector3(8f, 4f, 8f);
    }

    private void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        CheckpointManager.PlayerRespawned += OnPlayerRespawned;
    }

    private void OnDisable()
    {
        CheckpointManager.PlayerRespawned -= OnPlayerRespawned;
    }

    private void OnPlayerRespawned()
    {
        if (!rearmOnRespawn) return;

        HasTriggered = false;
        if (triggerOnce && col != null) col.enabled = true;

        // Если после респавна игрок оказался внутри этой зоны — переприменяем её трек,
        // т.к. другая зона могла перебить музыку перед смертью.
        PlayerInside = IsPlayerInside();
        if (PlayerInside)
        {
            ApplyTrack("респавн внутри зоны");
        }
        else if (debugLog)
        {
            Debug.Log($"[MusicZoneTrigger] '{name}' перевзведён после респавна (игрок вне зоны).", this);
        }
    }

    private bool IsPlayerInside()
    {
        if (col == null) return false;
        Collider[] hits = Physics.OverlapBox(
            transform.TransformPoint(col.center),
            Vector3.Scale(col.size, transform.lossyScale) * 0.5f,
            transform.rotation);
        foreach (Collider hit in hits)
        {
            if (CheckpointManager.IsPlayerCollider(hit)) return true;
        }
        return false;
    }

    /// <summary>Режим зоны → значение для MusicManager (0 = из менеджера, -1 = мгновенно, >0 = своё).</summary>
    private float ResolveFade(ZoneFadeMode mode, float customTime)
    {
        switch (mode)
        {
            case ZoneFadeMode.Instant: return -1f;
            case ZoneFadeMode.Custom: return Mathf.Max(0.01f, customTime);
            default: return 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CheckpointManager.IsPlayerCollider(other)) return;
        if (triggerOnce && HasTriggered) return;

        PlayerInside = true;
        HasTriggered = true;
        if (triggerOnce && col != null) col.enabled = false;

        ApplyTrack("вход в зону");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!CheckpointManager.IsPlayerCollider(other)) return;
        PlayerInside = false;

        if (MusicManager.Instance == null) return;

        float exitFade = ResolveFade(exitFadeMode, exitFadeTime);

        bool restart = ifAlreadyPlaying == SameTrackPolicy.RestartFromBeginning;

        if (!string.IsNullOrEmpty(exitTrackName))
        {
            // Свой трек на выход (тоже с "владельцем", чтобы не перебить соседнюю зону)
            MusicManager.Instance.PlayTrack(exitTrackName, exitFade, this, restart);
            if (debugLog) Debug.Log($"[MusicZoneTrigger] '{name}' выход из зоны → трек '{exitTrackName}'.", this);
        }
        else if (restoreDefaultOnExit)
        {
            MusicManager.Instance.PlayDefault(exitFade, this, restart);
            if (debugLog) Debug.Log($"[MusicZoneTrigger] '{name}' выход из зоны → основной трек.", this);
        }
    }

    private void ApplyTrack(string reason)
    {
        if (MusicManager.Instance == null)
        {
            Debug.LogWarning($"[MusicZoneTrigger] '{name}' в сцене нет MusicManager (объект MusicManager).", this);
            return;
        }
        MusicManager.Instance.PlayTrack(trackName, ResolveFade(enterFadeMode, enterFadeTime), this,
            ifAlreadyPlaying == SameTrackPolicy.RestartFromBeginning);
        if (debugLog) Debug.Log($"[MusicZoneTrigger] '{name}' {reason} → трек '{trackName}'.", this);
    }

    private void OnDrawGizmos()
    {
        BoxCollider c = col != null ? col : GetComponent<BoxCollider>();
        if (c != null)
        {
            Gizmos.color = HasTriggered ? new Color(0.5f, 0.5f, 0.5f, 0.4f) : new Color(0.8f, 0.3f, 1f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(c.center, c.size);
        }
    }
}
