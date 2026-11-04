using System.Collections;
using UnityEngine;

/// <summary>
/// Зона-триггер, проигрывающая звук ОДИН раз при входе игрока (хоррор-события).
///
/// Как пользоваться:
///  1. ПКМ в иерархии → "Звук" → "One-Shot Trigger" (или повесь скрипт на Empty вручную —
///     коллайдер-триггер настроится сам через Reset()).
///  2. На том же объекте (или на отдельном Empty для музыки) настрой AudioSource КАК ХОЧЕШЬ:
///     клип, громкость, 2D/3D, рандом, AudioEchoFilter (эхо), AudioReverbZone (реверберация).
///     Скрипт не трогает настройки звука — он только запускает проигрывание.
///  3. Если AudioSource на другом объекте — перетащи его в поле Source.
///
/// Звук играется один раз за сессию. Опция Rearm On Respawn позволяет проиграть его снова
/// после смерти игрока (для скримеров, которые должны срабатывать при каждом заходе в зону
/// после возрождения).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class OneShotSoundTrigger : MonoBehaviour
{
    [Header("Звук")]
    [Tooltip("AudioSource, который запустить. Если пусто — берётся с этого объекта, затем с детей.")]
    [SerializeField] private AudioSource source;

    [Tooltip("Задержка перед проигрыванием после входа в зону (сек). Удобно, чтобы звук бился с визуальным событием.")]
    [Min(0f)] [SerializeField] private float delay = 0f;

    [Tooltip("Остановить текущий звук на этом AudioSource перед проигрыванием")]
    [SerializeField] private bool stopSourceBeforePlaying = false;

    [Header("Правила срабатывания")]
    [Tooltip("Проиграть только один раз за игровую сессию")]
    [SerializeField] private bool playOncePerSession = true;

    [Tooltip("После респавна игрока снова разрешить проигрывание (для повторных хоррор-событий после смерти)")]
    [SerializeField] private bool rearmOnRespawn = false;

    [Header("После срабатывания")]
    [Tooltip("Отключить триггер-коллайдер, чтобы зона больше не срабатывала")]
    [SerializeField] private bool disableTriggerAfterPlay = true;

    [Tooltip("Удалить объект после того, как звук доиграет (для одноразовых скримеров)")]
    [SerializeField] private bool destroyAfterFinished = false;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = false;

    /// <summary>Проигрывался ли звук (с учётом реарм-ов).</summary>
    public bool HasPlayed { get; private set; }

    private Coroutine playRoutine;

    private void Reset()
    {
        // Как у CheckPoint: при добавлении компонента вручную настраиваем триггер сами
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        if (col.size == Vector3.one) // дефолтный размер — ставим разумный для звуковой зоны
            col.size = new Vector3(4f, 3f, 4f);
    }

    private void Awake()
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
            if (source == null) source = GetComponentInChildren<AudioSource>();
        }

        if (source != null) source.playOnAwake = false;
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

        HasPlayed = false;
        if (disableTriggerAfterPlay)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null) col.enabled = true;
        }

        if (debugLog) Debug.Log($"[OneShotSoundTrigger] '{name}' перевзведён после респавна.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CheckpointManager.IsPlayerCollider(other)) return;
        if (playOncePerSession && HasPlayed) return;

        HasPlayed = true;

        if (disableTriggerAfterPlay)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
        }

        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayDelayed());
    }

    private IEnumerator PlayDelayed()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (source == null)
        {
            Debug.LogWarning($"[OneShotSoundTrigger] На '{name}' не найден AudioSource — назначь его в поле Source.", this);
            yield break;
        }

        if (stopSourceBeforePlaying) source.Stop();
        source.Play();

        if (debugLog) Debug.Log($"[OneShotSoundTrigger] '{name}' проиграл '{source.clip?.name}'.", this);

        if (destroyAfterFinished)
        {
            // Ждём, пока звук доиграет (для зацикленного — удаляем сразу)
            float timeout = 30f;
            while (source.isPlaying && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = HasPlayed ? new Color(0.5f, 0.5f, 0.5f, 0.4f) : new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
        }

        // Показываем, откуда реально будет звучать источник
        AudioSource src = source != null ? source : GetComponentInChildren<AudioSource>();
        if (src != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawSphere(src.transform.position, 0.25f);
        }
    }
}
