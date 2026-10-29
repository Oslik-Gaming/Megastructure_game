using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Настройки")]
    [Tooltip("Блокировать курсор сразу при запуске игры")]
    [SerializeField] private bool lockOnStart = true;

    private void Awake()
    {
        // Стандартная проверка для Синглтона (чтобы не было двух менеджеров)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Применяем начальное состояние
        if (lockOnStart)
        {
            LockCursor();
        }
    }

    /// <summary>
    /// Скрывает и фиксирует курсор в центре (для геймплея)
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Показывает курсор и освобождает его (для меню/паузы)
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Пока игра НЕ на паузе — принудительно держим курсор захваченным.
        // Лечит случай, когда Cursor.lockState = Locked из кода "откладывается"
        // до возврата фокуса у Game view (в Editor курсор пропадает только после клика).
        if (!PauseMenuController.GamePaused &&
            (Cursor.lockState != CursorLockMode.Locked || Cursor.visible))
        {
            LockCursor();
        }
    }

    /// <summary>
    /// ВАЖНАЯ ФИЧА: Если игрок свернул игру (Alt+Tab) и развернул обратно, 
    /// курсор нужно перехватить заново, иначе он будет летать по экрану.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        // Если игра на паузе — курсор нужен свободным для меню.
        if (PauseMenuController.GamePaused)
        {
            UnlockCursor();
            return;
        }

        if (hasFocus)
        {
            LockCursor();
        }
        else
        {
            UnlockCursor();
        }
    }
}