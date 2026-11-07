using UnityEngine;

/// <summary>
/// Универсальный звуковой компонент для инструментов.
/// Вешается на объект инструмента (рядом с ToolBase) — на каждый инструмент свой.
///
/// Звук работы (цикл):
///     играет, пока инструмент находится в состоянии Using (нажата ЛКМ, идёт нагрев,
///     луч деструктора, удерживается предмет гравизахватом — у каждого инструмента
///     это своё состояние). Как только использование прекращается — звук останавливается.
///
/// Звук взаимодействия (разовый):
///     случайный клип из списка. Для простых инструментов играет сам при начале
///     использования; для гравизахвата GravityTool может вызывать его вручную
///     (захват/установка/бросок) через PlayInteractRandom().
/// </summary>
[DisallowMultipleComponent]
public class ToolSoundPlayer : MonoBehaviour
{
    [Header("Звук работы (цикл)")]
    [Tooltip("Зацикленный звук работы инструмента (гул, жужжание). Пусто — звука работы нет.")]
    [SerializeField] private AudioClip useLoopSound;

    [Tooltip("Громкость звука работы.")]
    [SerializeField, Range(0f, 1f)] private float useLoopVolume = 1f;

    [Header("Звук взаимодействия (разовый)")]
    [Tooltip("Список разовых звуков взаимодействия. Играет случайный клип.")]
    [SerializeField] private AudioClip[] interactSounds;

    [Tooltip("Громкость звука взаимодействия.")]
    [SerializeField, Range(0f, 1f)] private float interactVolume = 1f;

    [Tooltip("AudioSource для всех звуков. Пусто — берётся AudioSource с этого же объекта.")]
    [SerializeField] private AudioSource audioSource;

    private ToolBase toolBase;
    private bool wasUsing;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        toolBase = GetComponent<ToolBase>();
    }

    private void Update()
    {
        if (toolBase == null)
            return;

        bool usingNow = toolBase.State == ToolState.Using;

        // Начало работы — разовый звук взаимодействия
        if (usingNow && !wasUsing)
            PlayInteractRandom();

        // Цикличный звук работы: включён, пока инструмент используется
        SetLoop(useLoopSound != null && usingNow);

        wasUsing = usingNow;
    }

    /// <summary>
    /// Включает/выключает цикличный звук работы вручную.
    /// Внутри Update флаг каждый кадр пересчитывается по состоянию инструмента,
    /// поэтому для инструментов без ToolBase (например, гравизахват во время удержания)
    /// вызывайте этот метод каждый кадр — последний вызов побеждает.
    /// </summary>
    public void SetLoop(bool on)
    {
        if (audioSource == null || useLoopSound == null)
            return;

        if (on)
        {
            if (audioSource.clip != useLoopSound || !audioSource.isPlaying)
            {
                audioSource.clip = useLoopSound;
                audioSource.loop = true;
                audioSource.volume = useLoopVolume;
                audioSource.Play();
            }
        }
        else if (audioSource.isPlaying && audioSource.clip == useLoopSound)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    /// <summary>Играет случайный звук взаимодействия из списка (если список задан).</summary>
    public void PlayInteractRandom()
    {
        if (audioSource == null || interactSounds == null || interactSounds.Length == 0)
            return;

        AudioClip clip = interactSounds[Random.Range(0, interactSounds.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip, interactVolume);
    }
}
