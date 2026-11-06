using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Визуальный выжигатель.
/// ЛКМ (ToolPrimary) показывает сферическую зону нагрева, пока кнопка удерживается.
/// </summary>
public class HeaterTool : MonoBehaviour
{
    [Header("Конус нагрева")]
    [SerializeField] private Transform heatZoneOrigin;
    [SerializeField] private HeaterConeVisual coneVisual;
    [SerializeField, Min(0.1f)] private float coneLength = 3f;
    [SerializeField, Range(5f, 120f)] private float coneOpeningAngle = 45f;

    [Header("Урон")]
    [Tooltip("Урон в секунду, наносимый паразитам.")]
    [SerializeField, Min(0f)] private float damagePerSecond = 20f;
    [Tooltip("Множитель урона в зависимости от расстояния (0 = у вершины, 1 = у основания).")]
    [SerializeField] private AnimationCurve distanceDamageMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

    private Collider[] overlapBuffer = new Collider[32];

    private ToolBase toolBase;
    private ToolController toolController;
    private PlayerInputActions input;
    private bool primaryHeld;
    private bool primaryBlockedUntilRelease;

    private void Awake()
    {
        toolBase = GetComponent<ToolBase>();
        toolController = GetComponentInParent<ToolController>();
        input = new PlayerInputActions();

        if (toolBase == null)
            Debug.LogError("[HeaterTool] На объекте не найден ToolBase.", this);
        if (toolController == null)
            Debug.LogError("[HeaterTool] В родителях не найден ToolController.", this);
        if (heatZoneOrigin == null)
            Debug.LogError("[HeaterTool] Не назначен Heat Zone Origin.", this);
        if (coneVisual == null)
            Debug.LogError("[HeaterTool] Не назначен Cone Visual.", this);

        ApplyConeParameters();
        SetConeVisible(false);
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.ToolPrimary.started += OnPrimaryStarted;
        input.Player.ToolPrimary.canceled += OnPrimaryCanceled;
    }

    private void OnDisable()
    {
        input.Player.ToolPrimary.started -= OnPrimaryStarted;
        input.Player.ToolPrimary.canceled -= OnPrimaryCanceled;
        input.Player.Disable();
        primaryHeld = false;
        StopHeating();
    }

    private void OnDestroy() => input.Dispose();

    private void Update()
    {
        if (toolBase == null || toolController == null)
            return;

        if (toolController.ActiveTool != toolBase || toolController.IsSwitching)
        {
            if (primaryHeld)
                primaryBlockedUntilRelease = true;
            StopHeating();
            return;
        }

        if (!primaryHeld || primaryBlockedUntilRelease)
        {
            StopHeating();
            return;
        }

        if (toolBase.State == ToolState.Idle)
            toolBase.TryStartUsing();

        if (toolBase.State == ToolState.Using)
        {
            SetConeVisible(true);
            ApplyDamageToParasites();
        }
    }

    private void OnPrimaryStarted(InputAction.CallbackContext context)
    {
        primaryHeld = true;
        if (!IsActiveAndReady())
            primaryBlockedUntilRelease = true;
    }

    private void OnPrimaryCanceled(InputAction.CallbackContext context)
    {
        primaryHeld = false;
        primaryBlockedUntilRelease = false;
        StopHeating();
    }

    private bool IsActiveAndReady()
    {
        return toolBase != null && toolController != null &&
               toolController.ActiveTool == toolBase &&
               !toolController.IsSwitching &&
               toolBase.State == ToolState.Idle;
    }

    private void StopHeating()
    {
        SetConeVisible(false);

        if (toolBase != null && toolBase.State == ToolState.Using)
            toolBase.StopUsing();
    }

    private void ApplyConeParameters()
    {
        if (coneVisual != null)
            coneVisual.SetConeParameters(coneLength, coneOpeningAngle);
    }

    private void SetConeVisible(bool visible)
    {
        if (coneVisual != null && coneVisual.gameObject.activeSelf != visible)
            coneVisual.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Find all parasites in cone and apply damage.
    /// Uses OverlapSphere for broad phase, then mathematical cone test.
    /// </summary>
    private void ApplyDamageToParasites()
    {
        if (heatZoneOrigin == null)
            return;

        Vector3 origin = heatZoneOrigin.position;
        Vector3 forward = heatZoneOrigin.forward;
        float baseRadius = coneLength * Mathf.Tan(coneOpeningAngle * 0.5f * Mathf.Deg2Rad);

        // Broad phase: sphere overlap
        int hitCount = Physics.OverlapSphereNonAlloc(origin, coneLength, overlapBuffer, -1, QueryTriggerInteraction.Ignore);

        // Collect unique parasites (avoid duplicates from multiple colliders)
        System.Collections.Generic.HashSet<Parasite> hitParasites = new System.Collections.Generic.HashSet<Parasite>();

        for (int i = 0; i < hitCount; i++)
        {
            Parasite parasite = overlapBuffer[i].GetComponentInParent<Parasite>();
            if (parasite != null && !parasite.IsRemoved)
            {
                hitParasites.Add(parasite);
            }
        }

        // Narrow phase: cone test and damage application
        foreach (Parasite parasite in hitParasites)
        {
            Vector3 targetPos = parasite.transform.position;
            Vector3 toTarget = targetPos - origin;

            // Check if target is in front of origin
            float distanceAlongAxis = Vector3.Dot(toTarget, forward);
            if (distanceAlongAxis < 0f || distanceAlongAxis > coneLength)
                continue;

            // Check if target is within cone radius at this distance
            Vector3 pointOnAxis = origin + forward * distanceAlongAxis;
            float distanceFromAxis = Vector3.Distance(targetPos, pointOnAxis);
            float maxRadiusAtDistance = baseRadius * (distanceAlongAxis / coneLength);

            if (distanceFromAxis <= maxRadiusAtDistance)
            {
                // Target is inside cone - apply damage
                float normalizedDistance = distanceAlongAxis / coneLength;
                float damageMultiplier = distanceDamageMultiplier.Evaluate(normalizedDistance);
                float damage = damagePerSecond * damageMultiplier * Time.deltaTime;

                parasite.ApplyDamage(damage);
            }
        }
    }

    private void OnValidate()
    {
        ApplyConeParameters();
    }
}
