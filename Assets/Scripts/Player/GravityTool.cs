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

    private bool IsActiveAndReady()
    {
        return toolBase != null && toolController != null && playerInteract != null &&
               toolController.ActiveTool == toolBase &&
               !toolController.IsSwitching &&
               toolBase.State == ToolState.Idle;
    }

    private void OnPrimaryPerformed(InputAction.CallbackContext context)
    {
        if (IsActiveAndReady())
            playerInteract.TryGrabOrPlace();
    }

    private void OnSecondaryPerformed(InputAction.CallbackContext context)
    {
        if (IsActiveAndReady())
            playerInteract.ThrowHeldObject();
    }
}
