using UnityEngine;

/// <summary>
/// Base class for trash items that can take damage and be destroyed.
/// Handles health tracking, damage application, and removal when health reaches zero.
/// </summary>
public abstract class DamageableTrashItem : TrashItem
{
    [Header("Damage")]
    [Tooltip("Maximum health. The item is destroyed when health reaches zero.")]
    [SerializeField] protected float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    protected override void Awake()
    {
        base.Awake(); // TrashItem: инициализация звука и состояния удаления

        currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage to this item. Ignores zero or negative damage.
    /// When health reaches zero, the item is removed and destroyed.
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || IsRemoved)
            return;

        currentHealth -= damage;

        OnDamageApplied(damage, currentHealth);

        if (currentHealth <= 0f)
        {
            Remove();
        }
    }

    /// <summary>
    /// Called when damage is applied. Override to add visual or audio feedback.
    /// </summary>
    /// <param name="damageAmount">The amount of damage applied this frame.</param>
    /// <param name="remainingHealth">Health after damage.</param>
    protected virtual void OnDamageApplied(float damageAmount, float remainingHealth) { }

    protected override void OnAfterRemoved()
    {
        base.OnAfterRemoved();

        // Destroy the GameObject after a short delay to allow events to complete
        Destroy(gameObject, 0.1f);
    }

    protected override void OnResetForReuse()
    {
        base.OnResetForReuse();
        currentHealth = maxHealth;
        OnHealthRestored();
    }

    /// <summary>
    /// Called when health is restored (e.g., when reset for reuse).
    /// Override to restore visual state.
    /// </summary>
    protected virtual void OnHealthRestored() { }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        // Warn about duplicate IDs
        if (!string.IsNullOrEmpty(PersistentId))
        {
            var allItems = FindObjectsOfType<DamageableTrashItem>();
            int duplicateCount = 0;
            foreach (var item in allItems)
            {
                if (item != this && item.PersistentId == PersistentId)
                    duplicateCount++;
            }

            if (duplicateCount > 0)
            {
                Debug.LogWarning($"[{gameObject.name}] Duplicate Persistent ID '{PersistentId}' detected. Each ID must be unique within the scene.", this);
            }
        }
    }
#endif
}
