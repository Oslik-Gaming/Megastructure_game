using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Grabable : MonoBehaviour
{
    [Header("Weight")]
    [Min(0.01f)]
    [SerializeField] private float weight = 1f;

    [Header("Grab Sides")]
    [Tooltip("Empty child transforms. Their local Z axis must point out from the item.")]
    [SerializeField] private Transform[] grabSides;

    [Header("Place Bounce")]
    [Range(0f, 1f)]
    [SerializeField] private float bounceFactor = 0.25f;

    public float Weight => weight;
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
}
