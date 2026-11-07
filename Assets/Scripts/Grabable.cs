using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Grabable : MonoBehaviour
{
    [Header("Grab Sides")]
    [Tooltip("Empty child transforms. Their local Z axis must point out from the item.")]
    [SerializeField] private Transform[] grabSides;

    [Header("Place Bounce")]
    [Range(0f, 1f)]
    [SerializeField] private float bounceFactor = 0.25f;

    [Header("Impact Sound")]
    [Tooltip("Список звуков удара о поверхность. Играет случайный клип при столкновении со скоростью выше Impact Min Speed.")]
    [SerializeField] private AudioClip[] impactSounds;

    [Tooltip("Минимальная скорость столкновения (м/с), при которой играет звук удара.")]
    [SerializeField, Min(0f)] private float impactMinSpeed = 1f;

    [Tooltip("AudioSource для звука удара. Пусто — берётся AudioSource с этого же объекта, если он есть.")]
    [SerializeField] private AudioSource impactAudioSource;

    /// <summary>Вес предмета = масса Rigidbody (задаётся в одном месте — в инспекторе Rigidbody).</summary>
    public float Weight => Body != null ? Body.mass : 1f;
    public Transform[] GrabSides => grabSides;
    public bool IsCarried { get; private set; }
    public Rigidbody Body { get; private set; }

    public event Action<Grabable> Grabbed;
    public event Action<Grabable> Released;

    private Collider[] ownColliders;
    private bool placeBounceEnabled;

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        ownColliders = GetComponentsInChildren<Collider>();

        if (impactAudioSource == null)
            impactAudioSource = GetComponent<AudioSource>();
    }

    public void BeginCarry()
    {
        IsCarried = true;
        Grabbed?.Invoke(this);
    }

    public void EndCarry()
    {
        IsCarried = false;
        placeBounceEnabled = false;
        Released?.Invoke(this);
    }

    public void EnablePlaceBounce()
    {
        placeBounceEnabled = true;
    }

    public void IgnoreCollisionsWith(Collider[] otherColliders, bool ignore)
    {
        if (otherColliders == null)
            return;

        foreach (Collider own in ownColliders)
        {
            if (own == null || own.isTrigger)
                continue;

            foreach (Collider other in otherColliders)
            {
                if (other == null || other.isTrigger)
                    continue;

                Physics.IgnoreCollision(own, other, ignore);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Звук удара о поверхность — работает при любом столкновении, включая удары при переносе и сбросе предмета.
        PlayImpactSound(collision);

        if (!placeBounceEnabled || Body.isKinematic || collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);
        if (Mathf.Abs(contact.normal.y) < 0.5f)
            return;

        float impact = Mathf.Abs(collision.relativeVelocity.y);
        if (impact > 0.2f)
        {
            Vector3 velocity = Body.linearVelocity;
            velocity.y = impact * bounceFactor;
            Body.linearVelocity = velocity;
        }

        placeBounceEnabled = false;
    }

    private void PlayImpactSound(Collision collision)
    {
        if (impactAudioSource == null || impactSounds == null || impactSounds.Length == 0)
            return;

        if (collision.relativeVelocity.magnitude < impactMinSpeed)
            return;

        AudioClip clip = impactSounds[UnityEngine.Random.Range(0, impactSounds.Length)];
        if (clip != null)
            impactAudioSource.PlayOneShot(clip);
    }
}
