using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(PlayerMovements), typeof(Rigidbody))]
public sealed partial class VRPlayerRig : MonoBehaviour
{
    [SerializeField] private VRRigReferences rigPrefab;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private GameObject[] hideInVR;
    [SerializeField] private bool simulatorOnly;
    [SerializeField, Min(0.1f)] private float deviceEyeHeight = 1.65f;
    [SerializeField] private bool seated;

    private VRRigReferences rig;
    private Rigidbody body;
    private float floorY;
    private float scale;

    public static VRPlayerRig Current { get; private set; }
    public Transform Head => gameplayCamera.transform;
    public Transform Left => rig.leftController;
    public Transform Right => rig.rightController;
    public Transform TrackingSpace => rig.cameraOffset;
    public bool Transitioning { get; private set; }

    private void Awake()
    {
        if (simulatorOnly && !VRMode.Active) VRMode.Simulated = true;
        if (!VRMode.Active || rigPrefab == null || gameplayCamera == null || capsule == null)
        {
            enabled = false;
            return;
        }

        Current = this;
        body = GetComponent<Rigidbody>();
        scale = Mathf.Abs(transform.lossyScale.y);
        floorY = transform.InverseTransformPoint(capsule.transform.TransformPoint(capsule.center - Vector3.up * (capsule.height * 0.5f))).y;
        rig = Instantiate(rigPrefab, transform, false);
        rig.transform.localPosition = Vector3.up * floorY;
        // The existing robot FBX is scaled to 0.06. XR tracking units must remain meters.
        rig.transform.localScale = Vector3.one / scale;
        CaptureToolAlignment();
        Head.SetParent(rig.cameraOffset, false);
        Head.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        Head.localScale = Vector3.one;
        gameplayCamera.tag = "MainCamera";
        gameplayCamera.cullingMask = ~0;
        gameplayCamera.targetTexture = null;
        gameplayCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        AddTracking(Head, "<XRHMD>", "centerEye");
        rig.origin.Camera = gameplayCamera;
        ConfigureHeight(seated);
        foreach (var item in hideInVR)
            if (item != null) item.SetActive(false);
        // Renderers on the robot body are not first-person assets. Tool renderers are preserved.
        MountTools();
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (hand == null || !renderer.transform.IsChildOf(hand)) renderer.forceRenderingOff = true;
        ConfigureRendering();
        rig.gameObject.SetActive(true);
        turnInput = VRMode.CreateInput();
        turnInput.Player.Enable();
        InitializeComfort();
    }

    private void LateUpdate()
    {
        UpdateTurn();
        SyncRoomScale();
        UpdateHand();
        UpdateComfort();
    }

    public void ConfigureHeight(bool useSeatedMode)
    {
        seated = useSeatedMode;
        rig.origin.CameraYOffset = deviceEyeHeight;
        bool device = seated || VRMode.Simulated;
        rig.origin.RequestedTrackingOriginMode = device ? XROrigin.TrackingOriginMode.Device : XROrigin.TrackingOriginMode.Floor;
        rig.cameraOffset.localPosition = device ? Vector3.up * deviceEyeHeight : Vector3.zero;
    }

    public static void AddTracking(Transform target, string device, string posePrefix)
    {
        var driver = target.gameObject.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value,
            device + "/" + posePrefix + "Position", expectedControlType: "Vector3"));
        driver.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value,
            device + "/" + posePrefix + "Rotation", expectedControlType: "Quaternion"));
        driver.trackingStateInput = new InputActionProperty(new InputAction("Tracking State", InputActionType.Value,
            device + "/trackingState", expectedControlType: "Integer"));
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
    }

    private void OnDestroy()
    {
        DisposeComfort();
        turnInput?.Dispose();
        if (Current == this) Current = null;
        if (simulatorOnly) VRMode.Simulated = false;
    }
}
