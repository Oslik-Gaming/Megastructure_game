using UnityEngine;

public class PlayerHeadLook : MonoBehaviour
{
    [Header("Настройки")]
    public Transform headPoint; // Ссылка на HeadPoint
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    private PlayerInputActions input;
    private float verticalRotation = 0f;

    private void Awake()
    {
        input = new PlayerInputActions();

        // Если HeadPoint не назначен, ищем его в иерархии
        if (headPoint == null)
        {
            headPoint = transform.Find("Body/HeadPoint");
            if (headPoint == null)
            {
                Debug.LogError("HeadPoint не найден! Проверьте иерархию.");
            }
        }

        CursorManager.Instance.LockCursor();
    }

    private void OnEnable() => input.@Player.Enable();
    private void OnDisable() => input.@Player.Disable();
    private void OnDestroy() => input.Dispose();

    private void Update()
    {
        LookAround();
    }

    private void LookAround()
    {
        // Считываем движение мыши
        Vector2 mouseDelta = input.@Player.@Look.ReadValue<Vector2>();

        // 1. Поворот ВСЕГО игрока по горизонтали (ось Y)
        float horizontalRotation = mouseDelta.x * mouseSensitivity;
        transform.Rotate(0f, horizontalRotation, 0f);

        // 2. Наклон HeadPoint по вертикали (ось X) с ограничениями
        verticalRotation -= mouseDelta.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        
        // Применяем наклон к HeadPoint
        headPoint.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
    }
}