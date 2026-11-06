using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Визуальный деструктор.
/// ЛКМ (ToolPrimary) показывает луч, пока кнопка удерживается.
/// </summary>
public class DestructorTool : MonoBehaviour
{
    [Header("Луч")]
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LineRenderer beamRenderer;
    [SerializeField, Min(0f)] private float beamLength = 50f;

    [Header("Урон")]
    [Tooltip("Слои, которые блокируют луч (окружение + цели).")]
    [SerializeField] private LayerMask beamBlockerMask = -1;
    [Tooltip("Урон в секунду, наносимый камням (BigRock).")]
    [SerializeField, Min(0f)] private float beamDamagePerSecond = 25f;

    private ToolBase toolBase;
    private ToolController toolController;
    private PlayerInputActions input;
    private bool primaryHeld;
    private bool primaryBlockedUntilRelease;

    private void Awake()
    {
        toolBase = GetComponent<ToolBase>();
        toolController = GetComponentInParent<ToolController>();
        if (beamRenderer == null)
            beamRenderer = GetComponent<LineRenderer>();
        input = new PlayerInputActions();

        if (toolBase == null)
            Debug.LogError("[DestructorTool] На объекте не найден ToolBase.", this);
        if (toolController == null)
            Debug.LogError("[DestructorTool] В родителях не найден ToolController.", this);
        if (beamOrigin == null)
            Debug.LogError("[DestructorTool] Не назначен Beam Origin.", this);
        if (beamRenderer == null)
            Debug.LogError("[DestructorTool] Не найден LineRenderer.", this);
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
        StopBeam();
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
            StopBeam();
            return;
        }

        if (!primaryHeld || primaryBlockedUntilRelease)
        {
            StopBeam();
            return;
        }

        if (toolBase.State == ToolState.Idle)
            toolBase.TryStartUsing();

        if (toolBase.State == ToolState.Using)
            UpdateBeam();
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
        StopBeam();
    }

    private bool IsActiveAndReady()
    {
        return toolBase != null && toolController != null &&
               toolController.ActiveTool == toolBase &&
               !toolController.IsSwitching &&
               toolBase.State == ToolState.Idle;
    }

    private void UpdateBeam()
    {
        if (beamRenderer == null || beamOrigin == null)
            return;

        beamRenderer.enabled = true;
        beamRenderer.positionCount = 2;
        beamRenderer.SetPosition(0, beamOrigin.position);

        // Raycast to find the first obstacle
        Vector3 endPoint;
        BigRock hitRock = null;

        if (Physics.Raycast(beamOrigin.position, beamOrigin.forward, out RaycastHit hit, beamLength, beamBlockerMask, QueryTriggerInteraction.Ignore))
        {
            // Hit something - end beam at hit point
            endPoint = hit.point;

            // Check if it's a BigRock
            hitRock = hit.collider.GetComponentInParent<BigRock>();
        }
        else
        {
            // Nothing hit - beam extends to max length
            endPoint = beamOrigin.position + beamOrigin.forward * beamLength;
        }

        beamRenderer.SetPosition(1, endPoint);

        // Apply damage to BigRock if found
        if (hitRock != null && !hitRock.IsRemoved)
        {
            float damage = beamDamagePerSecond * Time.deltaTime;
            hitRock.ApplyDamage(damage);
        }
    }

    private void StopBeam()
    {
        if (beamRenderer != null)
            beamRenderer.enabled = false;

        if (toolBase != null && toolBase.State == ToolState.Using)
            toolBase.StopUsing();
    }
}
