using UnityEngine;

public static class VRMath
{
    public static Vector3 Move(Vector2 stick, Vector3 headForward, Vector3 headRight, Vector3 up)
    {
        Vector3 forward = Vector3.ProjectOnPlane(headForward, up);
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.Cross(headRight, up);
        forward.Normalize();
        return Vector3.ClampMagnitude(forward * stick.y + Vector3.Cross(up, forward) * stick.x, 1f);
    }

    public static Vector3 RotatePoint(Vector3 point, Vector3 pivot, Quaternion delta) => pivot + delta * (point - pivot);
}
