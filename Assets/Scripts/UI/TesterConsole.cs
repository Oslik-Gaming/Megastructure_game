using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
#endif

[DisallowMultipleComponent]
internal sealed class TesterConsole : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static TesterConsole activeConsole;
    public static bool IsOpen => activeConsole != null;

    private readonly Dictionary<string, Func<string>> commands = new(StringComparer.OrdinalIgnoreCase);
    private PlayerMovements player;
    private InputAction toggleAction;
    private UIDocument overlay;
    private PanelSettings panelSettings;
    private TextField commandInput;
    private Label status;

    private void Awake()
    {
        player = GetComponent<PlayerMovements>();
        commands.Add("fly", ToggleFlight);
        // Bind the physical key above Tab, independent of the current keyboard layout.
        toggleAction = new InputAction("TesterConsoleToggle", InputActionType.Button, "<Keyboard>/backquote");
        toggleAction.performed += OnToggle;
    }

    private void OnEnable() => toggleAction.Enable();

    private void Update()
    {
        if (activeConsole == this && (!player.isActiveAndEnabled || PauseMenuController.GamePaused))
            CloseIfOpen();
    }

    private void OnToggle(InputAction.CallbackContext _)
    {
        if (activeConsole == this)
            CloseIfOpen();
        else if (!IsOpen && player.isActiveAndEnabled && !PauseMenuController.GamePaused && Time.timeScale > 0f)
            Open();
    }

    private void Open()
    {
        if (overlay == null && !CreateOverlay()) return;
        activeConsole = this;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        commandInput.SetValueWithoutNotify(string.Empty);
        overlay.rootVisualElement.style.display = DisplayStyle.Flex;
        commandInput.schedule.Execute(() =>
        {
            if (activeConsole == this) commandInput.Focus();
        });
    }

    public static bool CloseIfOpen()
    {
        if (!IsOpen) return false;
        var console = activeConsole;
        activeConsole = null;
        if (console.overlay != null)
        {
            console.commandInput.Blur();
            console.overlay.rootVisualElement.style.display = DisplayStyle.None;
        }

        if (!PauseMenuController.GamePaused && console.player != null && console.player.isActiveAndEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        return true;
    }

    private bool CreateOverlay()
    {
        // Reuse the scene's UI Toolkit theme but put the console on its own topmost panel.
        PanelSettings source = null;
        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            if (doc.panelSettings == null || doc.panelSettings.targetTexture != null) continue;
            if (source == null || doc.panelSettings.sortingOrder > source.sortingOrder)
                source = doc.panelSettings;
        }

        if (source == null)
        {
            Debug.LogWarning("[TesterConsole] No screen-space UI Toolkit panel found.", this);
            return false;
        }

        panelSettings = Instantiate(source);
        panelSettings.sortingOrder = source.sortingOrder + 100;

        var overlayObject = new GameObject("Tester Console UI");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlayObject, gameObject.scene);
        overlay = overlayObject.AddComponent<UIDocument>();
        overlay.panelSettings = panelSettings;

        var root = overlay.rootVisualElement;
        var shield = new VisualElement();
        shield.StretchToParentSize();
        root.Add(shield);
        // Clicks outside the input stay on the console panel instead of reaching the HUD.
        shield.RegisterCallback<PointerDownEvent>(_ => commandInput.Focus());

        var box = new VisualElement();
        box.style.marginTop = 20;
        box.style.marginLeft = 20;
        box.style.width = new Length(85, LengthUnit.Percent);
        box.style.maxWidth = 560;
        box.style.paddingTop = box.style.paddingBottom = 12;
        box.style.paddingLeft = box.style.paddingRight = 14;
        box.style.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.98f);
        box.style.color = Color.white;
        box.style.fontSize = 18;
        shield.Add(box);

        box.Add(new Label("TESTER CONSOLE  /  ~ to close"));
        status = new Label("fly — toggle flight");
        status.style.whiteSpace = WhiteSpace.Normal;
        box.Add(status);

        commandInput = new TextField { maxLength = 80 };
        commandInput.Q(className: TextField.inputUssClassName).style.color = Color.black;
        commandInput.RegisterCallback<KeyDownEvent>(OnCommandKeyDown, TrickleDown.TrickleDown);
        box.Add(commandInput);

        root.style.display = DisplayStyle.None;
        return true;
    }

    private void OnCommandKeyDown(KeyDownEvent evt)
    {
        if (evt.character is '`' or '~' or 'ё' or 'Ё')
        {
            evt.StopImmediatePropagation();
            return;
        }
        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
        evt.StopImmediatePropagation();
        var command = commandInput.value.Trim();
        if (command.Length == 0) return;

        status.text = commands.TryGetValue(command, out var run)
            ? run()
            : "Unknown command. Available: fly";
        commandInput.SetValueWithoutNotify(string.Empty);
    }

    private string ToggleFlight()
    {
        if (!player.isActiveAndEnabled) return "Player controls are unavailable.";
        player.SetFlying(!player.IsFlying);
        return player.IsFlying ? "fly ON  /  WASD · Space up · Ctrl down · Shift boost" : "fly OFF";
    }

    private void OnDisable()
    {
        toggleAction.Disable();
        if (activeConsole == this) CloseIfOpen();
    }

    private void OnDestroy()
    {
        toggleAction.Dispose();
        if (overlay != null) Destroy(overlay.gameObject);
        if (panelSettings != null) Destroy(panelSettings);
    }
#else
    public static bool IsOpen => false;
    public static bool CloseIfOpen() => false;
#endif
}
