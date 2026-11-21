using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Input;

public sealed partial class VRPlayerRig
{
    [Header("Manipulator")]
    [SerializeField] private ToolController tools;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Vector3 handOffset;
    [SerializeField] private Vector3 handEuler;
    [SerializeField] private bool leftHanded;
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.25f;
    [SerializeField, Min(0f)] private float hapticSeconds = 0.06f;
    [SerializeField, Min(0f)] private float maxThrowHandSpeed = 8f;

    private Transform hand;
    private Vector3 previousHandPosition;
    private bool hasHandSample;
    public Transform Aim { get; private set; }
    public Transform Grip => leftHanded ? Left : Right;
    public Vector3 HandVelocity { get; private set; }

    private void MountTools()
    {
        hand = new GameObject("Robot Manipulator").transform;
        SetLeftHanded(leftHanded);
        Aim = muzzle == null ? hand : muzzle;
        if (tools == null) return;

        // Preserve the model's scale and alignment relative to the original view axes.
        Transform anchor = tools.transform;
        Vector3 originalScale = anchor.lossyScale;
        anchor.SetParent(hand, false);
        anchor.SetLocalPositionAndRotation(Vector3.zero, toolAlignment);
        anchor.localScale = originalScale;
        var bob = tools.GetComponent<ToolBobbing>();
        if (bob != null) bob.enabled = false;
    }

    private Quaternion toolAlignment;
    private void CaptureToolAlignment()
    {
        toolAlignment = tools == null ? Quaternion.identity : Quaternion.Inverse(Head.rotation) * tools.transform.rotation;
    }

    public void SetLeftHanded(bool value)
    {
        leftHanded = value;
        hand.SetParent(Grip, false);
        hand.SetLocalPositionAndRotation(handOffset, Quaternion.Euler(handEuler));
        hasHandSample = false;
        HandVelocity = Vector3.zero;
    }

    private void UpdateHand()
    {
        Vector3 p = TrackingSpace.InverseTransformPoint(Grip.position);
        HandVelocity = hasHandSample && Time.unscaledDeltaTime > 0f && !Transitioning
            ? TrackingSpace.TransformVector(Vector3.ClampMagnitude((p - previousHandPosition) / Time.unscaledDeltaTime, maxThrowHandSpeed))
            : Vector3.zero;
        previousHandPosition = p;
        hasHandSample = true;
    }

    public void PulseTool()
    {
        if (VRMode.Simulated || VRMode.GameplayBlocked || hapticAmplitude <= 0f || hapticSeconds <= 0f) return;
        var device = InputDevices.GetDeviceAtXRNode(leftHanded ? XRNode.LeftHand : XRNode.RightHand);
        if (device.isValid) OpenXRInput.SendHapticImpulse(device, hapticAmplitude, 0f, hapticSeconds);
    }
}
