using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TrashItem : MonoBehaviour
{
    private Rigidbody body;

    /// <summary>Вес предмета = масса Rigidbody (задаётся в одном месте — в инспекторе Rigidbody). Без Rigidbody — 1.</summary>
    public float Weight => body != null ? body.mass : 1f;

    protected virtual void Awake() => body = GetComponent<Rigidbody>();

    [Tooltip("Generated automatically for scene objects. Do not edit manually.")]
    [SerializeField] private string persistentId;

    public float Weight => weight;
    public string PersistentId => persistentId;
    public TrashLocation HomeLocation { get; private set; }
    public bool IsRemoved { get; private set; }

    public event Action<TrashItem> Removed;

    protected virtual void Awake()
    {
        if (!string.IsNullOrEmpty(persistentId) && TrashPersistence.IsRemoved(persistentId))
        {
            // Keep the object in the hierarchy long enough for TrashLocation
            // to register it and include its original weight in the total.
            // It is excluded from RemainingWeight by IsRemoved.
            IsRemoved = true;
            gameObject.SetActive(false);
        }
    }

    public void SetLocation(TrashLocation location)
    {
        HomeLocation = location;
    }

    public void RestoreRemovedState()
    {
        if (IsRemoved)
            return;

        IsRemoved = true;
        gameObject.SetActive(false);
    }

    public void Remove()
    {
        if (IsRemoved)
            return;

        IsRemoved = true;
        Removed?.Invoke(this);
        OnAfterRemoved();
    }

    // Use this only when returning an item to an object pool.
    public void ResetForReuse()
    {
        IsRemoved = false;
        OnResetForReuse();
    }

    /// <summary>Called after the item has been removed. Override to add cleanup logic.</summary>
    protected virtual void OnAfterRemoved()
    {
        if (!string.IsNullOrEmpty(persistentId))
            TrashPersistence.MarkAsRemoved(persistentId);
    }

    /// <summary>Called when the item is reset for reuse. Override to restore initial state.</summary>
    protected virtual void OnResetForReuse() { }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        // Generate a stable ID for objects placed in a scene. Prefab assets and
        // runtime-created objects intentionally do not receive persistence IDs.
        if (string.IsNullOrEmpty(persistentId) &&
            gameObject.scene.IsValid() &&
            !UnityEditor.EditorUtility.IsPersistent(gameObject))
        {
            persistentId = Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
