using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Чекпоинт — триггерная зона, срабатывающая ОДИН раз при входе игрока.
/// Становится активной точкой возрождения и сразу сохраняется в GameProgress
/// (переживает перезапуск игры).
///
/// Как пользоваться: добавить на пустой объект, включить галочку IsTrigger
/// (или добавить компонент — коллайдер-триггер настроится сам через Reset()).
/// Внутри автоматически создаётся дочерний объект SpawnPoint — его можно
/// двигать отдельно, чтобы задать точное место возрождения.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class CheckPoint : MonoBehaviour
{
    [Header("Идентификация")]
    [Tooltip("Уникальный ID чекпоинта (0, 1, 2...). НЕ меняйте после того, как прогресс уже сохранён у игроков: по этому ID загружается сохранение!")]
    [Min(0)] public int checkpointId = 0;

    [Header("Точка возрождения")]
    [Tooltip("Куда ставится игрок при респавне. Если пусто — используется центр этого объекта.")]
    public Transform spawnPoint;

    [Tooltip("Насколько поднять игрока над точкой спавна, чтобы он не застрял в коллайдере пола")]
    [Min(0f)] public float spawnHeightOffset = 0.1f;

    [Header("Отладка")]
    [Tooltip("Писать в консоль при активации")]
    public bool logActivation = true;

    /// <summary>Срабатывал ли триггер в текущей сессии (один раз за вход в игру).</summary>
    public bool HasBeenTriggered { get; private set; }

    /// <summary>Мировая позиция возрождения (с учётом вертикального смещения).</summary>
    public Vector3 SpawnPosition =>
        (spawnPoint != null ? spawnPoint.position : transform.position) + Vector3.up * spawnHeightOffset;

    /// <summary>Поворот игрока при возрождении.</summary>
    public Quaternion SpawnRotation => spawnPoint != null ? spawnPoint.rotation : transform.rotation;

    /// <summary>Вызывается Unity при добавлении компонента в редакторе — сразу настраиваем триггер.</summary>
    private void Reset()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(4f, 3f, 4f);

        EnsureSpawnPoint();
    }

    /// <summary>
    /// Гарантирует наличие РОВНО одного SpawnPoint.
    /// Если точек несколько (дубликаты от редактора) — оставляет одну, остальные удаляет.
    /// </summary>
    private void EnsureSpawnPoint()
    {
        // Собираем все дочерние объекты с именем SpawnPoint
        List<Transform> found = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name == "SpawnPoint")
                found.Add(child);
        }

        if (found.Count > 0)
        {
            // Приоритет: точка, на которую уже указывает поле, иначе первая найденная
            Transform keep = (spawnPoint != null && spawnPoint.parent == transform && found.Contains(spawnPoint))
                ? spawnPoint
                : found[0];

            spawnPoint = keep;

            // Удаляем дубликаты (в редакторе это безопасно — DestroyImmediate)
            for (int i = 0; i < found.Count; i++)
            {
                if (found[i] != keep)
                    Object.DestroyImmediate(found[i].gameObject);
            }
            return;
        }

        GameObject spawnGo = new GameObject("SpawnPoint");
        spawnGo.transform.SetParent(transform, false);
        spawnPoint = spawnGo.transform;
    }

#if UNITY_EDITOR
    /// <summary>ПКМ по компоненту в инспекторе → убрать дубликаты SpawnPoint.</summary>
    [ContextMenu("Убрать дубликаты SpawnPoint")]
    private void RemoveDuplicateSpawnPoints()
    {
        EnsureSpawnPoint();
        Debug.Log($"[CheckPoint] '{name}': осталась одна SpawnPoint: '{spawnPoint.name}'.", this);
    }
#endif

    private void Awake()
    {
        // Гарантируем, что коллайдер — триггер (даже если объект собирали вручную)
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasBeenTriggered) return;                 // срабатывает один раз
        if (!CheckpointManager.IsPlayerCollider(other)) return;

        HasBeenTriggered = true;
        if (logActivation)
            Debug.Log($"[CheckPoint] '{name}' (ID {checkpointId}): игрок вошёл, чекпоинт активирован.", this);

        CheckpointManager.Activate(this);
    }
}
