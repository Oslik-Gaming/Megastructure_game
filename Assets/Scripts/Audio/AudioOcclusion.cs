using UnityEngine;

/// <summary>
/// Заглушение звука за стенами (occlusion).
///
/// Каждый интервал пускает луч от ушей игрока (AudioListener) к источнику.
/// Если путь перекрыт геометрией — приглушает громкость и срезает верхние
/// частоты через AudioLowPassFilter (звук "из-за бетонной стены").
///
/// Повесить на объект с AudioSource. Фильтр добавится автоматически.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioOcclusion : MonoBehaviour
{
    [Header("Проверка препятствий")]
    [Tooltip("Слои, которые считаются стенами (обычно Default/уровень геометрии)")]
    [SerializeField] private LayerMask occlusionMask = ~0;
    [Tooltip("Как часто проверять (сек). 0.1–0.2 достаточно, чаще — дороже")]
    [Range(0.05f, 0.5f)] [SerializeField] private float updateInterval = 0.15f;
    [Tooltip("Дополнительные лучи по краям источника: не даёт звуку 'просачиваться' мимо угла стены")]
    [Range(0, 4)] [SerializeField] private int extraRays = 2;
    [Tooltip("Радиус разброса дополнительных лучей (метры)")]
    [SerializeField] private float extraRayRadius = 0.6f;

    [Header("Звук в открытом пространстве")]
    [Tooltip("Частота среза без препятствий (20000 = без эффекта)")]
    [SerializeField] private float openCutoffFrequency = 20000f;
    [Tooltip("Множитель громкости без препятствий")]
    [Range(0f, 1f)] [SerializeField] private float openVolume = 1f;

    [Header("Звук за стеной")]
    [Tooltip("Частота среза за стеной: ниже = глуше (700–1500 хорошо глушит)")]
    [SerializeField] private float blockedCutoffFrequency = 1000f;
    [Tooltip("Множитель громкости за стеной")]
    [Range(0f, 1f)] [SerializeField] private float blockedVolume = 0.12f;

    [Header("Плавность")]
    [Tooltip("Скорость перехода открыт/закрыт (выше = быстрее)")]
    [SerializeField] private float lerpSpeed = 6f;

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private Transform listener;
    private float timer;
    private bool occluded;
    private float currentVolume = 1f;
    private float currentCutoff = 20000f;
    private float baseVolume = 1f;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        lowPass = GetComponent<AudioLowPassFilter>();
        if (lowPass == null)
            lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        lowPass.cutoffFrequency = openCutoffFrequency;

        baseVolume = source.volume;
    }

    private void Start()
    {
        listener = FindListener();
        if (listener == null)
            Debug.LogWarning("[AudioOcclusion] AudioListener не найден на сцене!", this);
    }

    private void Update()
    {
        if (listener == null)
        {
            listener = FindListener();
            if (listener == null) return;
        }

        // Периодическая проверка рейкастами (не каждый кадр — дешевле)
        timer += Time.unscaledDeltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            occluded = IsOccluded();
        }

        // Плавно ведём громкость и срез частот к целевым значениям
        float targetVolume = baseVolume * (occluded ? blockedVolume : openVolume);
        float targetCutoff = occluded ? blockedCutoffFrequency : openCutoffFrequency;

        currentVolume = Mathf.Lerp(currentVolume, targetVolume, lerpSpeed * Time.unscaledDeltaTime);
        currentCutoff = Mathf.Lerp(currentCutoff, targetCutoff, lerpSpeed * Time.unscaledDeltaTime);

        source.volume = currentVolume;
        lowPass.cutoffFrequency = currentCutoff;
    }

    private bool IsOccluded()
    {
        Vector3 from = listener.position;
        Vector3 to = transform.position;

        // Центральный луч
        if (Blocked(from, to)) return true;

        // Дополнительные лучи по краям источника
        for (int i = 0; i < extraRays; i++)
        {
            Vector2 circle = Random.insideUnitCircle * extraRayRadius;
            Vector3 offset = transform.right * circle.x + transform.up * circle.y;
            if (Blocked(from, to + offset)) return true;
        }
        return false;
    }

    private bool Blocked(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.01f) return false;

        // Луч от ушей к источнику: коллайдеры самого игрока игнорируем
        // (камера находится внутри капсулы робота), остальная геометрия — стены
        int count = Physics.RaycastNonAlloc(from, direction / distance, hits, distance - 0.1f,
            occlusionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = hits[i].collider;
            if (hit.transform.IsChildOf(listener.root))
                continue; // свой коллайдер — не стена
            return true;
        }
        return false;
    }

    private static Transform FindListener()
    {
        AudioListener found = FindFirstObjectByType<AudioListener>();
        return found != null ? found.transform : null;
    }
}
