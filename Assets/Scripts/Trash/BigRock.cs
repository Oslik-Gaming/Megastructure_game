using UnityEngine;

/// <summary>
/// BigRock is a large, heavy trash item that cannot be grabbed.
/// It requires destruction tool to break it down through multiple stages.
/// </summary>
[DisallowMultipleComponent]
public class BigRock : DamageableTrashItem
{
    [Header("BigRock Visual Stages")]
    [Tooltip("Объекты стадий разрушения, от целого (100% здоровья) к полностью разрушенному. " +
             "Каждый элемент — либо дочерний объект сцены, либо ПРЕФАБ: префабы автоматически инстанцируются " +
             "при старте, поэтому на сцене ничего создавать не нужно. " +
             "Пороги стадий делят диапазон здоровья равномерно и автоматически.")]
    [SerializeField] private GameObject[] destructionStages = new GameObject[0];

    [Header("Stage Transition Sound")]
    [Tooltip("Звуки перехода между стадиями разрушения. При каждой смене стадии играет случайный клип из списка.")]
    [SerializeField] private AudioClip[] stageTransitionSounds;

    [Tooltip("Громкость звука перехода между стадиями.")]
    [Range(0f, 1f)]
    [SerializeField] private float stageTransitionVolume = 1f;

    [Tooltip("AudioSource для звука перехода. Пусто — берётся AudioSource с этого же объекта, если он есть; " +
             "если его нет — временный 3D-источник в точке камня.")]
    [SerializeField] private AudioSource stageTransitionAudioSource;

    private GameObject[] stageInstances;
    private int currentStageIndex = -1;

    protected override void Awake()
    {
        base.Awake();
        CreateStageInstances();

        if (stageTransitionAudioSource == null)
            stageTransitionAudioSource = GetComponent<AudioSource>();

        UpdateVisual();
    }

    protected override void OnDamageApplied(float damageAmount, float remainingHealth)
    {
        base.OnDamageApplied(damageAmount, remainingHealth);
        UpdateVisual();
    }

    protected override void OnHealthRestored()
    {
        base.OnHealthRestored();
        UpdateVisual();
    }

    /// <summary>
    /// Порядок стадии: 0 = исходный (100% здоровья), последняя = полностью разрушенный.
    /// Пороги делят диапазон здоровья равномерно: 4 стадии → >75%, >50%, >25%, иначе.
    /// </summary>
    private int GetCurrentStageIndex()
    {
        int stageCount = destructionStages.Length;
        if (stageCount == 0)
            return -1;

        float healthPercent = Mathf.Clamp01(CurrentHealth / MaxHealth);
        for (int i = 0; i < stageCount - 1; i++)
        {
            if (healthPercent > (stageCount - 1 - i) / (float)stageCount)
                return i;
        }
        return stageCount - 1;
    }

    /// <summary>
    /// Активирует только стадию, соответствующую текущему здоровью.
    /// При смене стадии проигрывает случайный звук перехода.
    /// </summary>
    private void UpdateVisual()
    {
        int stageIndex = GetCurrentStageIndex();
        if (stageIndex == currentStageIndex)
            return;

        bool hadStage = currentStageIndex >= 0;
        currentStageIndex = stageIndex;

        if (stageInstances != null)
        {
            for (int i = 0; i < stageInstances.Length; i++)
            {
                GameObject stage = stageInstances[i];
                if (stage != null)
                    stage.SetActive(i == stageIndex);
            }
        }

        if (hadStage)
            PlayStageTransitionSound();
    }

    /// <summary>
    /// Подготовка стадий: дочерние объекты сцены используются напрямую,
    /// префабы инстанцируются под камень (сцена не засоряется ручными объектами).
    /// </summary>
    private void CreateStageInstances()
    {
        if (destructionStages.Length == 0)
            return;

        stageInstances = new GameObject[destructionStages.Length];
        for (int i = 0; i < destructionStages.Length; i++)
        {
            GameObject stage = destructionStages[i];
            if (stage == null)
                continue;

            if (stage.gameObject.scene.IsValid())
            {
                // Дочерний объект сцены — используем напрямую.
                stageInstances[i] = stage;
                stage.SetActive(false);
            }
            else
            {
                // Префаб — инстанцируем под камень.
                GameObject instance = Instantiate(stage, transform);
                instance.name = stage.name;
                stageInstances[i] = instance;
            }
        }
    }

    private void PlayStageTransitionSound()
    {
        if (stageTransitionSounds == null || stageTransitionSounds.Length == 0)
            return;

        AudioClip clip = stageTransitionSounds[UnityEngine.Random.Range(0, stageTransitionSounds.Length)];
        if (clip == null)
            return;

        if (stageTransitionAudioSource != null)
            stageTransitionAudioSource.PlayOneShot(clip, stageTransitionVolume);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position, stageTransitionVolume);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure at least one visual is assigned
        if (destructionStages == null || destructionStages.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] BigRock has no visual stages assigned.", this);
        }
    }
#endif
}
