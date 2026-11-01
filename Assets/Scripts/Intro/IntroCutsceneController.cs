using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UIElements;
using TMPro;

/// <summary>
/// IntroCutsceneController — вступительная катсцена в стиле старого терминала (UI Toolkit).
///
/// Текст (introText) постепенно выводится символ за символом. Зажатая Q ускоряет
/// печать, Tab мгновенно выводит весь текст. Когда текст выведен полностью —
/// появляется мигающая подсказка, и нажатие любой кнопки завершает катсцену:
/// включается игрок и начинается игра.
///
/// Настройка сцены:
///  1. Пустой объект "IntroCutscene" в SampleScene.
///  2. На него UIDocument (Panel Settings = PanelSettings.asset, Source = IntroCutscene.uxml).
///  3. Туда же этот скрипт; в playerRoot — объект игрока (робота).
///
/// Звуки: вешай на события onCharacterTyped (звук печати) и onIntroFinished
/// (конец эмбиента и т.п.) через инспектор.
/// </summary>
public class IntroCutsceneController : MonoBehaviour
{
    [Header("Игрок")]
    [Tooltip("Скрипты управления игроком, которые отключаются на время катсцены\n(движение, обзор и т.п.). НЕ отключаем сам объект игрока — иначе в сцене\nне останется ни одной камеры и Unity будет писать 'No cameras rendering'.")]
    [SerializeField] private Behaviour[] playerScriptsToDisable;

    [Header("Текст катсцены (пиши здесь)")]
    [TextArea(10, 30)]
    [SerializeField] private string introText =
        "> BOOT SEQUENCE INITIALIZED...\n\n" +
        "> ЯДРО... OK\n> ОПТИКА... OK\n> СЕРВОПРИВОДЫ... OK\n\n" +
        "> ПОДЪЁМ ИЗ СПЯЩЕГО РЕЖИМА ЧЕРЕЗ 3... 2... 1...";

    [Header("Скорость")]
    [Tooltip("Символов в секунду при обычной печати.")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 25f;
    [Tooltip("Во сколько раз ускоряется печать, пока зажата Q.")]
    [SerializeField, Min(1f)] private float speedUpMultiplier = 5f;

    [Header("Подсказка после текста")]
    [SerializeField] private string promptText = "Нажмите на любую кнопку чтобы продолжить...";
    [Tooltip("Период мигания подсказки, сек.")]
    [SerializeField, Min(0.1f)] private float blinkPeriod = 1f;

    [Header("Финал: включение робота")]
    [Tooltip("Длительность затемнения экрана после нажатия кнопки, сек. Затем экран резко включается.")]
    [SerializeField, Min(0.05f)] private float blackoutDuration = 0.5f;

    [Header("Шрифт")]
    [Tooltip("Обычный Font (перетащи ttf/otf файл). Это основной способ задать шрифт:")]
    [SerializeField] private Font font;
    [Tooltip("TMP Font Asset — используется его исходный ttf (sourceFontFile), т.к. UI Toolkit\nне поддерживает TMP-шрифты напрямую. Сработает, только если при создании\nTMP-ассета был указан исходный файл шрифта.")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [Tooltip("Размер шрифта основного текста.")]
    [SerializeField] private float terminalFontSize = 32f;
    [Tooltip("Размер шрифта подсказки внизу.")]
    [SerializeField] private float promptFontSize = 22f;

    [Header("Отладка")]
    [Tooltip("Всегда показывать катсцену, независимо от прогресса (для тестов).")]
    [SerializeField] private bool debugAlwaysShowIntro = false;
    [Tooltip("Клавиша сброса прогресса прямо в Play-режиме (F9).")]
    [SerializeField] private bool debugResetKeyEnabled = true;

    [Header("Звук печати (опционально, можно прямо здесь)")]
    [Tooltip("AudioSource со звуком печати. Если clip зациклен (loop) — играет один раз до конца катсцены;\nесли не зациклен — короткий щелчок на каждый символ.")]
    [SerializeField] private AudioSource typingSound;

    [Header("События")]
    [Tooltip("Вызывается на каждый печатаемый символ (кроме пробелов) — сюда звук печати.")]
    public UnityEvent onCharacterTyped;
    [Tooltip("Вызывается один раз при полном завершении катсцены.")]
    public UnityEvent onIntroFinished;

    private VisualElement introRoot;
    private Label terminalLabel;
    private Label promptLabel;
    private VisualElement blackout;

    private int visibleChars;
    private float typeTimer;
    private bool textFullyShown;
    private bool finished;
    private bool blackoutStarted;
    private float blackoutTimer;

    private void Start()
    {
        // Start вызывается ПОСЛЕ всех OnEnable — rootVisualElement у UIDocument уже готов.
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogError("[Intro] На объекте нет UIDocument с IntroCutscene.uxml! Отключаю его, чтобы он не закрывал экран.");
            if (uiDocument != null) uiDocument.enabled = false;
            enabled = false;
            return;
        }

        var root = uiDocument.rootVisualElement;
        introRoot = root.Q<VisualElement>("IntroRoot");
        terminalLabel = root.Q<Label>("TerminalLabel");
        promptLabel = root.Q<Label>("PromptLabel");
        blackout = root.Q<VisualElement>("Blackout");

        if (introRoot == null || terminalLabel == null || promptLabel == null || blackout == null)
        {
            Debug.LogError("[Intro] В IntroCutscene.uxml не найдены IntroRoot/TerminalLabel/PromptLabel! Отключаю UIDocument.");
            uiDocument.enabled = false;
            enabled = false;
            return;
        }

        // Решаем: показывать интро или сразу играть
        bool newGame = GameProgress.ConsumeNewGameRequest();
        bool showIntro = debugAlwaysShowIntro || newGame || !GameProgress.HasSeenIntro;

        Debug.Log($"[Intro] newGame={newGame}, seen={GameProgress.HasSeenIntro}, debugAlwaysShowIntro={debugAlwaysShowIntro} => showIntro={showIntro}");

        if (!showIntro)
        {
            Finish(skipUiEvents: true);
            return;
        }

        // Готовим катсцену: отключаем только управление, камера продолжает работать
        SetPlayerControl(false);

        introRoot.style.display = DisplayStyle.Flex;
        terminalLabel.text = string.Empty;
        promptLabel.text = promptText;
        promptLabel.style.display = DisplayStyle.None;

        ApplyFont();

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        Debug.Log($"[Intro] Катсцена запущена, символов в тексте: {introText.Length}");
    }

    private void Update()
    {
        if (finished) return;

        // Фаза затемнения: чёрный плавно нарастает, потом резко выкл — "робот включился"
        if (blackoutStarted)
        {
            blackoutTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blackoutTimer / blackoutDuration);
            blackout.style.opacity = t;

            if (t >= 1f)
                Finish(skipUiEvents: false); // резкий срез в игру
            return;
        }

        // F9 — сброс прогресса прямо в Play-режиме
        if (debugResetKeyEnabled)
        {
            var kbAny = Keyboard.current;
            if (kbAny != null && kbAny.f9Key.wasPressedThisFrame)
            {
                GameProgress.ResetProgress();
                Debug.Log("[Intro] Прогресс сброшен (F9). Перезапусти сцену для повторного интро.");
            }
        }

        if (terminalLabel == null || string.IsNullOrEmpty(introText))
            textFullyShown = true;

        if (!textFullyShown)
        {
            float speed = charactersPerSecond;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame)
                    speed = float.MaxValue;          // Tab — показать весь текст
                else if (kb.qKey.isPressed)
                    speed *= speedUpMultiplier;      // Q — ускорение
            }

