using UnityEngine;

/// <summary>
/// Спавнит случайные префабы мусора на плоской прямоугольной области.
/// Область лежит в локальной плоскости XZ объекта, а поворот мусора меняется
/// только вокруг вертикальной оси Y.
/// </summary>
[DisallowMultipleComponent]
public class RandomTrashSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnZone
    {
        [Tooltip("Центр зоны в локальных координатах спавнера.")]
        public Vector2 center;

        [Tooltip("Размер зоны: X — ширина, Y — длина по локальной оси Z.")]
        [Min(0f)]
        public Vector2 size = new Vector2(5f, 5f);
    }

    [Header("Trash")]
    [Tooltip("Префабы мусора. Каждый префаб должен содержать компонент TrashItem.")]
    [SerializeField] private TrashItem[] trashPrefabs;

    [Min(0)]
    [SerializeField] private int amount = 10;

    [Tooltip("Родитель для созданных объектов. Если не задан, используется этот объект.")]
    [SerializeField] private Transform spawnedTrashRoot;

    [Header("Flat Spawn Area")]
    [Tooltip("Размер области в локальных координатах объекта: X — ширина, Y — длина по мировой плоскости Z.")]
    [Min(0f)]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 10f);

    [Tooltip("Дополнительные зоны спавна. Для каждого предмета случайно выбирается одна зона. Если список пуст, используется Area Size.")]
    [SerializeField] private SpawnZone[] zones;

    [Tooltip("Смещение мусора вверх относительно плоскости спавна.")]
    [SerializeField] private float heightOffset;

    [Header("Rotation")]
    [Tooltip("Минимальный случайный угол поворота вокруг оси Y.")]
    [SerializeField] private float minYRotation;

    [Tooltip("Максимальный случайный угол поворота вокруг оси Y.")]
    [SerializeField] private float maxYRotation = 360f;

    [Tooltip("Локация, в которой нужно зарегистрировать созданный мусор.")]
    [SerializeField] private TrashLocation trashLocation;

    [ContextMenu("Spawn Trash")]
    public void Spawn()
    {
        SpawnInternal(null);
    }

    /// <summary>Восстанавливает тот же набор мусора по сохранённому seed.</summary>
    public void SpawnWithSeed(int seed)
    {
        Random.State previousState = Random.state;
        Random.InitState(seed);
        SpawnInternal(seed);
        Random.state = previousState;
    }

    public bool HasSpawnedTrash
    {
        get
        {
            Transform root = spawnedTrashRoot != null ? spawnedTrashRoot : transform;
            return root.GetComponentInChildren<TrashItem>(true) != null;
        }
    }

    private void SpawnInternal(int? seed)
    {
        if (trashPrefabs == null || trashPrefabs.Length == 0 || amount <= 0)
            return;

        Transform root = spawnedTrashRoot != null ? spawnedTrashRoot : transform;
        TrashLocation location = trashLocation != null
            ? trashLocation
            : GetComponentInParent<TrashLocation>();

        for (int i = 0; i < amount; i++)
        {
            TrashItem prefab = GetRandomPrefab();
            if (prefab == null)
                continue;

            Vector3 localPosition = GetRandomLocalPosition();

            Quaternion randomRotation = Quaternion.Euler(
                0f,
                Random.Range(minYRotation, maxYRotation),
                0f);

            TrashItem spawned = Instantiate(
                prefab,
                transform.TransformPoint(localPosition),
                transform.rotation * randomRotation * prefab.transform.localRotation,
                root);

            if (location != null)
                location.RegisterRuntimeTrash(spawned);
        }
    }

    private Vector3 GetRandomLocalPosition()
    {
        Vector2 center = Vector2.zero;
        Vector2 size = areaSize;

        if (zones != null && zones.Length > 0)
        {
            SpawnZone zone = zones[Random.Range(0, zones.Length)];
            if (zone != null)
            {
                center = zone.center;
                size = zone.size;
            }
        }

        return new Vector3(
            center.x + Random.Range(-Mathf.Abs(size.x) * 0.5f, Mathf.Abs(size.x) * 0.5f),
            heightOffset,
            center.y + Random.Range(-Mathf.Abs(size.y) * 0.5f, Mathf.Abs(size.y) * 0.5f));
    }

    private TrashItem GetRandomPrefab()
    {
        // Несколько пустых элементов в массиве не должны ломать весь спавн.
        for (int attempt = 0; attempt < trashPrefabs.Length; attempt++)
        {
            TrashItem prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
            if (prefab != null)
                return prefab;
        }

        Debug.LogWarning($"[{nameof(RandomTrashSpawner)}] Не указаны валидные префабы мусора.", this);
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.35f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (zones == null || zones.Length == 0)
        {
            DrawZoneGizmo(Vector2.zero, areaSize);
        }
        else
        {
            foreach (SpawnZone zone in zones)
            {
                if (zone != null)
                    DrawZoneGizmo(zone.center, zone.size);
            }
        }

        Gizmos.matrix = previousMatrix;
    }

    private void DrawZoneGizmo(Vector2 center, Vector2 size)
    {
        Vector3 position = new Vector3(center.x, heightOffset, center.y);
        Vector3 dimensions = new Vector3(Mathf.Abs(size.x), 0.02f, Mathf.Abs(size.y));
        Gizmos.DrawCube(position, dimensions);
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 1f);
        Gizmos.DrawWireCube(position, dimensions);
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.35f);
    }
}
