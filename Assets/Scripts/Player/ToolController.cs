using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Progression;

/// <summary>
/// Выбирает активный инструмент и управляет переключением между инструментами.
/// Должен находиться на ToolAnchor, а все ToolBase — быть его дочерними объектами.
/// </summary>
public class ToolController : MonoBehaviour
{
    [Header("Инструменты")]
    [Tooltip("Инструменты в порядке клавиш: 1, 2, 3, 4.")]
    [SerializeField] private ToolBase[] tools = new ToolBase[4];
    [SerializeField] private ToolType[] toolTypes = new ToolType[4] 
    { 
        ToolType.GravityGun, 
        ToolType.Heater, 
        ToolType.Destructor, 
        ToolType.Sprayer 
    };

    /// <summary>Инструмент, который сейчас видим и принимает команды использования.</summary>
    public ToolBase ActiveTool { get; private set; }
    /// <summary>Индекс слота активного инструмента или -1, если ничего не выбрано.</summary>
    public int ActiveToolIndex => activeToolIndex;
    /// <summary>Вызывается при смене активного инструмента; аргумент — индекс слота.</summary>
    public event System.Action<int> ActiveToolChanged;
    /// <summary>Вызывается при разблокировке нового слота инструмента.</summary>
    public event System.Action UnlockedSlotsChanged;
    /// <summary>True между началом скрытия старого и завершением появления нового инструмента.</summary>
    public bool IsSwitching { get; private set; }
    public bool CanUseActiveTool => !IsSwitching && ActiveTool != null && ActiveTool.CanUse;

    private PlayerInputActions input;
    private int activeToolIndex = -1;
    private Coroutine switchRoutine;
    private ToolInventory toolInventory;
    private HashSet<int> unlockedSlots = new HashSet<int>();

    /// <summary>Проверить, разблокирован ли слот по индексу (0–3).</summary>
    public bool IsSlotUnlocked(int slotIndex) => unlockedSlots.Contains(slotIndex);

    /// <summary>Получить тип инструмента, назначенного слоту (0–3).</summary>
    public ToolType GetToolType(int slotIndex) => toolTypes[slotIndex];

    private void Awake()
    {
        input = new PlayerInputActions();
        toolInventory = FindFirstObjectByType<ToolInventory>();
    }

    private void Start()
    {
        if (!HasValidToolSetup())
        {
            HideAllToolsImmediately();
            return;
        }

        // Скрыть все инструменты
        HideAllToolsImmediately();

        // Разблокировать инструменты согласно ToolInventory
        if (toolInventory != null)
        {
            toolInventory.ToolUnlocked += OnToolUnlocked;
            
            for (int i = 0; i < toolTypes.Length; i++)
            {
                if (toolInventory.HasTool(toolTypes[i]))
                {
                    UnlockSlot(i, false);
                }
            }

            // Автоматически выбрать первый разблокированный инструмент
            if (unlockedSlots.Count > 0)
            {
                int firstUnlocked = GetFirstUnlockedSlot();
                if (firstUnlocked >= 0)
                {
                    SelectToolImmediate(firstUnlocked);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (toolInventory != null)
        {
            toolInventory.ToolUnlocked -= OnToolUnlocked;
        }
        input.Dispose();
    }

    private void OnToolUnlocked(ToolType toolType)
    {
        for (int i = 0; i < toolTypes.Length; i++)
        {
            if (toolTypes[i] == toolType)
            {
                UnlockSlot(i, true);
                break;
            }
        }
    }

    private void UnlockSlot(int slotIndex, bool autoSelect)
    {
        if (slotIndex < 0 || slotIndex >= tools.Length || tools[slotIndex] == null)
            return;

        if (unlockedSlots.Add(slotIndex))
        {
            Debug.Log($"[ToolController] Разблокирован слот {slotIndex + 1}: {toolTypes[slotIndex]}");
            UnlockedSlotsChanged?.Invoke();

            // Если нет активного инструмента, выбрать этот автоматически
            if (autoSelect && ActiveTool == null)
            {
                SelectToolImmediate(slotIndex);
            }
        }
    }

    private int GetFirstUnlockedSlot()
    {
        for (int i = 0; i < tools.Length; i++)
        {
            if (unlockedSlots.Contains(i))
                return i;
        }
        return -1;
    }

    private void SelectToolImmediate(int toolIndex)
    {
        if (toolIndex < 0 || toolIndex >= tools.Length || tools[toolIndex] == null)
            return;

        activeToolIndex = toolIndex;
        ActiveTool = tools[activeToolIndex];
        ActiveTool.SetVisibleImmediate();
        ActiveToolChanged?.Invoke(activeToolIndex);
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

    /// <summary>
    /// Начинает выбор инструмента по его индексу: 0 — клавиша 1, 3 — клавиша 4.
    /// Нажатия во время переключения и повторный выбор активного инструмента игнорируются.
    /// Можно выбирать только разблокированные инструменты.
    /// </summary>
    public void SelectTool(int toolIndex)
    {
        if (IsSwitching || toolIndex < 0 || toolIndex >= tools.Length || tools[toolIndex] == null)
            return;

        // Проверить, разблокирован ли слот
        if (!unlockedSlots.Contains(toolIndex))
            return;

        if (toolIndex == activeToolIndex)
            return;

        if (ActiveTool == null)
        {
            activeToolIndex = toolIndex;
            ActiveTool = tools[toolIndex];
            ActiveTool.Show();
            ActiveToolChanged?.Invoke(activeToolIndex);
            return;
        }

        switchRoutine = StartCoroutine(SwitchToolRoutine(toolIndex));
    }

    /// <summary>
    /// Выбирает инструмент по его типу. Используется, например, после подбора
    /// нового инструмента в мире.
    /// </summary>
    public void SelectTool(ToolType toolType)
    {
        for (int i = 0; i < toolTypes.Length; i++)
        {
            if (toolTypes[i] != toolType)
                continue;

            // Обычно слот уже разблокирован событием ToolUnlocked, но этот
            // вызов делает метод безопасным и для уже разблокированного предмета.
            if (!unlockedSlots.Contains(i))
                UnlockSlot(i, false);

            SelectTool(i);
            return;
        }

        Debug.LogWarning(
            $"[ToolController] Инструмент {toolType} отсутствует в массиве toolTypes.",
            this);
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
        ActiveToolChanged?.Invoke(activeToolIndex);
        ActiveTool.Show();
        yield return new WaitUntil(() => ActiveTool == null || ActiveTool.State == ToolState.Idle || ActiveTool.State == ToolState.NoPaint);

        IsSwitching = false;
        switchRoutine = null;
    }

    private bool HasValidToolSetup()
    {
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
