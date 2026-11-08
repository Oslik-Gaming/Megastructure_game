using UnityEngine;

/// <summary>
/// Растягивает паразита вдоль положительной оси Z, когда игрок входит в триггер.
/// Скорость роста нарастает линейно или экспоненциально.
/// Рост идёт до тех пор, пока ПЕРЕДНИЙ край паразита не дотянется до заданной
/// мировой координаты Z (Target Edge Z).
///
/// Как пользоваться:
///  1. Повесить на объект с BoxCollider (Is Trigger настроится сам) — например, на TrigerZone.
///  2. Растянуть коллайдер под зону входа игрока.
///  3. Перетащить паразита (объект с мешем) в поле Parasite.
///  4. На паразита повесить DeathZone и включить Is Trigger на его коллайдере — касание = смерть.
///
/// После смерти игрока (респавн на чекпоинте) размер паразита и скорость сбрасываются,
/// рост ждёт следующего входа в триггер.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ParasiteGrower : MonoBehaviour
{
    private enum SpeedMode { Linear, Exponential }

    [Header("Цель")]
    [Tooltip("Паразит, который будет тянуться вдоль +Z.")]
    [SerializeField] private Transform parasite;

    [Header("Скорость роста")]
    [Tooltip("Начальная скорость роста, единиц в секунду.")]
    [SerializeField, Min(0.01f)] private float startSpeed = 0.2f;

    [Tooltip("Как растёт скорость: Linear — прибавляется каждый сек, Exponential — умножается каждый сек.")]
    [SerializeField] private SpeedMode speedMode = SpeedMode.Linear;

    [Tooltip("Ускорение. Linear: прибавка скорости за секунду (м/с²). Exponential: доля прироста в секунду (0.5 = скорость ×1.5 каждую секунду).")]
    [SerializeField, Min(0f)] private float acceleration = 0.1f;

    [Header("Цель роста")]
    [Tooltip("Мировая координата Z, до которой должен дорасти передний край паразита.")]
    [SerializeField] private float targetEdgeZ = 20f;

    [Tooltip("Держать задний край паразита на месте: он растягивается только вперёд, в +Z.")]
    [SerializeField] private bool keepBackEdgeFixed = true;

    [Header("Отладка")]
    [SerializeField] private bool debugLog = true;

    private bool growing;
    private float speed;
    private float progressLogTimer;

    private Vector3 initialScale;
    private Vector3 initialLocalPosition;
    private bool captured;

    private ParasitePulse pulse;
    private bool pulseWasEnabled;

    private void Awake()
    {
        // Гарантируем, что зона входа — триггер
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        CaptureInitial();
        CheckpointManager.PlayerRespawned += ResetGrowth;
    }

    private void OnDestroy()
    {
        CheckpointManager.PlayerRespawned -= ResetGrowth;
    }

    /// <summary>Запомнить исходный размер и позицию паразита (для сброса).</summary>
    private void CaptureInitial()
    {
        if (parasite == null || captured) return;
        initialScale = parasite.localScale;
        initialLocalPosition = parasite.localPosition;
        captured = true;
    }

    /// <summary>Мировые границы паразита: сначала рендереры, потом коллайдеры, потом фолбэк по scale.</summary>
    private bool TryGetParasiteBounds(out Bounds bounds)
    {
        bounds = default;

        foreach (Renderer r in parasite.GetComponentsInChildren<Renderer>())
        {
            bounds = bounds.size == Vector3.zero ? r.bounds : Encapsulate(bounds, r.bounds);
        }
        if (bounds.size == Vector3.zero)
        {
            foreach (Collider c in parasite.GetComponentsInChildren<Collider>())
            {
                bounds = bounds.size == Vector3.zero ? c.bounds : Encapsulate(bounds, c.bounds);
            }
        }
        if (bounds.size == Vector3.zero)
        {
            // Совсем ничего не нашли — считаем размер по scale (диаметр дефолтного меша = 1)
            Vector3 s = parasite.lossyScale;
            bounds = new Bounds(parasite.position, s);
        }
        return bounds.size.z > 0.0001f;
    }

    private static Bounds Encapsulate(Bounds a, Bounds b)
    {
        a.Encapsulate(b);
        return a;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CheckpointManager.IsPlayerCollider(other)) return;
        if (growing) return;

        CaptureInitial();
        speed = startSpeed;
        growing = true;
        progressLogTimer = 0f;

        // ParasitePulse перезаписывает localScale каждый кадр во время пульсации
        // и мешает росту — на время роста выключаем его.
        pulse = parasite.GetComponentInParent<ParasitePulse>();
        if (pulse == null) pulse = parasite.GetComponentInChildren<ParasitePulse>();
        if (pulse != null)
        {
            pulseWasEnabled = pulse.enabled;
            pulse.enabled = false;
            if (debugLog) Debug.Log($"[ParasiteGrower] '{name}': ParasitePulse отключён на время роста.", this);
        }

        if (debugLog)
        {
            TryGetParasiteBounds(out Bounds b);
            Debug.Log($"[ParasiteGrower] '{name}': игрок вошёл — паразит начал расти. " +
                      $"Край сейчас Z={b.max.z:0.00}, цель Z={targetEdgeZ:0.00}.", this);
        }
    }

    private void Update()
    {
        if (!growing || parasite == null) return;

        // Нарастание скорости
        if (speedMode == SpeedMode.Linear)
            speed += acceleration * Time.deltaTime;
        else
            speed *= 1f + acceleration * Time.deltaTime;

        if (!TryGetParasiteBounds(out Bounds b)) return;

        float backZ = b.min.z;
        float frontZ = b.max.z;
        float sizeZ = b.size.z;

        // Насколько должен продвинуться передний край в этот кадр
        float newFront = frontZ + speed * Time.deltaTime;
        if (newFront >= targetEdgeZ) newFront = targetEdgeZ;
        float growth = newFront - frontZ;

        if (growth <= 0f)
        {
            growing = false;
            if (debugLog) Debug.Log($"[ParasiteGrower] '{name}': передний край паразита достиг Z={targetEdgeZ:0.00}.", this);
            return;
        }

        // Растягиваем по Z пропорционально приросту края
        float factor = (sizeZ + growth) / sizeZ;
        Vector3 scale = parasite.localScale;
        scale.z *= factor;
        parasite.localScale = scale;

        // Сдвигаем пивот так, чтобы задний край остался на месте.
        // Работает при родителе без поворота и с равномерным scale (как в сцене).
        if (keepBackEdgeFixed)
        {
            float centerOffsetZ = b.center.z - parasite.position.z;
            float newBackZ = parasite.position.z + centerOffsetZ * factor - (sizeZ + growth) * 0.5f;
            float shift = backZ - newBackZ;

            Vector3 pos = parasite.localPosition;
            pos.z += shift;
            parasite.localPosition = pos;
        }

        progressLogTimer += Time.deltaTime;
        if (debugLog && progressLogTimer >= 1f)
        {
            progressLogTimer = 0f;
            Debug.Log($"[ParasiteGrower] '{name}': край Z={frontZ + growth:0.00} → {targetEdgeZ:0.00}, скорость {speed:0.00}.", this);
        }
    }

    /// <summary>Сброс после смерти игрока: размер, позиция и скорость — как было.</summary>
    private void ResetGrowth()
    {
        growing = false;
        speed = 0f;

        // Возвращаем пульс на место
        if (pulse != null)
        {
            pulse.enabled = pulseWasEnabled;
            pulse = null;
        }

        if (parasite != null && captured)
        {
            parasite.localScale = initialScale;
            parasite.localPosition = initialLocalPosition;
        }

        if (debugLog) Debug.Log($"[ParasiteGrower] '{name}': сброс паразита после респавна.", this);
    }
}
