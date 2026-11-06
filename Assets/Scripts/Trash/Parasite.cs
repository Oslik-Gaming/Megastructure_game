using UnityEngine;

/// <summary>
/// Parasite is a trash item that shrinks as it takes damage from the heater tool.
/// It cannot be grabbed and must be eliminated with the heater.
/// </summary>
[DisallowMultipleComponent]
public class Parasite : DamageableTrashItem
{
    [Header("Parasite Visual")]
    [Tooltip("Transform to scale. If null, scales the root object.")]
    [SerializeField] private Transform visualRoot;

    private Vector3 initialScale;

    protected override void Awake()
    {
        base.Awake();

        // Store initial scale
        if (visualRoot == null)
            visualRoot = transform;

        initialScale = visualRoot.localScale;
        UpdateScale();
    }

    protected override void OnDamageApplied(float damageAmount, float remainingHealth)
    {
        base.OnDamageApplied(damageAmount, remainingHealth);
        UpdateScale();
    }

    protected override void OnHealthRestored()
    {
        base.OnHealthRestored();
        UpdateScale();
    }

    /// <summary>
    /// Scale the visual proportionally to remaining health.
    /// </summary>
    private void UpdateScale()
    {
        if (visualRoot == null)
            return;

        float healthPercent = Mathf.Clamp01(CurrentHealth / MaxHealth);
        visualRoot.localScale = initialScale * healthPercent;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Preview scale in editor
        if (!Application.isPlaying && visualRoot != null)
        {
            // Don't change scale in edit mode to avoid confusion
        }
    }
#endif
}
