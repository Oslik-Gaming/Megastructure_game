using UnityEngine;
using UnityEngine.UIElements;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private TrashLocation location;
    [SerializeField, Range(0f, 100f)] private float paintPercent = 100f;
    [SerializeField] private string task = "CLEAN SECTOR 01:\nGET RID OF MOSS AND DEBRIS";

    private VisualElement root;
    private VisualElement paintFill;
    private VisualElement cleanFill;
    private VisualElement objectCard;
    private Label objectTitle;
    private Label objectDescription;
    private Label warning;
    private Label taskLabel;
    private Label timeLabel;
    private bool outsideZone;

    public void Configure(UIDocument hudDocument, TrashLocation trashLocation)
    {
        if (location != null)
            location.Changed -= RefreshClean;
        document = hudDocument;
        location = trashLocation;
        if (isActiveAndEnabled && location != null)
            location.Changed += RefreshClean;
    }

    /// <summary>Установить текст текущего задания.</summary>
    public void SetTask(string taskText)
    {
        task = taskText;
        if (taskLabel != null)
            taskLabel.text = taskText;
    }

    /// <summary>Переключить отслеживание на новую локацию.</summary>
    public void SwitchLocation(TrashLocation newLocation)
    {
        if (location != null)
            location.Changed -= RefreshClean;
        location = newLocation;
        if (location != null)
            location.Changed += RefreshClean;
        RefreshClean();
    }

    private void OnEnable()
    {
        if (location != null)
            location.Changed += RefreshClean;
    }

    private void Start() => Initialize();

    private void OnDisable()
    {
        if (location != null)
            location.Changed -= RefreshClean;
    }

    private void Initialize()
    {
        if (document == null || document.rootVisualElement == null)
            return;

        root = document.rootVisualElement.Q("MainContainer");
        if (root == null)
        {
            Debug.LogError("[GameHud] Не найден MainContainer. Проверьте GameHud.uxml в UIDocument.", this);
            return;
        }
        paintFill = root.Q("paint-fill");
        cleanFill = root.Q("clean-fill");
        objectCard = root.Q("object-card");
        objectTitle = root.Q<Label>("object-title");
        objectDescription = root.Q<Label>("object-description");
        warning = root.Q<Label>("critical-warning");
        taskLabel = root.Q<Label>("task-text");
        timeLabel = root.Q<Label>("time");
        if (taskLabel == null)
        {
            Debug.LogError("[GameHud] GameHud.uxml не содержит элемент task-text.", this);
            return;
        }

        taskLabel.text = task;
        RefreshPaint();
        RefreshClean();
        HideObject();
        SetOutsideZone(outsideZone);
    }

    private void Update()
    {
        if (timeLabel != null)
            timeLabel.text = "TIME  " + Time.timeSinceLevelLoad.ToString("00:00");
        else
            Debug.Log("Нету time label");
    }

    public void SetPaint(float value)
    {
        paintPercent = Mathf.Clamp(value, 0f, 100f);
        RefreshPaint();
    }

    public void ShowObject(HudObjectInfo info)
    {
        if (objectCard == null || info == null)
            return;
        objectCard.style.display = DisplayStyle.Flex;
        objectTitle.text = info.Title;
        objectDescription.text = info.Description;
    }

    public void HideObject()
    {
        if (objectCard != null)
            objectCard.style.display = DisplayStyle.None;
    }

    public void SetOutsideZone(bool value)
    {
        outsideZone = value;
        if (root == null)
            return;
        root.EnableInClassList("critical", value);
        if (warning != null)
        {
            warning.text = "CRITICAL WARNING";
            warning.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void RefreshPaint() => SetFill(paintFill, paintPercent);

    private void RefreshClean()
    {
        SetFill(cleanFill, location == null ? 0f : location.CleanedPercent);
    }

    private static void SetFill(VisualElement fill, float percent)
    {
        if (fill != null)
            fill.style.width = Length.Percent(Mathf.Clamp(percent, 0f, 100f));
    }
}
