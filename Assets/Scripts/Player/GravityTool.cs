using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ToolPrimary (ЛКМ): захват или установка.
/// ToolSecondary (ПКМ): бросок.
/// </summary>
public class GravityTool : MonoBehaviour
{
    private ToolBase toolBase;
    private ToolController toolController;
    private PlayerRaycastInteract playerInteract;
    private PlayerInputActions input;

    private void Awake()
    {
        toolBase = GetComponent<ToolBase>();
        toolController = GetComponentInParent<ToolController>();
        playerInteract = GetComponentInParent<PlayerRaycastInteract>();
        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.ToolPrimary.performed += OnPrimaryPerformed;
        input.Player.ToolSecondary.performed += OnSecondaryPerformed;
    }

    private void OnDisable()
    {
        input.Player.ToolPrimary.performed -= OnPrimaryPerformed;
        input.Player.ToolSecondary.performed -= OnSecondaryPerformed;
        input.Player.Disable();
    }

    private void OnDestroy() => input.Dispose();

    private void Update()
    {
        if (toolBase == null || toolController == null || playerInteract == null)
            return;

        // Если гравизахват больше не активный инструмент (переключились на другой) —
        // удерживаемый предмет просто падает, а не остаётся висеть в воздухе.
        if (playerInteract.HasCarriedItem &&
            (toolController.ActiveTool != toolBase || toolController.IsSwitching))
        {
            playerInteract.DropHeldObject();
        }
    }

    private bool IsActiveAndReady()
    {
        return toolBase != null && toolController != null && playerInteract != null &&
               toolController.ActiveTool == toolBase &&
               !toolController.IsSwitching &&
               toolBase.State == ToolState.Idle;
    }

    private void OnPrimaryPerformed(InputAction.CallbackContext context)
    {
        if (!IsActiveAndReady() || !toolBase.TryStartUsing())
            return;

        try
        {
            // ЛКМ: захватить новый объект или установить удерживаемый.
            playerInteract.TryGrabOrPlace();
        }
        finally
        {
            toolBase.StopUsing();
        }
    }

    private void OnSecondaryPerformed(InputAction.CallbackContext context)
    {
        if (!IsActiveAndReady() || !toolBase.TryStartUsing())
            return;

        try
        {
            // ПКМ: бросить удерживаемый объект.
            playerInteract.ThrowHeldObject();
        }
        finally
        {
            toolBase.StopUsing();
        }
    }
}
