using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Временная диагностика управления инструментами.
/// Выводит в Console выбор инструмента, нажатия и отпускания кнопок инструмента.
/// Удалить с Player после проверки ввода.
/// </summary>
public class ToolInputDebug : MonoBehaviour
{
    private PlayerInputActions input;
    private bool primaryHeld;
    private bool secondaryHeld;

    private void Awake()
    {
        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Player.Enable();

        input.Player.SelectTool1.performed += OnSelectTool1;
        input.Player.SelectTool2.performed += OnSelectTool2;
        input.Player.SelectTool3.performed += OnSelectTool3;
        input.Player.SelectTool4.performed += OnSelectTool4;

        input.Player.ToolPrimary.started += OnPrimaryStarted;
        input.Player.ToolPrimary.canceled += OnPrimaryCanceled;
        input.Player.ToolSecondary.started += OnSecondaryStarted;
        input.Player.ToolSecondary.canceled += OnSecondaryCanceled;
    }

    private void OnDisable()
    {
        input.Player.SelectTool1.performed -= OnSelectTool1;
        input.Player.SelectTool2.performed -= OnSelectTool2;
        input.Player.SelectTool3.performed -= OnSelectTool3;
        input.Player.SelectTool4.performed -= OnSelectTool4;

        input.Player.ToolPrimary.started -= OnPrimaryStarted;
        input.Player.ToolPrimary.canceled -= OnPrimaryCanceled;
        input.Player.ToolSecondary.started -= OnSecondaryStarted;
        input.Player.ToolSecondary.canceled -= OnSecondaryCanceled;

        input.Player.Disable();
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Update()
    {
        if (primaryHeld)
            Debug.Log("ToolPrimary удерживается (ЛКМ)");

        if (secondaryHeld)
            Debug.Log("ToolSecondary удерживается (ПКМ)");
    }

    private void OnSelectTool1(InputAction.CallbackContext context) => Debug.Log("Выбран инструмент 1");
    private void OnSelectTool2(InputAction.CallbackContext context) => Debug.Log("Выбран инструмент 2");
    private void OnSelectTool3(InputAction.CallbackContext context) => Debug.Log("Выбран инструмент 3");
    private void OnSelectTool4(InputAction.CallbackContext context) => Debug.Log("Выбран инструмент 4");

    private void OnPrimaryStarted(InputAction.CallbackContext context)
    {
        primaryHeld = true;
        Debug.Log("ToolPrimary нажат (ЛКМ)");
    }

    private void OnPrimaryCanceled(InputAction.CallbackContext context)
    {
        primaryHeld = false;
        Debug.Log("ToolPrimary отпущен (ЛКМ)");
    }

    private void OnSecondaryStarted(InputAction.CallbackContext context)
    {
        secondaryHeld = true;
        Debug.Log("ToolSecondary нажат (ПКМ)");
    }

    private void OnSecondaryCanceled(InputAction.CallbackContext context)
    {
        secondaryHeld = false;
        Debug.Log("ToolSecondary отпущен (ПКМ)");
    }
}
