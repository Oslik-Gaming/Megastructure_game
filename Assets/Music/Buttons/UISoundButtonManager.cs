using UnityEngine;
using UnityEngine.UIElements;

public class UISoundButtonManager : MonoBehaviour
{
    [Header("Компоненты UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Аудиоисточник")]
    [SerializeField] private AudioSource audioSource;

    [Header("Звук Наведения (Hover)")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] [Range(0f, 1f)] private float hoverVolume = 1f;

    [Header("Звук Клика (Click)")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;

    // ==== Доступ к настройкам (для панелей, создаваемых в рантайме — например, настройки) ====
    public AudioClip HoverClip  => hoverSound;
    public AudioClip ClickClip  => clickSound;
    public float    HoverVolume => hoverVolume;
    public float    ClickVolume => clickVolume;
    public AudioSource Source    => audioSource;

    private bool attached; // колбэки уже навешены?

    /// <summary>
    /// Ручная инициализация для документов, созданных в рантайме (экран настроек).
    /// ВАЖНО: AddComponent вызывает OnEnable ДО того, как Setup назначит поля,
    /// поэтому регистрацию кнопок делаем здесь явно (через Attach).
    /// </summary>
    public void Setup(UIDocument document, AudioSource source,
                      AudioClip hover, AudioClip click, float hoverVol, float clickVol)
    {
        uiDocument   = document;
        audioSource  = source;
        hoverSound   = hover;
        clickSound   = click;
        hoverVolume  = hoverVol;
        clickVolume  = clickVol;
        Attach();
    }

    void OnEnable()
    {
        // Для менеджеров из сцены (поля назначены в редакторе) — как раньше.
        // Для рантайм-менеджеров этот вызов ничего не сделает (поля ещё пусты),
        // регистрацию за них выполнит Setup().
        Attach();
    }

    private void Attach()
    {
        if (attached) return;
        // Проверяем, прикреплен ли документ и его корень
        if (uiDocument == null || uiDocument.rootVisualElement == null || audioSource == null) return;

        attached = true;

        var root = uiDocument.rootVisualElement;
        var allButtons = root.Query<Button>().ToList();

        foreach (var button in allButtons)
        {
            button.RegisterCallback<MouseEnterEvent>(OnButtonHover);
            button.clicked += () => OnButtonClick(button);
        }
    }

    void OnDisable()
    {
        if (!attached) return;

        // Безопасная проверка: если UI уже уничтожен Unity при выходе из игры, 
        // мы просто выходим из метода, предотвращая ArgumentNullException
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            attached = false;
            return;
        }

        var root = uiDocument.rootVisualElement;
        var allButtons = root.Query<Button>().ToList();

        foreach (var button in allButtons)
        {
            button.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        }

        attached = false;
    }

    private void OnButtonHover(MouseEnterEvent evt)
    {
        if (hoverSound != null)
        {
            // Передаем вторым параметром индивидуальную громкость hoverVolume
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    private void OnButtonClick(Button button)
    {
        if (clickSound != null)
        {
            // Передаем вторым параметром индивидуальную громкость clickVolume
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }
}
