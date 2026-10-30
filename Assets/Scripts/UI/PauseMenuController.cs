using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// PauseMenuController — внутриигровое меню паузы.
///
/// ВВОД:
///  - Открытие/закрытие меню — через Input System (InputActionAsset).
///    По умолчанию ищется action map "UI" → action "PauseMenu" (Escape [Keyboard]).
///  - Навигация по меню — UI Toolkit KeyDownEvent (стрелки/WASD/Enter/цифры 1-4).
///
/// CRT-эффекты (глитч, курсор, сканлайны) этим скриптом НЕ управляются —
/// они реализованы отдельно. UXML элементы для них остаются в разметке,
/// но здесь не кэшируются и не трогаются.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    // =====================================================================
    //  КОНФИГУРАЦИЯ
    // =====================================================================

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Сцена-чекпоинт. Пусто = перезагрузка текущей сцены.")]
    [SerializeField] private string checkpointSceneName = "";

    [Header("Input System")]
    [Tooltip("Ассет Input Actions (например PlayerInputActions).")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "UI";
    [SerializeField] private string pauseActionName = "PauseMenu";

    [Header("Behaviour")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool pauseGameWhenShown = true;
    [SerializeField] private bool trackUptime = true;

    // =====================================================================
    //  RUNTIME STATE
    // =====================================================================

    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement scaleContainer;
    private VisualElement mainContainer;
    private VisualElement cursor;          // мигающий курсор в верхней панели (как в главном меню)
    private Coroutine cursorBlinkRoutine;

    private Button[] menuButtons;
    private string[] baseTexts;
    private int selectedIndex;

    private Label statusUnitId, statusSector, statusClean, statusPaint;
    private Label statusTemp, statusGrip, statusAnomaly, statusUptime;

    private InputAction pauseAction;
    private bool isShown;
    private float savedTimeScale = 1f;
    private float sessionStartTime;

    private const float BASE_WIDTH = 1920f;
    private const float BASE_HEIGHT = 1080f;

    // =====================================================================
    //  LIFECYCLE
    // =====================================================================

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        CacheElements();
        WireButtons();
        RegisterCallbacks();
        SetupInput();
        ResetStatusDefaults();

        sessionStartTime = Time.realtimeSinceStartup;

        // Мигающий курсор — как в MainMenuController
        if (cursorBlinkRoutine == null)
            cursorBlinkRoutine = StartCoroutine(CursorBlink());

        if (startHidden)
        {
            // ВАЖНО: не вызываем Hide() — он делает ранний return при isShown == false.
            // Применяем скрытое состояние напрямую.
            isShown = false;
            GamePaused = false;
            if (scaleContainer != null)
            {
                scaleContainer.style.display = DisplayStyle.None;
                scaleContainer.AddToClassList("hidden");
            }
        }
        else Show();
    }

    void OnDisable()
    {
        if (cursorBlinkRoutine != null)
        {
            StopCoroutine(cursorBlinkRoutine);
            cursorBlinkRoutine = null;
        }

        TeardownInput();

        if (root != null)
        {
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    void Update()
    {
        if (isShown && trackUptime)
            UpdateUptime();
    }

    // =====================================================================
    //  SETUP
    // =====================================================================

    private void CacheElements()
    {
        scaleContainer = root.Q<VisualElement>("ScaleContainer");
        mainContainer  = root.Q<VisualElement>("MainContainer");
        cursor         = root.Q<VisualElement>("Cursor");

        menuButtons = new[]
        {
            root.Q<Button>("ResumeButton"),
            root.Q<Button>("SettingsButton"),
            root.Q<Button>("RestartButton"),
            root.Q<Button>("ExitButton"),
        };

        baseTexts = new string[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
            baseTexts[i] = menuButtons[i] != null ? menuButtons[i].text : "";

        statusUnitId  = root.Q<Label>("StatusUnitId");
        statusSector  = root.Q<Label>("StatusSector");
        statusClean   = root.Q<Label>("StatusClean");
        statusPaint   = root.Q<Label>("StatusPaint");
        statusTemp    = root.Q<Label>("StatusTemp");
        statusGrip    = root.Q<Label>("StatusGrip");
        statusAnomaly = root.Q<Label>("StatusAnomaly");
        statusUptime  = root.Q<Label>("StatusUptime");
    }

    private void WireButtons()
    {
        if (menuButtons.Length > 0 && menuButtons[0] != null) menuButtons[0].clicked += OnResume;
        if (menuButtons.Length > 1 && menuButtons[1] != null) menuButtons[1].clicked += OnSettings;
        if (menuButtons.Length > 2 && menuButtons[2] != null) menuButtons[2].clicked += OnRestart;
        if (menuButtons.Length > 3 && menuButtons[3] != null) menuButtons[3].clicked += OnExit;

        // Hover подсветка
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            int index = i;
            menuButtons[i].focusable = false;
            menuButtons[i].RegisterCallback<MouseEnterEvent>(_ => SelectButton(index));
        }
    }

    private void RegisterCallbacks()
    {
        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    // =====================================================================
    //  INPUT SYSTEM
    // =====================================================================

    private void SetupInput()
    {
        if (inputActions == null)
        {
            Debug.LogError("[PauseMenu] InputActionAsset не назначен. " +
                           "Перетащите PlayerInputActions в инспекторе.");
            return;
        }

        var map = inputActions.FindActionMap(actionMapName);
        if (map == null)
        {
            Debug.LogError($"[PauseMenu] Action Map '{actionMapName}' не найден в {inputActions.name}.");
            return;
        }

        pauseAction = map.FindAction(pauseActionName);
        if (pauseAction == null)
        {
            Debug.LogError($"[PauseMenu] Action '{pauseActionName}' не найден в map '{actionMapName}'.");
            return;
        }

        pauseAction.performed += OnPausePerformed;
        pauseAction.Enable();
    }

    private void TeardownInput()
    {
        if (pauseAction == null) return;
        pauseAction.performed -= OnPausePerformed;
        pauseAction.Disable();
        pauseAction = null;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        // Вызывается и при Time.timeScale = 0 — Input System не зависит от масштаба времени.
        if (isShown)
        {
            // Пока открыт экран настроек — Esc закрывает настройки, а не игру
            var settings = GetComponent<SettingsPanelController>();
            if (settings != null && settings.IsShown)
            {
                settings.Hide();
                return;
            }
            ResumeGame();
        }
        else Show();
    }

    // =====================================================================
    //  ПУБЛИЧНЫЙ API
    // =====================================================================

    public void Show()
    {
        if (isShown) return;
        isShown = true;

        if (pauseGameWhenShown)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (scaleContainer != null)
        {
            scaleContainer.style.display = DisplayStyle.Flex;
            scaleContainer.RemoveFromClassList("hidden");
        }

        GamePaused = true;
        CursorManager.Instance?.UnlockCursor();

        SelectButton(0);
        root?.Focus();
    }

    public void Hide()
    {
        if (!isShown) return;
        isShown = false;

        if (pauseGameWhenShown)
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;

        if (scaleContainer != null)
        {
            scaleContainer.style.display = DisplayStyle.None;
            scaleContainer.AddToClassList("hidden");
        }

        GamePaused = false;
        CursorManager.Instance?.LockCursor();
    }

    public void Toggle() { if (isShown) Hide(); else Show(); }
    public void ResumeGame() => Hide();
    public bool IsShown => isShown;

    /// <summary>Вернуть клавиатурный фокус в меню (вызывается при закрытии настроек).</summary>
    public void FocusMenuRoot()
    {
        if (root == null) return;
        root.focusable = true;
        root.Focus();
    }

    /// <summary>Глобальный флаг паузы — другие скрипты (курсор, ввод) могут по нему ориентироваться.</summary>
    public static bool GamePaused { get; private set; }

    // =====================================================================
    //  СТАТУС-ПАНЕЛЬ — публичные сеттеры (вызывайте из игрового кода)
    // =====================================================================

    public void SetUnitId(string v)  { if (statusUnitId  != null) statusUnitId.text  = $"ЮНИТ ID: {v}"; }
    public void SetSector(string v)  { if (statusSector  != null) statusSector.text  = $"СЕКТОР: {v}"; }
    public void SetClean(string v)   { if (statusClean   != null) statusClean.text   = $"УБОРКА: {v}"; }
    public void SetPaint(string v)   { if (statusPaint   != null) statusPaint.text   = $"КРАСКА: {v}"; }
    public void SetTemp(string v)    { if (statusTemp    != null) statusTemp.text    = $"ТЕМП: {v}"; }
    public void SetGrip(string v)    { if (statusGrip    != null) statusGrip.text    = $"ХВАТ: {v}"; }
    public void SetAnomaly(string v) { if (statusAnomaly != null) statusAnomaly.text = $"АНОМАЛИЯ: {v}"; }
    public void SetUptime(string v)  { if (statusUptime  != null) statusUptime.text  = $"ВРЕМЯ СЕССИИ: {v}"; }

    private void ResetStatusDefaults()
    {
        SetUnitId("CLR-042");
        SetSector("01");
        SetClean("67%");
        SetPaint("42%");
        SetTemp("38°C");
        SetGrip("СТАБИЛЬНО");
        SetAnomaly("НЕТ");
        // UPTIME обновляется в Update()
    }

    // =====================================================================
    //  ДЕЙСТВИЯ — точки расширения (заглушки)
    // =====================================================================

    protected virtual void OnResume()   => ResumeGame();
    protected virtual void OnSettings()
    {
        // Компонент добавится сам, если его ещё нет — настраивать ничего не нужно
        var panel = GetComponent<SettingsPanelController>();
        if (panel == null) panel = gameObject.AddComponent<SettingsPanelController>();
        panel.Show();
    }
    protected virtual void OnRestart()  { Debug.Log("[PAUSE] RESTART TO CHECKPOINT — заглушка."); LoadCheckpoint(); }
    protected virtual void OnExit()      { Debug.Log("[PAUSE] SAVE & EXIT — заглушка."); SaveGameStub(); LoadMainMenu(); }

    private void SaveGameStub()
    {
        // TODO: реализуйте сериализацию прогресса (PlayerPrefs/JSON/ScriptableObjects).
        Debug.Log("[PAUSE] SaveGameStub() — место для реального сохранения.");
    }

    private void LoadCheckpoint()
    {
        string scene = string.IsNullOrEmpty(checkpointSceneName)
            ? SceneManager.GetActiveScene().name
            : checkpointSceneName;

        Time.timeScale = 1f;
        GamePaused = false;
        if (Application.CanStreamedLevelBeLoaded(scene))
            SceneManager.LoadScene(scene);
        else
            Debug.LogError($"[PauseMenu] Сцена чекпоинта '{scene}' не найдена в Build Settings.");
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        GamePaused = false;
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogError($"[PauseMenu] Сцена главного меню '{mainMenuSceneName}' не найдена в Build Settings.");
    }

    // =====================================================================
    //  НАВИГАЦИЯ
    // =====================================================================

    private void SelectButton(int index)
    {
        if (menuButtons == null) return;
        selectedIndex = Mathf.Clamp(index, 0, menuButtons.Length - 1);
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            bool selected = i == selectedIndex;
            menuButtons[i].EnableInClassList("selected", selected);
            menuButtons[i].text = (selected ? ">> " : "    ") + baseTexts[i];
        }
    }

    private void ActivateSelected()
    {
        switch (selectedIndex)
        {
            case 0:  OnResume();   break;
            case 1:  OnSettings(); break;
            case 2:  OnRestart();  break;
            default: OnExit();     break;
        }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        // ESC здесь не обрабатываем — им занимается Input System (OnPausePerformed),
        // иначе было бы двойное срабатывание.

        if (!isShown || menuButtons == null) return;

        switch (evt.keyCode)
        {
            case KeyCode.UpArrow: case KeyCode.W:
                SelectButton((selectedIndex - 1 + menuButtons.Length) % menuButtons.Length); break;
            case KeyCode.DownArrow: case KeyCode.S:
                SelectButton((selectedIndex + 1) % menuButtons.Length); break;
            case KeyCode.Return: case KeyCode.KeypadEnter: case KeyCode.Space:
                ActivateSelected(); break;
            case KeyCode.Alpha1: case KeyCode.Keypad1: SelectButton(0); ActivateSelected(); break;
            case KeyCode.Alpha2: case KeyCode.Keypad2: SelectButton(1); ActivateSelected(); break;
            case KeyCode.Alpha3: case KeyCode.Keypad3: SelectButton(2); ActivateSelected(); break;
            case KeyCode.Alpha4: case KeyCode.Keypad4: SelectButton(3); ActivateSelected(); break;
            default: return;
        }
        evt.StopPropagation();
    }

    // =====================================================================
    //  МИГАЮЩИЙ КУРСОР (как в главном меню)
    // =====================================================================

    private IEnumerator CursorBlink()
    {
        var wait = new WaitForSecondsRealtime(0.5f); // realtime: не замирает при timeScale = 0
        while (true)
        {
            if (cursor != null) cursor.style.opacity = 1;
            yield return wait;
            if (cursor != null) cursor.style.opacity = 0;
            yield return wait;
        }
    }

    // =====================================================================
    //  МАСШТАБ + UPTIME
    // =====================================================================

    private void OnGeometryChanged(GeometryChangedEvent evt)
        => ApplyScale(evt.newRect.width, evt.newRect.height);

    private void ApplyScale(float w, float h)
    {
        if (mainContainer == null) return;
        float scale = Mathf.Min(w / BASE_WIDTH, h / BASE_HEIGHT);
        mainContainer.transform.scale = new Vector3(scale, scale, 1f);
    }

    private void UpdateUptime()
    {
        if (statusUptime == null) return;
        float t = Time.realtimeSinceStartup - sessionStartTime;
        int hh = Mathf.FloorToInt(t / 3600f);
        int mm = Mathf.FloorToInt((t % 3600f) / 60f);
        int ss = Mathf.FloorToInt(t % 60f);
        statusUptime.text = $"ВРЕМЯ СЕССИИ: {hh:00}:{mm:00}:{ss:00}";
    }
}
