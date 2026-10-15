using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";

    private UIDocument uiDocument;
    private VisualElement scaleContainer;

    private Button[] menuButtons;
    private string[] baseTexts;
    private int selectedIndex;

    private VisualElement glitchTop, glitchBottom, glitchLeft, glitchRight;
    private VisualElement glitchBlock1, glitchBlock2;
    private VisualElement cursor;

    private const float BASE_WIDTH = 1920f;
    private const float BASE_HEIGHT = 1080f;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null) return;

        scaleContainer = root.Q<VisualElement>("ScaleContainer");
        glitchTop      = root.Q<VisualElement>("GlitchTop");
        glitchBottom   = root.Q<VisualElement>("GlitchBottom");
        glitchLeft     = root.Q<VisualElement>("GlitchLeft");
        glitchRight    = root.Q<VisualElement>("GlitchRight");
        glitchBlock1   = root.Q<VisualElement>("GlitchBlock1");
        glitchBlock2   = root.Q<VisualElement>("GlitchBlock2");
        cursor         = root.Q<VisualElement>("Cursor");

        var play     = root.Q<Button>("PlayButton");
        var settings = root.Q<Button>("SettingsButton");
        var exit     = root.Q<Button>("ExitButton");
        menuButtons  = new[] { play, settings, exit };

        baseTexts = new string[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
            baseTexts[i] = menuButtons[i].text;

        play.clicked     += OnPlayClicked;
        settings.clicked += OnSettingsClicked;
        exit.clicked     += OnExitClicked;

        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;
            menuButtons[i].focusable = false;
            menuButtons[i].RegisterCallback<MouseEnterEvent>(_ => SelectButton(index));
        }

        root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        SelectButton(0);
        StartCoroutine(GlitchAnimation());
        StartCoroutine(CursorBlink());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        var root = uiDocument.rootVisualElement;
        root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    // ================= Навигация =================

    private void SelectButton(int index)
    {
        if (menuButtons == null) return;
        selectedIndex = Mathf.Clamp(index, 0, menuButtons.Length - 1);
        for (int i = 0; i < menuButtons.Length; i++)
        {
            bool selected = i == selectedIndex;
            menuButtons[i].EnableInClassList("selected", selected);
            menuButtons[i].text = (selected ? ">> " : "    ") + baseTexts[i];
        }
    }

    private void ActivateSelected()
    {
        switch (selectedIndex)
        {
            case 0:  OnPlayClicked(); break;
            case 1:  OnSettingsClicked(); break;
            default: OnExitClicked(); break;
        }
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (menuButtons == null) return;

        switch (evt.keyCode)
        {
            case KeyCode.UpArrow:
            case KeyCode.W:
                SelectButton((selectedIndex - 1 + menuButtons.Length) % menuButtons.Length);
                break;
            case KeyCode.DownArrow:
            case KeyCode.S:
                SelectButton((selectedIndex + 1) % menuButtons.Length);
                break;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Space:
                ActivateSelected();
                break;
            case KeyCode.Escape:
                OnExitClicked();
                break;
            case KeyCode.Alpha1: case KeyCode.Keypad1: SelectButton(0); ActivateSelected(); break;
            case KeyCode.Alpha2: case KeyCode.Keypad2: SelectButton(1); ActivateSelected(); break;
            case KeyCode.Alpha3: case KeyCode.Keypad3: SelectButton(2); ActivateSelected(); break;
            default: return;
        }
        evt.StopPropagation();
    }

    // ================= Действия =================

    private void OnPlayClicked()
    {
        Debug.Log("MENU: START GAME");
        // UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    private void OnSettingsClicked() => Debug.Log("MENU: OPEN SETTINGS");

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================= Масштаб =================

    private void OnGeometryChanged(GeometryChangedEvent evt)
        => ApplyScale(evt.newRect.width, evt.newRect.height);

    private void ApplyScale(float w, float h)
    {
        if (scaleContainer == null) return;
        float scale = Mathf.Min(w / BASE_WIDTH, h / BASE_HEIGHT);
        scaleContainer.style.width  = BASE_WIDTH;
        scaleContainer.style.height = BASE_HEIGHT;
        scaleContainer.style.left   = (w - BASE_WIDTH * scale) * 0.5f;
        scaleContainer.style.top    = (h - BASE_HEIGHT * scale) * 0.5f;
        scaleContainer.transform.scale = new Vector3(scale, scale, 1f);
    }

    // ================= Эффекты =================

    IEnumerator GlitchAnimation()
    {
        while (true)
        {
            if (Random.value < 0.3f) yield return StartCoroutine(TriggerGlitch());
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }

    IEnumerator TriggerGlitch()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                glitchTop.style.opacity = Random.Range(0.3f, 1f);
                glitchTop.style.height = Random.Range(1, 5);
                yield return new WaitForSeconds(0.05f);
                glitchTop.style.opacity = 0;
                break;
            case 1:
                glitchBottom.style.opacity = Random.Range(0.3f, 1f);
                glitchBottom.style.height = Random.Range(1, 5);
                yield return new WaitForSeconds(0.05f);
                glitchBottom.style.opacity = 0;
                break;
            case 2:
                glitchLeft.style.opacity = Random.Range(0.3f, 1f);
                glitchLeft.style.width = Random.Range(1, 4);
                yield return new WaitForSeconds(0.05f);
                glitchLeft.style.opacity = 0;
                break;
            case 3:
                glitchBlock1.style.opacity = Random.Range(0.1f, 0.4f);
                glitchBlock1.style.left = Random.Range(0, (int)BASE_WIDTH);
                glitchBlock1.style.top = Random.Range(0, (int)BASE_HEIGHT);
                glitchBlock1.style.width = Random.Range(50, 300);
                glitchBlock1.style.height = Random.Range(2, 20);
                glitchBlock2.style.opacity = Random.Range(0.1f, 0.3f);
                glitchBlock2.style.left = Random.Range(0, (int)BASE_WIDTH);
                glitchBlock2.style.top = Random.Range(0, (int)BASE_HEIGHT);
                glitchBlock2.style.width = Random.Range(30, 200);
                glitchBlock2.style.height = Random.Range(2, 15);
                yield return new WaitForSeconds(0.08f);
                glitchBlock1.style.opacity = 0;
                glitchBlock2.style.opacity = 0;
                break;
        }
    }

    IEnumerator CursorBlink()
    {
        while (true)
        {
            cursor.style.opacity = 1;
            yield return new WaitForSeconds(0.5f);
            cursor.style.opacity = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }
}