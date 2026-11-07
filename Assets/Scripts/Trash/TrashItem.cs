using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TrashItem : MonoBehaviour
{
    private Rigidbody body;

    /// <summary>Вес предмета = масса Rigidbody (задаётся в одном месте — в инспекторе Rigidbody). Без Rigidbody — 1.</summary>
    public float Weight => body != null ? body.mass : 1f;

    protected virtual void Awake() => body = GetComponent<Rigidbody>();

    public TrashLocation HomeLocation { get; private set; }
    public bool IsRemoved { get; private set; }

    public event Action<TrashItem> Removed;

    public void SetLocation(TrashLocation location)
    {
        HomeLocation = location;
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
    protected virtual void OnAfterRemoved() { }

    /// <summary>Called when the item is reset for reuse. Override to restore initial state.</summary>
    protected virtual void OnResetForReuse() { }
}
