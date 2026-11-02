using UnityEngine;

/// <summary>
/// Звуки шагов робота. Шаг считается по ПРОЙДЕННОМУ РАССТОЯНИЮ (не по таймеру) —
/// поэтому ритм всегда совпадает с реальной скоростью: при спринте шаги чаще сами собой.
///
/// Требования:
///  - на объекте должен быть PlayerMovements (для земли и спринта);
///  - AudioSource создаётся автоматически (3D-звук).
///
/// Настройка: перетащи в footsteps 4–8 вариаций звука шага (бетон/метал).
/// </summary>
[RequireComponent(typeof(PlayerMovements))]
public class FootstepSystem : MonoBehaviour
{
    [Header("Звуки")]
    [Tooltip("Вариации шага (4–8 штук). Играется случайная.")]
    [SerializeField] private AudioClip[] footsteps;

    [Header("Ритм (метры на один шаг)")]
    [Tooltip("Длина шага при ходьбе")]
    [Min(0.2f)] [SerializeField] private float walkStepDistance = 1.4f;
    [Tooltip("Длина шага при беге (больше = шаги реже, но скорость выше => чаще по времени)")]
    [Min(0.2f)] [SerializeField] private float sprintStepDistance = 1.9f;

    [Header("Громкость")]
    [Range(0f, 1f)] [SerializeField] private float walkVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float sprintVolume = 0.8f;

    [Header("Вариативность")]
    [Tooltip("Случайный питч: ниже/выше, чтобы шаги не звучали как дрель")]
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;
    [Tooltip("Дополнительный множитель питча при беге")]
    [SerializeField] private float sprintPitchBoost = 1.06f;

    [Header("Полёт")]
    [Tooltip("Обнулять накопитель дистанции в воздухе (чтобы после прыжка не было мгновенного шага)")]
    [SerializeField] private bool resetInAir = true;

    private PlayerMovements player;
    private AudioSource source;
    private Vector3 lastPosition;
    private float distanceAccumulator;

    private void Awake()
    {
        player = GetComponent<PlayerMovements>();

        source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;            // полностью 3D-звук
            source.dopplerLevel = 0f;            // без эффекта Доплера на собственных шагах
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 30f;
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (player == null || footsteps == null || footsteps.Length == 0)
            return;

        // В воздухе шагов нет
        if (!player.IsOnGround)
        {
            if (resetInAir)
            {
                distanceAccumulator = 0f;
                lastPosition = transform.position;
            }
            return;
        }

        // Накапливаем пройденное расстояние (только горизонталь)
        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;
        distanceAccumulator += delta.magnitude;

        float stepDistance = player.IsSprinting ? sprintStepDistance : walkStepDistance;

        if (distanceAccumulator >= stepDistance)
        {
            distanceAccumulator -= stepDistance; // остаток переносим — ритм не дёргается
            PlayStep();
        }
    }

    private void PlayStep()
    {
        AudioClip clip = footsteps[Random.Range(0, footsteps.Length)];
        if (clip == null) return;

        bool sprinting = player.IsSprinting;
        source.pitch = Random.Range(minPitch, maxPitch) * (sprinting ? sprintPitchBoost : 1f);
        source.PlayOneShot(clip, sprinting ? sprintVolume : walkVolume);
    }
}
