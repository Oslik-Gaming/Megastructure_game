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

    [Header("Shrink Axes")]
    [Tooltip("Уменьшать ли масштаб по оси X при потере здоровья.")]
    [SerializeField] private bool shrinkX = true;
    [Tooltip("Уменьшать ли масштаб по оси Y при потере здоровья (выключите, чтобы паразит не сплющивался по высоте).")]
    [SerializeField] private bool shrinkY = true;
    [Tooltip("Уменьшать ли масштаб по оси Z при потере здоровья.")]
    [SerializeField] private bool shrinkZ = true;

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
    /// Только по осям, разрешённым в Shrink Axes — остальные оси сохраняют исходный масштаб.
    /// </summary>
    private void UpdateScale()
    {
        if (visualRoot == null)
            return;

        float healthPercent = Mathf.Clamp01(CurrentHealth / MaxHealth);

        Vector3 scale = initialScale;
        if (shrinkX) scale.x = initialScale.x * healthPercent;
        if (shrinkY) scale.y = initialScale.y * healthPercent;
        if (shrinkZ) scale.z = initialScale.z * healthPercent;

        visualRoot.localScale = scale;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        // Preview scale in editor
        if (!Application.isPlaying && visualRoot != null)
        {
            // Don't change scale in edit mode to avoid confusion
        }
    }
#endif
}
