using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TrashItem : MonoBehaviour
{
    [Header("Removal Sound")]
    [Tooltip("Список звуков разрушения/удаления. Играет случайный клип при вызове Remove().")]
    [SerializeField] private AudioClip[] removalSounds;

    [Tooltip("AudioSource для звука разрушения. Пусто — звук сыграет в точке объекта как временный 3D-источник (громкость 1).")]
    [SerializeField] private AudioSource removalAudioSource;

    [Header("Weight")]
    [Tooltip("Вес мусора для HUD-индикатора чистоты локации (TrashLocation). Не связан с физикой — масса задаётся в Rigidbody.")]
    [Min(0.0001f)]
    [SerializeField] private float weight = 1f;

    [SerializeField, HideInInspector] private string persistentId;

    /// <summary>Игровой вес предмета для HUD-индикатора чистоты локации (не масса Rigidbody).</summary>
    public float Weight => weight;

    protected virtual void Awake()
    {
        if (removalAudioSource == null)
            removalAudioSource = GetComponent<AudioSource>();

        if (!string.IsNullOrEmpty(persistentId) && TrashPersistence.IsRemoved(persistentId))
        {
            // Keep the object in the hierarchy long enough for TrashLocation
            // to register it and include its original weight in the total.
            // It is excluded from RemainingWeight by IsRemoved.
            IsRemoved = true;
            gameObject.SetActive(false);
        }
    }

    public string PersistentId => persistentId;
    public TrashLocation HomeLocation { get; private set; }
    public bool IsRemoved { get; private set; }

    public event Action<TrashItem> Removed;

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
        PlayRemovalSound();
        Removed?.Invoke(this);
        OnAfterRemoved();
    }

    // Use this only when returning an item to an object pool.
    public void ResetForReuse()
    {
        IsRemoved = false;
        OnResetForReuse();
    }

    private void PlayRemovalSound()
    {
        if (removalSounds == null || removalSounds.Length == 0)
            return;

        AudioClip clip = removalSounds[UnityEngine.Random.Range(0, removalSounds.Length)];
        if (clip == null)
            return;

        if (removalAudioSource != null)
            removalAudioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
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

            // Для префаб-инстансов сгенерированный ID нужно записать как
            // override инстанса, иначе он не сохранится в файл сцены и
            // состояние удаления такого объекта не будет восстанавливаться.
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
    }
#endif
}
