using UnityEngine;

public enum RotationAxis
{
    X = 0,
    Y = 1,
    Z = 2
}

public class PlayerHeadLook : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Объект, который вращается мышью по горизонтали (обычно корень игрока). Можно оставить пустым.")]
    public Transform bodyTransform;
    [Tooltip("Объект головы, который вращается по вертикали")]
    public Transform headPoint;

    [Header("Оси вращения")]
    [Tooltip("Ось, вокруг которой вращается тело при движении мыши влево/вправо")]
    public RotationAxis horizontalAxis = RotationAxis.Y;
    [Tooltip("Ось, вокруг которой вращается голова при движении мыши вперёд/назад")]
    public RotationAxis verticalAxis = RotationAxis.Z;

    [Header("Ограничения угла головы")]
    [Tooltip("Минимальный угол поворота головы")]
    public float minHeadRotationAngle = -80f;
    [Tooltip("Максимальный угол поворота головы")]
    public float maxHeadRotationAngle = 80f;

    [Header("Инверсия")]
    [Tooltip("Инвертировать горизонтальный поворот (мышь вправо — тело влево)")]
    public bool invertHorizontal = false;
    [Tooltip("Инвертировать вертикальный поворот (мышь вверх — голова вниз)")]
    public bool invertVertical = false;

    // Чувствительность мыши берётся из GameSettings (экран настроек, PlayerPrefs).

    // Стартовый локальный угол головы по выбранной оси (StartAngle)
    private float startAngle = 0f;
    // Текущий накопленный угол поворота головы, зажатый в диапазоне [-80..80]
    private float headRotationAngle = 0f;

    private PlayerInputActions input;

    private void Awake()
    {
        input = new PlayerInputActions();

        if (bodyTransform == null) bodyTransform = transform;

        // Если HeadPoint не назначен, ищем его в иерархии
        if (headPoint == null)
        {
            headPoint = transform.Find("Body/HeadPoint");
            if (headPoint == null)
            {
                Debug.LogError("HeadPoint не найден! Проверьте иерархию.");
            }
        }
    }

    private void Start()
    {
        if (headPoint != null)
        {
            // Запоминаем стартовый угол по выбранной оси и приводим к диапазону [-180..180]
            float a = GetAxisValue(headPoint.localEulerAngles, verticalAxis);
            startAngle = a > 180f ? a - 360f : a;
            headRotationAngle = 0f;
        }
    }

    private void OnEnable() => input.@Player.Enable();
    private void OnDisable() => input.@Player.Disable();
    private void OnDestroy() => input.Dispose();

    private void Update()
    {
        // На паузе (меню ESC) ввод мыши полностью игнорируем —
        // голова не должна реагировать на движение мыши.
        if (Time.timeScale <= 0f) return;

        LookAround();
    }

    private void LookAround()
    {
        // Считываем движение мыши
        Vector2 mouseDelta = input.@Player.@Look.ReadValue<Vector2>();

        // Чувствительность читаем каждый кадр — слайдер в настройках применяется сразу
        float sensitivity = GameSettings.SensitivityMultiplier;

        // 1. Горизонтальный поворот тела вокруг выбранной оси (без ограничений)
        if (bodyTransform != null)
        {
            float horizontal = mouseDelta.x * sensitivity * (invertHorizontal ? -1f : 1f);
            bodyTransform.Rotate(AxisVector(horizontalAxis), horizontal, Space.Self);
        }

        // 2. Вертикальный угол головы: накопление + жёсткое ограничение [-80..80]
        headRotationAngle -= mouseDelta.y * sensitivity * (invertVertical ? -1f : 1f);
        headRotationAngle = Mathf.Clamp(headRotationAngle, minHeadRotationAngle, maxHeadRotationAngle);

        // 3. Применяем угол к голове ТОЛЬКО по выбранной оси.
        //    Остальные оси остаются нетронутыми — ничего лишнего не вращается.
        if (headPoint != null)
        {
            Vector3 euler = headPoint.localEulerAngles;

            // Итоговый угол: StartAngle + HeadRotationAngle, переводим в диапазон [0..360)
            float target = startAngle + headRotationAngle;
            SetAxisValue(ref euler, verticalAxis, Mathf.Repeat(target, 360f));

            headPoint.localEulerAngles = euler;
        }
    }

    // --- Вспомогательные методы для работы с выбранной осью ---

    private static Vector3 AxisVector(RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            default: return Vector3.forward;
        }
    }

    private static float GetAxisValue(Vector3 euler, RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.X: return euler.x;
            case RotationAxis.Y: return euler.y;
            default: return euler.z;
        }
    }

    private static void SetAxisValue(ref Vector3 euler, RotationAxis axis, float value)
    {
        switch (axis)
        {
            case RotationAxis.X: euler.x = value; break;
            case RotationAxis.Y: euler.y = value; break;
            default: euler.z = value; break;
        }
    }
}
