using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Визуальный выжигатель.
/// ЛКМ (ToolPrimary) показывает сферическую зону нагрева, пока кнопка удерживается.
/// </summary>
public class HeaterTool : MonoBehaviour
{
    [Header("Визуальная зона нагрева")]
    [SerializeField] private GameObject heatZoneVisual;
    [SerializeField, Min(0f)] private float heatRadius = 1.5f;

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
        if (heatZoneVisual == null)
            Debug.LogError("[HeaterTool] Не назначен Heat Zone Visual.", this);

        ApplyHeatZoneScale();
        SetHeatZoneVisible(false);
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
            SetHeatZoneVisible(true);
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
        SetHeatZoneVisible(false);

        if (toolBase != null && toolBase.State == ToolState.Using)
            toolBase.StopUsing();
    }

    private void ApplyHeatZoneScale()
    {
        if (heatZoneVisual != null)
            heatZoneVisual.transform.localScale = Vector3.one * (heatRadius * 2f);
    }

    private void SetHeatZoneVisible(bool visible)
    {
        if (heatZoneVisual != null && heatZoneVisual.activeSelf != visible)
            heatZoneVisual.SetActive(visible);
    }

    private void OnValidate()
    {
        ApplyHeatZoneScale();
    }
}
