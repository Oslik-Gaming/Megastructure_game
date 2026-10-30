using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// SettingsPanelController — общий экран настроек (громкость, чувствительность мыши,
/// список управления). Работает и в главном меню, и в меню паузы.
///
/// ПОДКЛЮЧЕНИЕ: настраивать ничего не нужно. Контроллер меню сам добавляет этот
/// компонент на свой GameObject при нажатии "НАСТРОЙКИ". Разметка лежит в
/// Assets/Resources/UI/Settings.uxml, стили — Assets/UI/USS/Settings.uss.
/// Значения хранит GameSettings (PlayerPrefs — сохраняются между запусками).
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    public static SettingsPanelController Instance { get; private set; }

    /// <summary>Открыт ли сейчас экран настроек.</summary>
    public bool IsShown => isShown;

    [Header("Звук кнопок (необязательно)")]
    [Tooltip("UISoundButtonManager с нужными звуками наведения/клика. " +
             "Создайте пустой объект (например SettingsSounds), добавьте на него " +
             "AudioSource и UISoundButtonManager, назначьте клипы — и укажите его здесь. " +
             "Если оставить пустым — возьмётся менеджер звука из сцены (как в главном меню).")]
    [SerializeField] private UISoundButtonManager buttonSounds;

    [Header("SFX-источник")]
    [Tooltip("Имя объекта в сцене, через AudioSource которого играют звуки настроек. " +
             "На сам объект ничего не добавляется — используется только его AudioSource.")]
    [SerializeField] private string sfxSourceObjectName = "SFX_Source";

    // =====================================================================
    //  ЭЛЕМЕНТЫ UI
    // =====================================================================

    private UIDocument doc;
    private VisualElement root;
    private VisualElement mainContainer;
    private Label volumeValue, sensitivityValue;   // числа справа
    private Slider volumeSlider, sensitivitySlider;
    private VisualElement controlsList;
    private Button backButton;
    private VisualElement cursor;
    private bool backHovered;                      // мышь на кнопке "НАЗАД" сейчас?

    private bool isShown;
    private PlayerInputActions inputForBindings;   // только чтобы прочитать список биндов
    private Coroutine cursorBlinkRoutine;

    private const float BaseWidth = 1920f;
    private const float BaseHeight = 1080f;

    // Русские названия действий (для списка управления)
    private static readonly Dictionary<string, string> ActionNames = new Dictionary<string, string>
    {
        { "Movement", "ДВИЖЕНИЕ"        },
        { "Jump",     "ПРЫЖОК"          },
        { "Look",     "ОБЗОР (МЫШЬ)"    },
        { "Interact", "ВЗАИМОДЕЙСТВИЕ"  },
        { "Sprint",   "БЕГ"             },
        { "PauseMenu", "ПАУЗА"          },
    };

    // =====================================================================
    //  LIFECYCLE
    // =====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (inputForBindings != null) { inputForBindings.Dispose(); inputForBindings = null; }
    }

    private void OnDestroy()
    {
        if (doc == null) return;
        if (doc.panelSettings != null) Destroy(doc.panelSettings); // клон создан в рантайме — чистим сами
        Destroy(doc.gameObject);
        doc = null;
    }

    private bool initialized;

    private void Start() => EnsureInitialized();

    /// <summary>
    /// Создаёт UI-документ и кэширует элементы. Безопасно вызывать когда угодно:
    /// и из Start(), и прямо из первого нажатия "НАСТРОЙКИ" (компонент добавляется
    /// кнопкой в том же кадре — раньше, чем срабатывает Start, поэтому Show() не
    /// должен зависеть от Start()).
    /// </summary>
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        CreateDocument();
        CacheElements();
        WireEvents();
        BuildControlsList();
        RefreshValues();
        // Изначально скрыт (Hide() срабатывает только если isShown, поэтому напрямую)
        if (root != null) root.style.display = DisplayStyle.None;
    }

    // =====================================================================
    //  СОЗДАНИЕ UI-ДОКУМЕНТА
    // =====================================================================

    private void CreateDocument()
    {
        var go = new GameObject("SettingsPanelDocument");
        // ВАЖНО: документ НЕ делаем дочерним объектом меню — в Unity 6 вложенный
        // UIDocument обязан использовать тот же PanelSettings, что и родитель (Assert),
        // а делить панель с меню нельзя (иначе меню и настройки делят flex-колонку).
        // Поэтому документ живёт на корне сцены, а уничтожается вместе с контроллером.
        doc = go.AddComponent<UIDocument>();
        doc.panelSettings = CreateOwnPanelSettings();            // СВОЯ панель — иначе настройки и меню делят одну панель
                                                                 // и встают друг под другом (меню поджимается вверх)
        doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Settings");
        doc.sortingOrder = 10;                                   // поверх меню

        if (doc.visualTreeAsset == null)
            Debug.LogError("[Settings] Не найден Assets/Resources/UI/Settings.uxml!");
        if (doc.panelSettings == null)
            Debug.LogError("[Settings] Не найден PanelSettings в сцене!");

        AttachMenuSounds();  // звук наведения/клика как в главном меню
    }

    // Подключает к панели настроек звуки кнопок:
    // 1) клипы берутся из менеджера, назначенного в инспекторе (buttonSounds),
    //    а если он пуст — из любого UISoundButtonManager сцены (звуки как в главном меню);
    // 2) воспроизведение идёт через УЖЕ СУЩЕСТВУЮЩИЙ источник сцены — объект
    //    "SFX_Source" (общий SFX-канал). Свой AudioSource создаётся только
    //    как запасной вариант, если SFX_Source в сцене не найден.
    private void AttachMenuSounds()
    {
        var template = buttonSounds != null ? buttonSounds : FindFirstObjectByType<UISoundButtonManager>();
        if (template == null) return;   // менеджера нет — панель работает тихо

        // Общий SFX-источник сцены (имя настраивается в инспекторе)
        AudioSource src = FindSfxSource();
        if (src == null)
        {
            // Запасной вариант: свой источник на объекте панели
            src = doc.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;      // UI-звук — всегда 2D

            // Переносим общие настройки источника (громкость, микшер)
            var templateSrc = template.Source;
            if (templateSrc != null)
            {
                src.outputAudioMixerGroup = templateSrc.outputAudioMixerGroup;
                src.volume = templateSrc.volume;
                src.pitch = templateSrc.pitch;
            }
        }

        var manager = doc.gameObject.AddComponent<UISoundButtonManager>();
        manager.Setup(doc, src, template.HoverClip, template.ClickClip,
                      template.HoverVolume, template.ClickVolume);
    }

    // Ищет AudioSource на объекте с заданным именем; если не нашлось —
    // пробует запасное имя "SFX_Sound".
    private AudioSource FindSfxSource()
    {
        var go = GameObject.Find(sfxSourceObjectName);
        if (go != null)
        {
            var src = go.GetComponent<AudioSource>();
            if (src != null) return src;
        }

        // Запасной вариант имени (у вас на сцене объект может называться SFX_Sound)
        go = GameObject.Find("SFX_Sound");
        return go != null ? go.GetComponent<AudioSource>() : null;
    }

    // Берёт PanelSettings из меню как образец и делает СВОЮ копию.
    // Копия нужна, чтобы панель настроек рендерилась независимо от меню:
    // полноэкранно, поверх меню, без общих flex-вычислений.
    private PanelSettings CreateOwnPanelSettings()
    {
        var source = FindPanelSettings();
        if (source == null)
        {
            Debug.LogError("[Settings] Не найден PanelSettings в сцене!");
            return null;
        }

        var own = Instantiate(source);
        own.name = "SettingsPanelSettings";
        return own;
    }

    private PanelSettings FindPanelSettings()
    {
        var own = GetComponent<UIDocument>();
        if (own != null && own.panelSettings != null) return own.panelSettings;

        foreach (var d in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            if (d != doc && d.panelSettings != null)
                return d.panelSettings;
        return null;
    }

    private void CacheElements()
    {
        root = doc.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[Settings] rootVisualElement недоступен.");
            return;
        }

        mainContainer    = root.Q<VisualElement>("MainContainer");
        volumeValue      = root.Q<Label>("VolumeValue");
        sensitivityValue = root.Q<Label>("SensitivityValue");
        volumeSlider     = root.Q<Slider>("VolumeSlider");
        sensitivitySlider = root.Q<Slider>("SensitivitySlider");
        controlsList     = root.Q<VisualElement>("ControlsList");
        backButton       = root.Q<Button>("BackButton");
        cursor           = root.Q<VisualElement>("Cursor");

        root.focusable = true; // чтобы Esc доходил до панели (нужен фокус)
    }

    private void WireEvents()
    {
        if (root == null) return;

        if (volumeSlider != null)
            volumeSlider.RegisterValueChangedCallback(evt =>
            {
                GameSettings.SetMasterVolume(evt.newValue);
                volumeValue.text = Mathf.RoundToInt(GameSettings.MasterVolume).ToString();
            });

        if (sensitivitySlider != null)
            sensitivitySlider.RegisterValueChangedCallback(evt =>
            {
                GameSettings.SetMouseSensitivity(evt.newValue);
                sensitivityValue.text = Mathf.RoundToInt(GameSettings.MouseSensitivity).ToString();
            });

        if (backButton != null)
        {
            backButton.clicked += Hide;
            // Стрелки ">>" появляются при наведении мыши и исчезают, когда мышь уходит
            backButton.RegisterCallback<MouseEnterEvent>(_ => { backHovered = true;  UpdateBackVisual(); });
            backButton.RegisterCallback<MouseLeaveEvent>(_ => { backHovered = false; UpdateBackVisual(); });
        }

        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    // =====================================================================
    //  СПИСОК УПРАВЛЕНИЯ (из PlayerInputActions, только просмотр)
    // =====================================================================

    private void BuildControlsList()
    {
        if (controlsList == null) return;

        inputForBindings = new PlayerInputActions();

        AddHeader("ИГРА:");
        AddActionBindings(inputForBindings.asset.FindActionMap("Player"));
        AddHeader("МЕНЮ:");
        AddActionBindings(inputForBindings.asset.FindActionMap("UI"));
    }

    private void AddHeader(string text)
    {
        var header = new Label(text);
        header.AddToClassList("controls-header");
        controlsList.Add(header);
    }

    private void AddActionBindings(InputActionMap map)
    {
        if (map == null) return;

        foreach (var action in map.actions)
        {
            string keys = GetBindingsString(action);
            if (string.IsNullOrEmpty(keys)) continue;

            var row = new VisualElement();
            row.AddToClassList("controls-row");

            var name = new Label(ActionDisplayName(action.name));
            name.AddToClassList("controls-action");

            var keysLabel = new Label(keys);
            keysLabel.AddToClassList("controls-keys");

            row.Add(name);
            row.Add(keysLabel);
            controlsList.Add(row);
        }
    }

    // Собирает все кнопки действия в строку вида "W / S / A / D".
    private static string GetBindingsString(InputAction action)
    {
        var parts = new List<string>();
        foreach (var binding in action.bindings)
        {
            if (binding.isComposite) continue; // саму группу "2D Vector" не показываем
            string s = binding.ToDisplayString();
            if (!string.IsNullOrEmpty(s) && !parts.Contains(s))
                parts.Add(s);
        }
        return string.Join(" / ", parts);
    }

    private static string ActionDisplayName(string actionName)
        => ActionNames.TryGetValue(actionName, out var ru) ? ru : actionName.ToUpperInvariant();

    // =====================================================================
    //  ПОКАЗАТЬ / СКРЫТЬ
    // =====================================================================

    public void Show()
    {
        // Первый вызов может прийти раньше Start() — инициализируемся на месте
        EnsureInitialized();
        if (isShown || root == null) return;
        isShown = true;

        RefreshValues();
        root.style.display = DisplayStyle.Flex;
        root.Focus();

        if (cursorBlinkRoutine == null)
            cursorBlinkRoutine = StartCoroutine(CursorBlink());
    }

    public void Hide()
    {
        if (!isShown) return;
        isShown = false;

        if (cursorBlinkRoutine != null)
        {
            StopCoroutine(cursorBlinkRoutine);
            cursorBlinkRoutine = null;
        }

        if (root != null) root.style.display = DisplayStyle.None;

        // Вернуть фокус в меню, из которого открыли настройки
        GetComponent<MainMenuController>()?.FocusMenuRoot();
        GetComponent<PauseMenuController>()?.FocusMenuRoot();
    }

    // =====================================================================
    //  ЗНАЧЕНИЯ
    // =====================================================================

    private void RefreshValues()
    {
        if (volumeSlider == null) return;
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        volumeValue.text = Mathf.RoundToInt(GameSettings.MasterVolume).ToString();
        sensitivityValue.text = Mathf.RoundToInt(GameSettings.MouseSensitivity).ToString();
    }

    // =====================================================================
    //  КЛАВИАТУРА: только Esc — закрыть. Навигация стрелками убрана:
    //  настройки управляются мышью (слайдеры и кнопка "НАЗАД").
    // =====================================================================

    // Стрелки ">>" и подсветка — ТОЛЬКО у кнопки "НАЗАД", при наведении мыши.
    // Остальной текст настроек всегда одного цвета.
    private void UpdateBackVisual()
    {
        if (backButton == null) return;
        backButton.text = (backHovered ? ">> " : "    ") + "НАЗАД";
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Escape) return;

        // В сцене с меню паузы Esc обрабатывает PauseMenuController
        // через Input System — он сам закроет настройки.
        if (GetComponent<PauseMenuController>() == null) Hide();
        evt.StopPropagation();
    }

    // =====================================================================
    //  МИГАЮЩИЙ КУРСОР + МАСШТАБ (как в остальных меню)
    // =====================================================================

    private System.Collections.IEnumerator CursorBlink()
    {
        var wait = new WaitForSecondsRealtime(0.5f);
        while (true)
        {
            if (cursor != null) cursor.style.opacity = 1;
            yield return wait;
            if (cursor != null) cursor.style.opacity = 0;
            yield return wait;
        }
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
        => ApplyScale(evt.newRect.width, evt.newRect.height);

    private void ApplyScale(float w, float h)
    {
        if (mainContainer == null) return;
        float scale = Mathf.Min(w / BaseWidth, h / BaseHeight);
        mainContainer.transform.scale = new Vector3(scale, scale, 1f);
    }
}
