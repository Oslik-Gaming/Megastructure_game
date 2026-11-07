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

        bool isActiveTool = toolController.ActiveTool == toolBase && !toolController.IsSwitching;

        // Если гравизахват больше не активный инструмент (переключились на другой) —
        // удерживаемый предмет просто падает, а не остаётся висеть в воздухе.
        if (playerInteract.HasCarriedItem && !isActiveTool)
        {
            playerInteract.DropHeldObject();
        }

        // Пока предмет удерживается — инструмент в состоянии Using,
        // благодаря этому ToolSoundPlayer играет звук работы гравизахвата.
        if (playerInteract.HasCarriedItem && isActiveTool)
        {
            if (toolBase.State == ToolState.Idle)
                toolBase.TryStartUsing();
        }
        else if (toolBase.State == ToolState.Using)
        {
            toolBase.StopUsing();
        }
    }

    private bool IsActiveAndReady()
    {
        // State.Using допустим: пока удерживается предмет, инструмент остаётся "в использовании",
        // и ЛКМ/ПКМ должны продолжать работать (установка/бросок).
        return toolBase != null && toolController != null && playerInteract != null &&
               toolController.ActiveTool == toolBase &&
               !toolController.IsSwitching &&
               (toolBase.State == ToolState.Idle || toolBase.State == ToolState.Using);
    }

    private void OnPrimaryPerformed(InputAction.CallbackContext context)
    {
        if (!IsActiveAndReady())
            return;

        // ЛКМ: захватить новый объект или установить удерживаемый.
        // Состоянием Using управляет Update, чтобы звучал звук работы.
        playerInteract.TryGrabOrPlace();
    }

    private void OnSecondaryPerformed(InputAction.CallbackContext context)
    {
        if (!IsActiveAndReady())
            return;

        // ПКМ: бросить удерживаемый объект.
        playerInteract.ThrowHeldObject();
    }
}
