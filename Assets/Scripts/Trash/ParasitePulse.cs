using System.Collections;
using UnityEngine;

/// <summary>
/// Периодическая пульсация паразита. Вешается на кубик (визуальную модель) паразита.
/// Пауза между пульсациями выбирается СЛУЧАЙНО в диапазоне от Pulse Interval Min до Pulse Interval Max.
/// Пульсация отсчитывается от ТЕКУЩЕГО масштаба паразита, поэтому корректно работает
/// вместе с уменьшением от урона (Parasite.UpdateScale): уменьшенный паразит
/// пульсирует вокруг своего уменьшенного размера и никогда не «возвращается» к полному.
/// Звук: если задан массив Pulse Sounds — каждый раз играет случайный клип из него;
/// иначе играет AudioClip, назначенный на AudioSource (если источник есть).
/// </summary>
[DisallowMultipleComponent]
public class ParasitePulse : MonoBehaviour
{
    [Header("Пульсация")]
    [Tooltip("Минимальная пауза между пульсациями в секундах.")]
    [SerializeField, Min(0.1f)] private float pulseIntervalMin = 2f;

    [Tooltip("Максимальная пауза между пульсациями в секундах.")]
    [SerializeField, Min(0.1f)] private float pulseIntervalMax = 5f;

    [Tooltip("Длительность одной пульсации (надувание + сдувание) в секундах.")]
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.5f;

    [Tooltip("Пиковый масштаб во время пульсации. 1.2 = объект надувается на 20% от текущего размера.")]
    [SerializeField, Min(1f)] private float pulseScale = 1.2f;

    [Header("Звук")]
    [Tooltip("Список звуков пульсации. При каждой пульсации играет случайный клип из списка (если список не пуст).")]
    [SerializeField] private AudioClip[] pulseSounds;

    [Tooltip("AudioSource, через который играют звуки. Можно оставить пустым — тогда берётся AudioSource с этого же объекта, если он есть.")]
    [SerializeField] private AudioSource audioSource;

    private Coroutine pulseRoutine;
    private bool pulsing;

    // Состояние текущей пульсации (для корректного восстановления масштаба)
    private Vector3 pulseStartScale;
    private float pulseStartHealthFactor = 1f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        pulseRoutine = StartCoroutine(PulseLoop());
    }

    private void OnDisable()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        // Если выключились прямо во время пульсации — возвращаем масштаб
        // к текущему «здоровому» размеру (с учётом урона), без пульсации.
        if (pulsing)
        {
            pulsing = false;
            float factor = GetHealthFactor();
            transform.localScale = pulseStartScale * (factor / Mathf.Max(0.0001f, pulseStartHealthFactor));
        }
    }

    private IEnumerator PulseLoop()
    {
        while (true)
        {
            // Пауза до следующей пульсации выбирается случайно в заданном диапазоне.
            yield return new WaitForSeconds(GetRandomInterval());
            yield return PulseOnce();
        }
    }

    /// <summary>Случайная пауза между пульсациями.</summary>
    private float GetRandomInterval()
    {
        float min = Mathf.Min(pulseIntervalMin, pulseIntervalMax);
        float max = Mathf.Max(pulseIntervalMin, pulseIntervalMax);
        return Random.Range(min, max);
    }

    private IEnumerator PulseOnce()
    {
        Parasite parasite = GetComponentInParent<Parasite>();

        // Мёртвый/удаляемый паразит не пульсирует.
        if (parasite != null && parasite.IsRemoved)
            yield break;

        PlayPulseSound();

        // В момент старта пульсации масштаб объекта — «чистый» масштаб без пульсации
        // (с учётом уменьшения от здоровья). Запоминаем его и текущий фактор здоровья.
        pulseStartScale = transform.localScale;
        pulseStartHealthFactor = GetHealthFactor();
        pulsing = true;

        float halfDuration = Mathf.Max(0.025f, pulseDuration * 0.5f);

        // Надувание
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            ApplyPulsedScale(Mathf.Lerp(1f, pulseScale, t / halfDuration));
            yield return null;
        }

        // Сдувание
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            ApplyPulsedScale(Mathf.Lerp(pulseScale, 1f, t / halfDuration));
            yield return null;
        }

        ApplyPulsedScale(1f);
        pulsing = false;
    }

    /// <summary>
    /// Масштаб во время пульсации = базовый масштаб на старте пульсации,
    /// умноженный на текущий фактор здоровья (чтобы урон во время пульсации не терялся)
    /// и на огибающую пульсации.
    /// </summary>
    private void ApplyPulsedScale(float envelope)
    {
        float factor = GetHealthFactor();
        float healthRatio = factor / Mathf.Max(0.0001f, pulseStartHealthFactor);
        transform.localScale = pulseStartScale * healthRatio * envelope;
    }

    /// <summary>
    /// Проигрывает звук пульсации. Если задан массив Pulse Sounds — случайный клип из него
    /// через PlayOneShot; иначе обычный Play() (клип, назначенный на самом AudioSource).
    /// </summary>
    private void PlayPulseSound()
    {
        if (audioSource == null)
            return;

        if (pulseSounds != null && pulseSounds.Length > 0)
        {
            AudioClip clip = pulseSounds[Random.Range(0, pulseSounds.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
        else
        {
            audioSource.Play();
        }
    }

    /// <summary>Текущий масштаб паразита в долях от полного (1 = полный, учитывается урон).</summary>
    private float GetHealthFactor()
    {
        Parasite parasite = GetComponentInParent<Parasite>();
        if (parasite == null || parasite.MaxHealth <= 0f)
            return 1f;

        return Mathf.Clamp01(parasite.CurrentHealth / parasite.MaxHealth);
    }
}
