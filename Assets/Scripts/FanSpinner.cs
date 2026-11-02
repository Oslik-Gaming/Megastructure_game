using UnityEngine;

/// <summary>
/// FanSpinner — постоянное вращение объекта (лопастей вентилятора).
///
/// Крутится всегда, пока объект активен. Настройки:
///  - Axis            — ось вращения (обычно локальная Z для лопастей);
///  - Rotation Speed  — скорость в градусах в секунду;
///  - Direction       — направление: Forward (по часовой при взгляде против оси) / Reverse;
///  - Spin Up Time    — плавный разгон из состояния покоя (0 = крутится сразу на полную скорость);
///  - Spin Down Time  — плавная остановка при выключении компонента (0 = мгновенно);
///  - Use Unscaled Time — крутиться даже при Time.timeScale = 0 (паузы, катсцены).
///
/// Вешается на объект с лопастями (например, на fan_Centered).
/// </summary>
public class FanSpinner : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public enum SpinDirection { Forward, Reverse }

    [Header("Вращение")]
    [Tooltip("Ось вращения (в локальных координатах объекта).")]
    [SerializeField] private Axis axis = Axis.Z;
    [Tooltip("Скорость вращения, градусов в секунду. 360 = один оборот в секунду.")]
    [SerializeField] private float rotationSpeed = 180f;
    [Tooltip("Направление вращения.")]
    [SerializeField] private SpinDirection direction = SpinDirection.Forward;

    [Header("Разгон и остановка")]
    [Tooltip("Сколько секунд разгоняется до полной скорости (0 = сразу).")]
    [SerializeField, Min(0f)] private float spinUpTime = 0f;
    [Tooltip("Сколько секунд останавливается после выключения компонента (0 = сразу).")]
    [SerializeField, Min(0f)] private float spinDownTime = 0f;

    [Header("Прочее")]
    [Tooltip("Крутиться даже когда время игры остановлено (timeScale = 0).")]
    [SerializeField] private bool useUnscaledTime = false;
    [Tooltip("Случайный стартовый угол, чтобы несколько вентиляторов не крутились синхронно.")]
    [SerializeField] private bool randomStartAngle = false;

    private float currentSpeed;      // текущая скорость с учётом разгона/торможения
    private bool stopping;
    private Vector3 AxisVector => axis switch
    {
        Axis.X => Vector3.right,
        Axis.Y => Vector3.up,
        _ => Vector3.forward,
    };

    private void Awake()
    {
        if (randomStartAngle)
            transform.localRotation = Quaternion.AngleAxis(Random.Range(0f, 360f), AxisVector) * transform.localRotation;

        currentSpeed = spinUpTime > 0f ? 0f : rotationSpeed;
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (stopping)
        {
            // Плавная остановка
            if (spinDownTime <= 0f)
                currentSpeed = 0f;
            else
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, rotationSpeed / spinDownTime * dt);
        }
        else
        {
            // Плавный разгон
            if (spinUpTime <= 0f)
                currentSpeed = rotationSpeed;
            else
                currentSpeed = Mathf.MoveTowards(currentSpeed, rotationSpeed, rotationSpeed / spinUpTime * dt);
        }

        if (currentSpeed <= 0f)
        {
            if (stopping) enabled = false; // полностью остановились
            return;
        }

        float sign = direction == SpinDirection.Forward ? 1f : -1f;
        transform.Rotate(AxisVector, currentSpeed * sign * dt, Space.Self);
    }

    /// <summary>Запустить плавную остановку и выключить компонент после неё.</summary>
    public void Stop()
    {
        stopping = true;
    }

    /// <summary>Возобновить вращение после Stop().</summary>
    public void Resume()
    {
        stopping = false;
        enabled = true;
    }

    private void OnDisable()
    {
        // Если выключили компонент извне — обнуляем скорость, чтобы при включении
        // разгон начинался заново
        stopping = false;
        currentSpeed = spinUpTime > 0f ? 0f : rotationSpeed;
    }
}
