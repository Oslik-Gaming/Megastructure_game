using System.Collections;
using UnityEngine;

public sealed partial class VRPlayerRig
{
    [Header("Comfort")]
    [SerializeField, Min(0f)] private float fadeSeconds = 0.15f;
    [SerializeField] private bool movementVignette = true;
    [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.65f;
    [SerializeField, Min(0f)] private float lethalFallDistance = 2.5f;

    private Material comfortMaterial;
    private Mesh comfortMesh;
    private float fade;
    private static readonly int FadeProperty = Shader.PropertyToID("_Fade");
    private static readonly int VignetteProperty = Shader.PropertyToID("_Vignette");
    private float airborneStart;
    private bool wasAirborne;
    private PlayerMovements movement;

    public void SetMovementVignette(bool value) => movementVignette = value;
    public void ToggleVignette() => SetMovementVignette(!movementVignette);
    public void ToggleTurnMode() => SetSmoothTurn(!smoothTurn);
    public void ToggleHand() => SetLeftHanded(!leftHanded);
    public void ToggleSeated() => ConfigureHeight(!seated);

    private void InitializeComfort()
    {
        movement = GetComponent<PlayerMovements>();
        var shader = Resources.Load<Shader>("VRComfortOverlay");
        if (shader == null) return;
        var overlay = new GameObject("Stereo comfort overlay", typeof(MeshFilter), typeof(MeshRenderer));
        overlay.transform.SetParent(Head, false);
        comfortMesh = new Mesh { name = "XR comfort triangle" };
        comfortMesh.vertices = new[] {new Vector3(-1,-1,1), new Vector3(3,-1,1), new Vector3(-1,3,1)};
        comfortMesh.triangles = new[] {0,1,2};
        comfortMesh.bounds = new Bounds(Vector3.forward, Vector3.one * 8f);
        overlay.GetComponent<MeshFilter>().sharedMesh = comfortMesh;
        var renderer = overlay.GetComponent<MeshRenderer>();
        comfortMaterial = new Material(shader);
        renderer.sharedMaterial = comfortMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        comfortRenderer = renderer;
    }

    private Renderer comfortRenderer;
    private void UpdateComfort()
    {
        float down = Vector3.Dot(body.position, -transform.up);
        if (!movement.IsOnGround && !Transitioning && Time.timeScale > 0f)
        {
            if (!wasAirborne) airborneStart = down;
            wasAirborne = true;
            if (lethalFallDistance > 0f && down - airborneStart > lethalFallDistance && CheckpointManager.Instance != null)
                CheckpointManager.KillPlayer();
        }
        else wasAirborne = false;

        if (comfortMaterial == null) return;
        float speed = Vector3.ProjectOnPlane(body.linearVelocity, transform.up).magnitude;
        float vignette = movementVignette && !VRMode.GameplayBlocked ? Mathf.Clamp01(speed / Mathf.Max(0.01f, MoveSpeed(false))) * vignetteStrength : 0f;
        comfortMaterial.SetFloat(FadeProperty, headObstructed ? 1f : fade);
        comfortMaterial.SetFloat(VignetteProperty, vignette);
        comfortRenderer.enabled = headObstructed || fade > 0f || vignette > 0f;
    }

    public IEnumerator FadeTo(float alpha)
    {
        float start = fade;
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            fade = Mathf.Lerp(start, alpha, t / fadeSeconds);
            yield return null;
        }
        fade = alpha;
        yield return null; // guarantee one rendered frame at target alpha before teleport
    }

    public void SetTransitioning(bool value) => Transitioning = value;

    private void OnEnable() => Application.onBeforeRender += CheckLastTrackedHead;

    private void OnDisable() => Application.onBeforeRender -= CheckLastTrackedHead;

    [BeforeRenderOrder(1000)]
    private void CheckLastTrackedHead()
    {
        if (comfortMaterial == null) return;
        // Just before rendering the driver may supply a newer pose than LateUpdate saw.
        // Never replace the physical pose; hide geometry if it overlaps the tracked eye.
        bool obstructed = headObstructed || IsHeadObstructed(Head.position);
        comfortMaterial.SetFloat(FadeProperty, obstructed ? 1f : fade);
        if (obstructed) comfortRenderer.enabled = true;
    }

    private void DisposeComfort()
    {
        if (comfortMaterial != null) Destroy(comfortMaterial);
        if (comfortMesh != null) Destroy(comfortMesh);
    }
}
