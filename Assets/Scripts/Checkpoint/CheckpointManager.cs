using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер чекпоинтов. Один на сцену.
///
/// Обязанности:
///  - при старте сцены восстанавливает сохранённый чекпоинт из GameProgress
///    и мгновенно ставит игрока на него ("продолжить с чекпоинта");
///  - принимает активацию чекпоинтов (CheckPoint.OnTriggerEnter) и пишет её в сейв;
///  - выполняет респавн после смерти (DeathZone / падение за карту):
///    заморозка игрока → затемнение экрана → телепорт → проявление.
/// </summary>
[DisallowMultipleComponent]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    /// <summary>Событие: игрок заспавнился на чекпоинте после смерти (для звуков/событий, которые должны сбрасываться).</summary>
    public static event System.Action PlayerRespawned;

    [Header("Игрок")]
    [Tooltip("Ссылка на игрока. Если пусто — найдётся автоматически по компоненту PlayerMovements.")]
    [SerializeField] private PlayerMovements player;

    [Header("Стартовая точка (если сохранённого чекпоинта нет)")]
    [Tooltip("Куда ставить игрока на старте, если сейва чекпоинта нет. Если пусто — игрок остаётся там, где стоит в сцене.")]
    [SerializeField] private Transform defaultSpawn;
    [Tooltip("Смещение вверх от стартовой точки, чтобы не застрять в полу")]
    [Min(0f)] [SerializeField] private float defaultSpawnHeightOffset = 0.1f;

    [Header("Респавн")]
    [Tooltip("Подъём игрока над точкой спавна при телепорте, чтобы не застрять в земле")]
    [Min(0f)] [SerializeField] private float respawnLift = 0.05f;

    [Header("Страховка от падения за карту")]
    [Tooltip("Считать игрока погибшим, если он упал ниже порога")]
    [SerializeField] private bool enableFallDeath = true;
    [Tooltip("Мировой Y, ниже которого игрок считается погибшим")]
    [SerializeField] private float fallDeathY = -50f;

    [Header("Ссылки")]
    [Tooltip("Затемнитель экрана. Если пусто — найдётся автоматически на сцене.")]
    [SerializeField] private ScreenFader screenFader;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = true;

    private CheckPoint activeCheckpoint;
    private bool respawning;

    /// <summary>Последний активированный чекпоинт (может быть null в начале игры).</summary>
    public CheckPoint ActiveCheckpoint => activeCheckpoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Checkpoints] Найдено несколько CheckpointManager на сцене — лишний отключён.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (screenFader == null) screenFader = FindFirstObjectByType<ScreenFader>();
        if (player == null) player = FindFirstObjectByType<PlayerMovements>();
        if (player == null)
            Debug.LogWarning("[Checkpoints] Игрок (PlayerMovements) не найден на сцене — респавн работать не будет!", this);

        RestoreCheckpointFromSave();
        StartCoroutine(PlacePlayerAtRestorePointNextFrame());
        WarnDuplicateIds();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ================= Восстановление =================

    private IEnumerator PlacePlayerAtRestorePointNextFrame()
    {
        // Даём PlayerHeadLook и другим компонентам завершить Start().
        yield return null;

        if (player == null) yield break;

        if (GameProgress.TryLoadPlayerTransform(
                out Vector3 savedPosition,
                out Quaternion savedRotation,
                out Quaternion savedHeadRotation))
        {
            TeleportPlayer(savedPosition, savedRotation);

            PlayerHeadLook headLook = player.GetComponent<PlayerHeadLook>();
            if (headLook != null)
                headLook.RestoreHeadLocalRotation(savedHeadRotation);

            if (debugLog)
                Debug.Log($"[Checkpoints] Восстановлено сохранённое положение игрока: {savedPosition}.", this);
            yield break;
        }

        PlacePlayerAtRestorePoint();
    }

    private void RestoreCheckpointFromSave()
    {
        int savedId = GameProgress.SavedCheckpointId;
        if (savedId < 0) return; // сейва нет — старт с defaultSpawn

        foreach (CheckPoint cp in FindObjectsByType<CheckPoint>(FindObjectsSortMode.None))
        {
            if (cp.checkpointId != savedId) continue;

            activeCheckpoint = cp;
            if (debugLog)
                Debug.Log($"[Checkpoints] Загружен сохранённый чекпоинт ID {savedId} ('{cp.name}').", this);
            return;
        }

        Debug.LogWarning($"[Checkpoints] Сохранённый чекпоинт ID {savedId} не найден в сцене! Игрок стартует с начала.", this);
    }

    /// <summary>Мгновенно (без затемнения) ставим игрока в точку восстановления.</summary>
    private void PlacePlayerAtRestorePoint()
    {
        if (player == null) return;
        if (!TryGetRestorePoint(out Vector3 pos, out Quaternion rot)) return;

        TeleportPlayer(pos, rot);
    }

    private bool TryGetRestorePoint(out Vector3 pos, out Quaternion rot)
    {
        if (activeCheckpoint != null)
        {
            pos = activeCheckpoint.SpawnPosition;
            rot = activeCheckpoint.SpawnRotation;
            return true;
        }
        if (defaultSpawn != null)
        {
            pos = defaultSpawn.position + Vector3.up * defaultSpawnHeightOffset;
            rot = defaultSpawn.rotation;
            return true;
        }
        pos = Vector3.zero;
        rot = Quaternion.identity;
        return false;
    }

    // ================= Публичный API =================

    /// <summary>Сделать чекпоинт активным и сохранить прогресс. Вызывается из CheckPoint.</summary>
    public static void Activate(CheckPoint checkpoint)
    {
        if (Instance == null || checkpoint == null) return;
        if (Instance.activeCheckpoint == checkpoint) return; // уже активен (защита от повторного входа)

        Instance.activeCheckpoint = checkpoint;
        GameProgress.SaveCheckpoint(checkpoint.checkpointId);

        if (Instance.debugLog)
            Debug.Log($"[Checkpoints] Активный чекпоинт: '{checkpoint.name}' (ID {checkpoint.checkpointId}). Прогресс сохранён.", checkpoint);
    }

    /// <summary>Убить игрока: затемнение → телепорт на последний чекпоинт → проявление. Вызывается из DeathZone.</summary>
    public static void KillPlayer()
    {
        if (Instance == null)
        {
            Debug.LogWarning("[Checkpoints] KillPlayer вызван, но на сцене нет CheckpointManager!");
            return;
        }
        Instance.StartCoroutine(Instance.RespawnRoutine());
    }

    /// <summary>Проверка, что коллайдер принадлежит игроку (для триггеров).</summary>
    public static bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag("Player")) return true;

        Rigidbody attached = other.attachedRigidbody;
        return attached != null && attached.CompareTag("Player");
    }

    // ================= Респавн =================

    private IEnumerator RespawnRoutine()
    {
        if (respawning || player == null) yield break;
        respawning = true;

        // 1. Замораживаем игрока: отключаем управление и гасим скорости
        player.enabled = false;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. Затемняем экран
        if (screenFader != null)
            yield return screenFader.FadeOut();

        // 3. Телепортируем на последний чекпоинт
        if (TryGetRestorePoint(out Vector3 pos, out Quaternion rot))
            TeleportPlayer(pos + Vector3.up * respawnLift, rot);

        // 4. Возвращаем управление
        player.enabled = true;

        // 5. Проявляем экран
        if (screenFader != null)
            yield return screenFader.FadeIn();

        respawning = false;

        PlayerRespawned?.Invoke();
    }

    private void TeleportPlayer(Vector3 pos, Quaternion rot)
    {
        player.transform.SetPositionAndRotation(pos, rot);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = pos;
            rb.rotation = rot;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Без SyncTransforms физика может "дотянуть" тело обратно к старой позиции
        Physics.SyncTransforms();
    }

    private void Update()
    {
        if (!enableFallDeath || respawning || player == null) return;

        if (player.transform.position.y < fallDeathY)
        {
            if (debugLog)
                Debug.Log($"[Checkpoints] Игрок упал ниже Y {fallDeathY} — гибель и респавн.", this);
            KillPlayer();
        }
    }

    // ================= Отладка =================

    private void WarnDuplicateIds()
    {
        if (!debugLog) return;

        Dictionary<int, int> idCounts = new Dictionary<int, int>();
        foreach (CheckPoint cp in FindObjectsByType<CheckPoint>(FindObjectsSortMode.None))
        {
            idCounts.TryGetValue(cp.checkpointId, out int count);
            idCounts[cp.checkpointId] = count + 1;
        }

        foreach (KeyValuePair<int, int> pair in idCounts)
        {
            if (pair.Value > 1)
                Debug.LogError($"[Checkpoints] ID {pair.Key} используется у {pair.Value} чекпоинтов! Каждый чекпоинт должен иметь уникальный ID, иначе сейв сломается.", this);
        }
    }
}
