using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;
using RenderMode = UnityEngine.RenderMode;

[DefaultExecutionOrder(-100)]
public sealed class VRWorldUI : MonoBehaviour
{
    [SerializeField] private UIDocument hud;
    [SerializeField] private PauseMenuController pause;
    [SerializeField] private Font font;
    [SerializeField] private Vector2Int hudResolution = new(1280, 720);
    [SerializeField, Min(0.1f)] private float distance = 1.2f;
    [SerializeField, Min(0.1f)] private float panelWidth = 0.95f;
    [SerializeField, Min(0.1f)] private float followSpeed = 7f;
    [SerializeField, Range(0f, 1f)] private float horrorIntensity = 0.35f;

    private VRPlayerRig rig;
    private Canvas hudCanvas, terminal;
    private RenderTexture hudTexture;
    private PanelSettings clonedSettings;
    private XRRayInteractor ray;
    private GameObject inputRoot;
    private PlayerInputActions input;
    private InputActionReference click;
    private bool wasPaused;
    private Text hint;
    private LineRenderer pointer;
    private Material pointerMaterial;
    private EventSystem previousEvents;
    private EdgeGlitchEffect glitch;

    private void Start()
    {
        rig = VRPlayerRig.Current;
        if (rig == null) { enabled = false; return; }
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        input = VRMode.CreateInput();
        if (hud != null && hud.panelSettings != null)
        {
            // Keep all existing UITK HUD producers; just render their panel onto a world canvas.
            clonedSettings = Instantiate(hud.panelSettings);
            hudTexture = new RenderTexture(hudResolution.x, hudResolution.y, 24, RenderTextureFormat.ARGB32);
            clonedSettings.targetTexture = hudTexture;
            clonedSettings.clearColor = true;
            clonedSettings.colorClearValue = Color.clear;
            foreach (var document in FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (document.panelSettings == hud.panelSettings && document != hud) document.panelSettings = clonedSettings;
            hud.panelSettings = clonedSettings;
            glitch = hud.GetComponent<EdgeGlitchEffect>();
            if (glitch == null) glitch = hud.gameObject.AddComponent<EdgeGlitchEffect>();
            glitch.Intensity = horrorIntensity;
            hudCanvas = MakeCanvas("Visor HUD");
            var image = new GameObject("Visor", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(hudCanvas.transform, false);
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.texture = hudTexture;
            image.raycastTarget = false;
        }
        if (hudCanvas == null) hudCanvas = MakeCanvas("Visor HUD");
        hint = MakeText(hudCanvas.transform, "", -237f, 21);
        hint.text = "Interact: " + BindingLabel(input.Player.Interact) + "   Scan: " + BindingLabel(input.Player.HighlightTrash)
            + "   Tool: " + BindingLabel(input.Player.NextTool) + "   Pause: " + BindingLabel(input.UI.PauseMenu);
        if (pause == null) return;
        terminal = MakeCanvas("GREEN TERMINAL");
        terminal.gameObject.SetActive(false);
        var background = terminal.gameObject.AddComponent<Image>();
        background.color = new Color(0.015f, 0.06f, 0.025f, 0.98f);
        terminal.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        MakeText(terminal.transform, "> MEGASTRUCTURE // VR TERMINAL", 207f, 32);
        AddButton("RESUME", 150f, pause.ResumeGame);
        AddButton("TURN: SNAP / SMOOTH", 100f, rig.ToggleTurnMode);
        AddButton("MOVEMENT VIGNETTE", 50f, rig.ToggleVignette);
        AddButton("RENDER HAND: LEFT / RIGHT", 0f, rig.ToggleHand);
        AddButton("TRACKING: STANDING / SEATED", -50f, rig.ToggleSeated);
        AddButton("HUD GLITCH INTENSITY", -100f, CycleGlitch);
        AddButton("RESTART CHECKPOINT", -150f, pause.RestartFromVR);
        AddButton("SAVE AND EXIT", -200f, pause.ExitFromVR);

        inputRoot = new GameObject("XR Terminal Event System");
        inputRoot.transform.SetParent(transform, false);
        inputRoot.SetActive(false);
        inputRoot.AddComponent<EventSystem>();
        var module = inputRoot.AddComponent<XRUIInputModule>();
        module.enableMouseInput = module.enableTouchInput = module.enableGamepadInput = module.enableJoystickInput = false;
        var manager = inputRoot.AddComponent<XRInteractionManager>();
        var rayObject = new GameObject("XR UI Ray");
        rayObject.transform.SetParent(inputRoot.transform, false);
        ray = rayObject.AddComponent<XRRayInteractor>();
        ray.interactionManager = manager;
        ray.enableUIInteraction = true;
        ray.raycastMask = 0; // UI only
        click = InputActionReference.Create(input.UI.XRClick);
        ray.uiPressInput.inputActionReferencePerformed = click;
        pointer = rayObject.AddComponent<LineRenderer>();
        var lineShader = Resources.Load<Shader>("VRPointer");
        if (lineShader != null) pointer.sharedMaterial = pointerMaterial = new Material(lineShader);
        pointer.positionCount = 2;
        pointer.startWidth = pointer.endWidth = 0.003f;
    }

    private void CycleGlitch()
    {
        horrorIntensity = horrorIntensity >= 0.7f ? 0f : horrorIntensity + 0.35f;
        if (glitch != null) glitch.Intensity = horrorIntensity;
    }

    private void LateUpdate()
    {
        if (hudCanvas == null) return;
        bool paused = terminal != null && pause.IsShown;
        Place(hudCanvas.transform, Time.unscaledDeltaTime * followSpeed);
        hudCanvas.enabled = !paused;
        if (paused != wasPaused)
        {
            if (paused)
            {
                previousEvents = EventSystem.current;
                if (previousEvents != null) previousEvents.enabled = false;
            }
            terminal.gameObject.SetActive(paused);
            inputRoot.SetActive(paused);
            if (paused)
            {
                input.UI.XRClick.Enable();
                Place(terminal.transform, 1f);
            }
            else
            {
                input.UI.XRClick.Disable();
                if (previousEvents != null) previousEvents.enabled = true;
                previousEvents = null;
            }
            wasPaused = paused;
        }
        if (paused)
        {
            ray.transform.SetPositionAndRotation(rig.Right.position, rig.Right.rotation);
            pointer.SetPosition(0, ray.transform.position);
            pointer.SetPosition(1, ray.TryGetCurrentUIRaycastResult(out var hit) ? hit.worldPosition : ray.transform.position + ray.transform.forward * distance * 2f);
        }
    }

    private static string BindingLabel(InputAction action)
    {
        // OpenXR covers several controllers with different printing on the buttons.
        foreach (var control in action.controls)
        {
            if (!control.device.layout.Contains("Touch")) break;
            if (action.name == "Interact") return "A";
            if (action.name == "HighlightTrash") return "B";
            if (action.name == "NextTool") return "X";
            if (action.name == "PauseMenu") return "Menu";
        }
        return action.GetBindingDisplayString(group: VRMode.XR);
    }

    private void Place(Transform panel, float blend)
    {
        Transform head = rig.Head;
        panel.SetPositionAndRotation(Vector3.Lerp(panel.position, head.position + head.forward * distance, blend),
            Quaternion.Slerp(panel.rotation, Quaternion.LookRotation(head.forward, rig.transform.up), blend));
    }

    private Canvas MakeCanvas(string title)
    {
        var result = new GameObject(title, typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
        result.transform.SetParent(transform, false);
        result.renderMode = RenderMode.WorldSpace;
        result.worldCamera = rig.Head.GetComponent<Camera>();
        var rect = (RectTransform)result.transform;
        rect.sizeDelta = new Vector2(960, 540);
        rect.localScale = Vector3.one * (panelWidth / (960f * transform.lossyScale.y));
        Place(rect, 1f);
        return result;
    }

    private Text MakeText(Transform parent, string value, float y, int size)
    {
        var label = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(parent, false);
        label.rectTransform.sizeDelta = new Vector2(920, 52);
        label.rectTransform.anchoredPosition = Vector2.up * y;
        label.text = value;
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.48f, 0.98f, 0.64f);
        label.raycastTarget = false;
        return label;
    }

    private void AddButton(string name, float y, UnityEngine.Events.UnityAction onClick)
    {
        var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(terminal.transform, false);
        var rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(660, 48);
        rect.anchoredPosition = Vector2.up * y;
        button.image.color = new Color(0.12f, 0.26f, 0.16f);
        button.targetGraphic = button.image;
        button.onClick.AddListener(onClick);
        MakeText(button.transform, name, 0f, 27);
    }

    private void OnDestroy()
    {
        if (previousEvents != null) previousEvents.enabled = true;
        input?.Dispose();
        if (click != null) Destroy(click);
        if (hudTexture != null) { hudTexture.Release(); Destroy(hudTexture); }
        if (clonedSettings != null) Destroy(clonedSettings);
        if (pointerMaterial != null) Destroy(pointerMaterial);
    }
}
