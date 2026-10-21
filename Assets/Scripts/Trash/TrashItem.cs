using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TrashItem : MonoBehaviour
{
    [Tooltip("Вес мусора. Если весь мусор одинаковый, оставьте 1.")]
    [Min(0.0001f)]
    [SerializeField] private float weight = 1f;

    public float Weight => weight;
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
    }

    // Use this only when returning an item to an object pool.
    public void ResetForReuse()
    {
        IsRemoved = false;
    }
}
