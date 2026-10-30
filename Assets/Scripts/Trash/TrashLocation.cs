using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrashLocation : MonoBehaviour
{
    [Tooltip("Корень со всеми объектами мусора. Если не задан, используется этот объект.")]
    [SerializeField] private Transform trashRoot;
    [SerializeField] private bool collectOnAwake = true;

    private readonly HashSet<TrashItem> registeredItems = new HashSet<TrashItem>();
    private readonly HashSet<TrashItem> activeItems = new HashSet<TrashItem>();

    public float InitialWeight { get; private set; }
    public float RemainingWeight { get; private set; }
    public float RemainingPercent => InitialWeight <= 0f
        ? 0f
        : Mathf.Clamp01(RemainingWeight / InitialWeight) * 100f;
    public float CleanedPercent => 100f - RemainingPercent;

    public event Action Changed;

    private void Awake()
    {
        if (collectOnAwake)
            CollectInitialTrash();
    }

    [ContextMenu("Collect Initial Trash")]
    public void CollectInitialTrash()
    {
        UnregisterAll();

        Transform root = trashRoot != null ? trashRoot : transform;
        foreach (TrashItem item in root.GetComponentsInChildren<TrashItem>(true))
            Register(item);

        NotifyChanged();
    }

    public void RegisterRuntimeTrash(TrashItem item)
    {
        Register(item);
        NotifyChanged();
    }

    private void Register(TrashItem item)
    {
        if (item == null || !registeredItems.Add(item))
            return;

        item.SetLocation(this);
        item.Removed += HandleTrashRemoved;

        InitialWeight += item.Weight;
        if (!item.IsRemoved)
        {
            activeItems.Add(item);
            RemainingWeight += item.Weight;
        }
    }

    private void HandleTrashRemoved(TrashItem item)
    {
        if (item == null || !activeItems.Remove(item))
            return;

        RemainingWeight = Mathf.Max(0f, RemainingWeight - item.Weight);
        NotifyChanged();
    }

    private void UnregisterAll()
    {
        foreach (TrashItem item in registeredItems)
        {
            if (item != null)
                item.Removed -= HandleTrashRemoved;
        }

        registeredItems.Clear();
        activeItems.Clear();
        InitialWeight = 0f;
        RemainingWeight = 0f;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }

    private void OnDestroy()
    {
        UnregisterAll();
    }
}
