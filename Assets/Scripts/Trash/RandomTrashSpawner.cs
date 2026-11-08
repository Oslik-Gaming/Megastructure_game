using UnityEngine;

/// <summary>
/// Спавнит случайные префабы мусора на плоской прямоугольной области.
/// Область лежит в локальной плоскости XZ объекта, а поворот мусора меняется
/// только вокруг вертикальной оси Y.
/// </summary>
[DisallowMultipleComponent]
public class RandomTrashSpawner : MonoBehaviour
{
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

            Vector3 localPosition = new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                heightOffset,
                Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f));

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
        Gizmos.DrawCube(new Vector3(0f, heightOffset, 0f), new Vector3(areaSize.x, 0.02f, areaSize.y));
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 1f);
        Gizmos.DrawWireCube(new Vector3(0f, heightOffset, 0f), new Vector3(areaSize.x, 0.02f, areaSize.y));
        Gizmos.matrix = previousMatrix;
    }
}
