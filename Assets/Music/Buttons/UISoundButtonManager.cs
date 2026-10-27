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

    void OnEnable()
    {
        // Проверяем, прикреплен ли документ и его корень
        if (uiDocument == null || uiDocument.rootVisualElement == null || audioSource == null) return;

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
        // Безопасная проверка: если UI уже уничтожен Unity при выходе из игры, 
        // мы просто выходим из метода, предотвращая ArgumentNullException
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        var root = uiDocument.rootVisualElement;
        var allButtons = root.Query<Button>().ToList();

        foreach (var button in allButtons)
        {
            button.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
        }
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
