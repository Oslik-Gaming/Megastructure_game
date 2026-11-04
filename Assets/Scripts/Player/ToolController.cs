using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Выбирает активный инструмент и управляет переключением между инструментами.
/// Должен находиться на ToolAnchor, а все ToolBase — быть его дочерними объектами.
/// </summary>
public class ToolController : MonoBehaviour
{
    [Header("Инструменты")]
    [Tooltip("Инструменты в порядке клавиш: 1, 2, 3, 4.")]
    [SerializeField] private ToolBase[] tools = new ToolBase[4];
    [Tooltip("Инструмент, активный при старте: 0 = клавиша 1, 1 = клавиша 2 и так далее.")]
    [SerializeField, Range(0, 3)] private int startingToolIndex;

    /// <summary>Инструмент, который сейчас видим и принимает команды использования.</summary>
    public ToolBase ActiveTool { get; private set; }
    /// <summary>True между началом скрытия старого и завершением появления нового инструмента.</summary>
    public bool IsSwitching { get; private set; }
    public bool CanUseActiveTool => !IsSwitching && ActiveTool != null && ActiveTool.CanUse;

    private PlayerInputActions input;
    private int activeToolIndex = -1;
    private Coroutine switchRoutine;

    private void Awake()
    {
        input = new PlayerInputActions();
    }

    private void Start()
    {
        if (!HasValidToolSetup())
        {
            HideAllToolsImmediately();
            return;
        }

        activeToolIndex = startingToolIndex;
        ActiveTool = tools[activeToolIndex];

        // Принудительно приводим сцену к одному однозначному начальному состоянию:
        // только выбранный инструмент видим и имеет активные Renderer/Collider.
        for (int i = 0; i < tools.Length; i++)
        {
            if (i == activeToolIndex)
                tools[i].SetVisibleImmediate();
            else
                tools[i].SetHiddenImmediate();
        }
    }

    private void HideAllToolsImmediately()
    {
        if (tools == null)
            return;

        foreach (ToolBase tool in tools)
        {
            if (tool != null)
                tool.SetHiddenImmediate();
        }
    }

    private void OnEnable()
    {
        input.Player.Enable();

        input.Player.SelectTool1.performed += OnSelectTool1;
        input.Player.SelectTool2.performed += OnSelectTool2;
        input.Player.SelectTool3.performed += OnSelectTool3;
        input.Player.SelectTool4.performed += OnSelectTool4;
    }

    private void OnDisable()
    {
        input.Player.SelectTool1.performed -= OnSelectTool1;
        input.Player.SelectTool2.performed -= OnSelectTool2;
        input.Player.SelectTool3.performed -= OnSelectTool3;
        input.Player.SelectTool4.performed -= OnSelectTool4;
        input.Player.Disable();

        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
            switchRoutine = null;
            IsSwitching = false;
        }
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    /// <summary>
    /// Начинает выбор инструмента по его индексу: 0 — клавиша 1, 3 — клавиша 4.
    /// Нажатия во время переключения и повторный выбор активного инструмента игнорируются.
    /// </summary>
    public void SelectTool(int toolIndex)
    {
        if (IsSwitching || toolIndex < 0 || toolIndex >= tools.Length || tools[toolIndex] == null)
            return;

        if (toolIndex == activeToolIndex)
            return;

        if (ActiveTool == null)
        {
            activeToolIndex = toolIndex;
            ActiveTool = tools[toolIndex];
            ActiveTool.Show();
            return;
        }

        switchRoutine = StartCoroutine(SwitchToolRoutine(toolIndex));
    }

    /// <summary>
    /// Безопасная точка входа для будущих инструментов: только текущий инструмент
    /// может начать использование, и только когда не выполняется переключение.
    /// </summary>
    public bool TryStartUsingActiveTool()
    {
        return CanUseActiveTool && ActiveTool.TryStartUsing();
    }

    /// <summary>Завершает использование только текущего инструмента.</summary>
    public void StopUsingActiveTool()
    {
        if (!IsSwitching && ActiveTool != null)
            ActiveTool.StopUsing();
    }

    private IEnumerator SwitchToolRoutine(int nextToolIndex)
    {
        IsSwitching = true;

        ActiveTool.Hide();
        yield return new WaitUntil(() => ActiveTool == null || ActiveTool.State == ToolState.Hidden);

        activeToolIndex = nextToolIndex;
        ActiveTool = tools[activeToolIndex];
        ActiveTool.Show();
        yield return new WaitUntil(() => ActiveTool == null || ActiveTool.State == ToolState.Idle || ActiveTool.State == ToolState.NoPaint);

        IsSwitching = false;
        switchRoutine = null;
    }

    private bool HasValidToolSetup()
    {
        if (tools == null || tools.Length != 4)
        {
            Debug.LogError("[ToolController] В списке Tools должно быть ровно 4 инструмента.", this);
            return false;
        }

        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i] != null)
                continue;

            Debug.LogError($"[ToolController] Не назначен инструмент для клавиши {i + 1}.", this);
            return false;
        }

        return true;
    }

    private void OnSelectTool1(InputAction.CallbackContext context) => SelectTool(0);
    private void OnSelectTool2(InputAction.CallbackContext context) => SelectTool(1);
    private void OnSelectTool3(InputAction.CallbackContext context) => SelectTool(2);
    private void OnSelectTool4(InputAction.CallbackContext context) => SelectTool(3);
}
