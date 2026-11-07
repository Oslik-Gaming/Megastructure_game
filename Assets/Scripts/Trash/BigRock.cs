using UnityEngine;

/// <summary>
/// BigRock is a large, heavy trash item that cannot be grabbed.
/// It requires destruction tool to break it down through multiple stages.
/// </summary>
[DisallowMultipleComponent]
public class BigRock : DamageableTrashItem
{
    [Header("BigRock Visual Stages")]
    [Tooltip("Visual mesh for 100% health (pristine). Active at start.")]
    [SerializeField] private GameObject visualFull;

    [Tooltip("Visual mesh for 75% health or less.")]
    [SerializeField] private GameObject visual75;

    [Tooltip("Visual mesh for 50% health or less.")]
    [SerializeField] private GameObject visual50;

    [Tooltip("Visual mesh for 25% health or less.")]
    [SerializeField] private GameObject visual25;

    protected override void Awake()
    {
        base.Awake();
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
    /// Update which visual stage is active based on current health percentage.
    /// </summary>
    private void UpdateVisual()
    {
        float healthPercent = CurrentHealth / MaxHealth;

        // Deactivate all visuals first
        if (visualFull != null) visualFull.SetActive(false);
        if (visual75 != null) visual75.SetActive(false);
        if (visual50 != null) visual50.SetActive(false);
        if (visual25 != null) visual25.SetActive(false);

        // Activate the appropriate stage
        if (healthPercent > 0.75f)
        {
            if (visualFull != null) visualFull.SetActive(true);
        }
        else if (healthPercent > 0.5f)
        {
            if (visual75 != null) visual75.SetActive(true);
        }
        else if (healthPercent > 0.25f)
        {
            if (visual50 != null) visual50.SetActive(true);
        }
        else
        {
            if (visual25 != null) visual25.SetActive(true);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        // Ensure at least one visual is assigned
        if (visualFull == null && visual75 == null && visual50 == null && visual25 == null)
        {
            Debug.LogWarning($"[{gameObject.name}] BigRock has no visual stages assigned.", this);
        }
    }
#endif
}