            typeTimer += Time.deltaTime * speed;
            while (typeTimer >= 1f && visibleChars < introText.Length)
            {
                typeTimer -= 1f;
                visibleChars++;
                if (!char.IsWhiteSpace(introText[visibleChars - 1]))
                {
                    onCharacterTyped?.Invoke();
                    PlayTypingSound();
                }
            }

            terminalLabel.text = introText.Substring(0, visibleChars);
            textFullyShown = visibleChars >= introText.Length;
            return;
        }

        // Текст показан полностью: мигающая подсказка, любая кнопка — продолжить
        promptLabel.style.display = DisplayStyle.Flex;
        promptLabel.style.opacity = (Time.time % blinkPeriod) < blinkPeriod * 0.6f ? 1f : 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            StartBlackout();
    }

    private void StartBlackout()
    {
        blackoutStarted = true;
        blackoutTimer = 0f;

        // Убираем текст и подсказку — остаётся только чистый чёрный
        terminalLabel.style.opacity = 0f;
        promptLabel.style.display = DisplayStyle.None;
    }

    private void Finish(bool skipUiEvents)
    {
        finished = true;

        GameProgress.MarkIntroSeen();

        // Резко прячем катсцену целиком — игра появляется мгновенно, как будто
        // у робота включились глаза
        if (introRoot != null) introRoot.style.display = DisplayStyle.None;
        SetPlayerControl(true);

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (typingSound != null) typingSound.Stop();

        if (!skipUiEvents)
            onIntroFinished?.Invoke();

        Debug.Log("[Intro] Катсцена завершена, прогресс сохранён.");
    }

    private void ApplyFont()
    {
        StyleFontDefinition fontDef;

        if (font != null)
        {
            // Обычный Font — основной путь
            fontDef = FontDefinition.FromFont(font);
        }
        else if (fontAsset != null && fontAsset.sourceFontFile != null)
        {
            // TMP-ассет конвертируем через его исходный ttf
            fontDef = FontDefinition.FromFont(fontAsset.sourceFontFile);
        }
        else
        {
            // Ничего не задано — остаётся шрифт из USS
            terminalLabel.style.fontSize = terminalFontSize;
            promptLabel.style.fontSize = promptFontSize;
            return;
        }

        terminalLabel.style.unityFontDefinition = fontDef;
        promptLabel.style.unityFontDefinition = fontDef;
        terminalLabel.style.fontSize = terminalFontSize;
        promptLabel.style.fontSize = promptFontSize;
    }

    private void SetPlayerControl(bool enabled)
    {
        if (playerScriptsToDisable == null) return;
        foreach (var behaviour in playerScriptsToDisable)
        {
            if (behaviour != null) behaviour.enabled = enabled;
        }
    }

    private void PlayTypingSound()
    {
        if (typingSound == null || typingSound.clip == null) return;

        if (typingSound.loop)
        {
            // Зацикленный шум печатной машинки: запускаем один раз
            if (!typingSound.isPlaying) typingSound.Play();
        }
        else
        {
            // Одиночные щелчки на каждый символ
            typingSound.pitch = Random.Range(0.95f, 1.05f);
            typingSound.PlayOneShot(typingSound.clip);
        }
    }

#if UNITY_EDITOR
    /// <summary>ПКМ по заголовку компонента в инспекторе → сбросить прогресс интро.</summary>
    [ContextMenu("Сбросить прогресс интро")]
    private void ResetIntroProgress()
    {
        GameProgress.ResetProgress();
        Debug.Log("[Intro] Прогресс сброшен через ContextMenu.");
    }
#endif
}
