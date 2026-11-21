using UnityEngine;

public sealed partial class VRPlayerRig
{
    [Header("Turning / Room scale")]
    [SerializeField] private bool smoothTurn;
    [SerializeField, Min(1f)] private float snapDegrees = 45f;
    [SerializeField, Min(1f)] private float smoothDegreesPerSecond = 75f;
    [SerializeField, Range(0.1f, 1f)] private float turnThreshold = 0.7f;
    [SerializeField, Range(0f, 0.5f)] private float turnResetThreshold = 0.25f;
    [SerializeField, Min(0.01f)] private float headClearance = 0.12f;
    [SerializeField, Min(0.001f)] private float collisionSkin = 0.02f;
    [SerializeField] private LayerMask roomCollisionLayers = ~0;
    [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
    [SerializeField, Min(0f)] private float sprintSpeed = 3.5f;

    private PlayerInputActions turnInput;
    private bool turnLatched;
    private bool headObstructed;
    private readonly RaycastHit[] roomHits = new RaycastHit[32];
    private readonly Collider[] headHits = new Collider[32];

    public void SetSmoothTurn(bool value) => smoothTurn = value;
    public float MoveSpeed(bool sprint) => sprint ? sprintSpeed : walkSpeed;
    public Vector3 Foot => Vector3.ProjectOnPlane(capsule.transform.TransformPoint(capsule.center) - transform.position, transform.up)
        + transform.TransformPoint(Vector3.up * floorY);

    private void UpdateTurn()
    {
        float x = turnInput.Player.Turn.ReadValue<Vector2>().x;
        if (Mathf.Abs(x) <= turnResetThreshold) turnLatched = false;
        if (VRMode.GameplayBlocked) return;
        float degrees = 0f;
        if (smoothTurn)
        {
            if (Mathf.Abs(x) >= turnResetThreshold) degrees = x * smoothDegreesPerSecond * Time.deltaTime;
        }
        else if (!turnLatched && Mathf.Abs(x) >= turnThreshold)
        {
            turnLatched = true;
            degrees = Mathf.Sign(x) * snapDegrees;
        }
        if (degrees != 0f) RotateOrigin(Quaternion.AngleAxis(degrees, transform.up));
    }

    private void RotateOrigin(Quaternion delta)
    {
        Transform origin = rig.transform;
        origin.SetPositionAndRotation(VRMath.RotatePoint(origin.position, Head.position, delta), delta * origin.rotation);
    }

    private void SyncRoomScale()
    {
        Vector3 up = transform.up;
        Transform ct = capsule.transform;
        Vector3 center = ct.TransformPoint(capsule.center);
        float radius = capsule.radius * scale;
        Vector3 foot = transform.TransformPoint(Vector3.up * floorY);
        Vector3 head = Head.position;
        Vector3 shift = Vector3.ProjectOnPlane(head - center, up);
        float halfSegment = Mathf.Max(0f, capsule.height * scale * 0.5f - radius);
        Vector3 bottom = center - up * halfSegment;
        Vector3 top = center + up * halfSegment;
        float allowed = shift.magnitude;
        int mask = roomCollisionLayers;
        float castRadius = Mathf.Max(0.001f, radius - collisionSkin);
        if (allowed > 0.0001f)
        {
            int hits = Physics.CapsuleCastNonAlloc(bottom, top, castRadius, shift.normalized,
                roomHits, allowed + collisionSkin, mask, QueryTriggerInteraction.Ignore);
            allowed = LimitRoomShift(allowed, hits);
        }
        Vector3 correction = shift.normalized * allowed - shift;
        rig.transform.position += correction;
        head += correction;
        float height = Mathf.Max(radius * 2f, Vector3.Dot(head - foot, up) + headClearance);
        float rise = height - capsule.height * scale;
        if (rise > 0f)
        {
            int hits = Physics.SphereCastNonAlloc(top + shift + correction, castRadius, up, roomHits,
                rise + collisionSkin, mask, QueryTriggerInteraction.Ignore);
            height = capsule.height * scale + LimitRoomShift(rise, hits);
        }
        Vector3 newCenter = center + shift + correction;
        newCenter += up * (height * 0.5f - Vector3.Dot(newCenter - foot, up));
        capsule.height = height / scale;
        capsule.center = ct.InverseTransformPoint(newCenter);
        headObstructed = IsHeadObstructed(head);
    }

    private bool IsHeadObstructed(Vector3 head)
    {
        int count = Physics.OverlapSphereNonAlloc(head, headClearance, headHits, roomCollisionLayers, QueryTriggerInteraction.Ignore);
        if (count == headHits.Length) return true;
        for (int i = 0; i < count; i++) if (IsRoomObstacle(headHits[i])) return true;
        return false;
    }

    private float LimitRoomShift(float distance, int count)
    {
        if (count == roomHits.Length) return 0f;
        for (int i = 0; i < count; i++)
            if (IsRoomObstacle(roomHits[i].collider)) distance = Mathf.Min(distance, Mathf.Max(0f, roomHits[i].distance - collisionSkin));
        return distance;
    }

    private bool IsRoomObstacle(Collider collider) => collider != null && collider.attachedRigidbody != body &&
        !collider.transform.IsChildOf(transform) && !Physics.GetIgnoreLayerCollision(capsule.gameObject.layer, collider.gameObject.layer);
}
