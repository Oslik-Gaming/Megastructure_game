using UnityEngine;
using UnityEngine.UIElements;
using Progression;

/// <summary>
/// Отображает на HUD панель инструментов: какие слоты разблокированы и какой
/// инструмент игрок держит в руках. Требует GameHud.uxml с элементами
/// tool-slot-0..3 и tool-name-0..3.
/// </summary>
public sealed class ToolHud : MonoBehaviour
{
    [Tooltip("UIDocument с GameHud.uxml (тот же, что и у GameHud).")]
    [SerializeField] private UIDocument document;

    [Tooltip("ToolController на ToolAnchor. Если не задан, ищется в сцене.")]
    [SerializeField] private ToolController toolController;

    private VisualElement[] slots = new VisualElement[4];
    private Label[] names = new Label[4];
    private ToolInventory toolInventory;
    private bool initialized;

    private static string GetToolName(ToolType type)
    {
        switch (type)
        {
            case ToolType.GravityGun: return "GRAVITY GUN";
            case ToolType.Heater: return "HEATER";
            case ToolType.Destructor: return "DESTRUCTOR";
            case ToolType.Sprayer: return "SPRAYER";
            default: return type.ToString().ToUpperInvariant();
        }
    }

    private void Awake()
    {
        if (toolController == null)
            toolController = FindFirstObjectByType<ToolController>();
        toolInventory = FindFirstObjectByType<ToolInventory>();
    }

    private void OnEnable()
    {
        if (toolController != null)
        {
            toolController.ActiveToolChanged += OnActiveToolChanged;
            toolController.UnlockedSlotsChanged += Refresh;
        }
        if (toolInventory != null)
            toolInventory.ToolUnlocked += OnToolUnlocked;

        Refresh();
    }

    private void OnDisable()
    {
        if (toolController != null)
        {
            toolController.ActiveToolChanged -= OnActiveToolChanged;
            toolController.UnlockedSlotsChanged -= Refresh;
        }
        if (toolInventory != null)
            toolInventory.ToolUnlocked -= OnToolUnlocked;
    }

    private void Start() => Refresh();

    private void OnActiveToolChanged(int slotIndex) => Refresh();

    private void OnToolUnlocked(ToolType toolType) => Refresh();

    private void Refresh()
    {
        if (!EnsureInitialized())
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            bool unlocked = toolController != null && toolController.IsSlotUnlocked(i);

            // Инструменты, которых нет у игрока, вообще не показываем в списке.
            slots[i].style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;
            slots[i].EnableInClassList("unlocked", unlocked);
            slots[i].EnableInClassList("active",
                unlocked && toolController.ActiveToolIndex == i);

            if (names[i] != null)
            {
                names[i].text = GetToolName(toolController.GetToolType(i));
            }
        }
    }

    private bool EnsureInitialized()
    {
        if (initialized)
            return true;

        if (document == null || document.rootVisualElement == null)
            return false;

        VisualElement root = document.rootVisualElement.Q("MainContainer");
        if (root == null)
        {
            Debug.LogError("[ToolHud] Не найден MainContainer. Проверьте GameHud.uxml в UIDocument.", this);
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = root.Q($"tool-slot-{i}");
            names[i] = root.Q<Label>($"tool-name-{i}");
            if (slots[i] == null || names[i] == null)
            {
                Debug.LogError($"[ToolHud] Не найден tool-slot-{i} или tool-name-{i} в GameHud.uxml.", this);
                return false;
            }
        }

        initialized = true;
        return true;
    }
}
