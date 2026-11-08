using UnityEngine;

/// <summary>
/// Создаёт тело паразита из случайно расположенных кубов.
/// Повесьте компонент на дочерний объект visualRoot паразита, чтобы сжатие
/// здоровья в <see cref="Parasite"/> применялось ко всей сгенерированной форме.
/// </summary>
[DisallowMultipleComponent]
public class ParasiteCubeGenerator : MonoBehaviour
{
    [Header("Префабы кубов")]
    [Tooltip("Из этого списка для каждого куба случайно выбирается префаб.")]
    [SerializeField] private GameObject[] cubePrefabs;

    [Header("Генерация")]
    [Tooltip("Сколько кубов создать.")]
    [SerializeField, Min(1)] private int cubeCount = 20;

    [Tooltip("Размер области размещения в локальных координатах этого объекта.")]
    [SerializeField] private Vector3 areaSize = new Vector3(2f, 2f, 2f);

    [Tooltip("Создавать кубы автоматически при запуске сцены.")]
    [SerializeField] private bool generateOnStart = true;

    [Header("Случайный размер")]
    [Tooltip("Минимальный локальный масштаб куба по каждой оси.")]
    [SerializeField] private Vector3 minScale = Vector3.one * 0.2f;

    [Tooltip("Максимальный локальный масштаб куба по каждой оси.")]
    [SerializeField] private Vector3 maxScale = Vector3.one;

    [Header("Поворот")]
    [Tooltip("Добавлять случайный поворот каждому кубу.")]
    [SerializeField] private bool randomRotation = true;

    [Tooltip("Максимальный поворот по каждой оси в градусах.")]
    [SerializeField] private Vector3 maxRotation = new Vector3(20f, 20f, 20f);

    private const string GeneratedRootName = "Generated Parasite Cubes";

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    /// <summary>Удаляет прежние кубы и создаёт новую случайную форму.</summary>
    [ContextMenu("Generate Cubes")]
    public void Generate()
    {
        if (cubePrefabs == null || cubePrefabs.Length == 0)
        {
            Debug.LogWarning("ParasiteCubeGenerator: назначьте хотя бы один Cube Prefab.", this);
            return;
        }

        Transform generatedRoot = GetOrCreateGeneratedRoot();
        ClearGeneratedCubes(generatedRoot);

        for (int i = 0; i < cubeCount; i++)
        {
            GameObject prefab = GetRandomPrefab();
            if (prefab == null)
                continue;

            GameObject cube = Instantiate(prefab, generatedRoot);
            cube.name = prefab.name;
            cube.transform.localPosition = GetRandomPosition();
            cube.transform.localRotation = randomRotation
                ? Quaternion.Euler(
                    Random.Range(-maxRotation.x, maxRotation.x),
                    Random.Range(-maxRotation.y, maxRotation.y),
                    Random.Range(-maxRotation.z, maxRotation.z))
                : Quaternion.identity;
            cube.transform.localScale = GetRandomScale();
        }
    }

    [ContextMenu("Clear Generated Cubes")]
    public void Clear()
    {
        Transform generatedRoot = transform.Find(GeneratedRootName);
        if (generatedRoot != null)
            ClearGeneratedCubes(generatedRoot);
    }

    private Transform GetOrCreateGeneratedRoot()
    {
        Transform generatedRoot = transform.Find(GeneratedRootName);
        if (generatedRoot != null)
            return generatedRoot;

        GameObject root = new GameObject(GeneratedRootName);
        root.transform.SetParent(transform, false);
        return root.transform;
    }

    private void ClearGeneratedCubes(Transform generatedRoot)
    {
        for (int i = generatedRoot.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(generatedRoot.GetChild(i).gameObject);
            else
                DestroyImmediate(generatedRoot.GetChild(i).gameObject);
        }
    }

    private GameObject GetRandomPrefab()
    {
        int firstIndex = Random.Range(0, cubePrefabs.Length);
        for (int offset = 0; offset < cubePrefabs.Length; offset++)
        {
            GameObject prefab = cubePrefabs[(firstIndex + offset) % cubePrefabs.Length];
            if (prefab != null)
                return prefab;
        }

        Debug.LogWarning("ParasiteCubeGenerator: в списке Cube Prefabs нет назначенных префабов.", this);
        return null;
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-areaSize.x, areaSize.x) * 0.5f,
            Random.Range(-areaSize.y, areaSize.y) * 0.5f,
            Random.Range(-areaSize.z, areaSize.z) * 0.5f);
    }

    private Vector3 GetRandomScale()
    {
        return new Vector3(
            Random.Range(Mathf.Min(minScale.x, maxScale.x), Mathf.Max(minScale.x, maxScale.x)),
            Random.Range(Mathf.Min(minScale.y, maxScale.y), Mathf.Max(minScale.y, maxScale.y)),
            Random.Range(Mathf.Min(minScale.z, maxScale.z), Mathf.Max(minScale.z, maxScale.z)));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.25f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, areaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
    }
}
