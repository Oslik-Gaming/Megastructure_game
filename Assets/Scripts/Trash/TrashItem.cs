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

    private Rigidbody body;

    /// <summary>Вес предмета = масса Rigidbody (задаётся в одном месте — в инспекторе Rigidbody). Без Rigidbody — 1.</summary>
    public float Weight => body != null ? body.mass : 1f;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (removalAudioSource == null)
            removalAudioSource = GetComponent<AudioSource>();
    }

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
    protected virtual void OnAfterRemoved() { }

    /// <summary>Called when the item is reset for reuse. Override to restore initial state.</summary>
    protected virtual void OnResetForReuse() { }
}
