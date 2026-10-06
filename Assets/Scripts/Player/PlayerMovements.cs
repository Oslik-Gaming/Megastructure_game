using UnityEngine;

public class PlayerMovements : MonoBehaviour
{
    private PlayerInputActions input;
    private Rigidbody rb;
    
    [Header("Настройки")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 5f;

    // Флаг: "игрок хочет прыгнуть"
    private bool jumpRequested = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = new PlayerInputActions();

        // Подписываемся на событие прыжка.
        // Это сработает МГНОВЕННО в момент нажатия, независимо от FixedUpdate.
        input.@Player.@Jump.performed += ctx => jumpRequested = true;
    }

    private void OnEnable() => input.@Player.Enable();
    private void OnDisable() => input.@Player.Disable();
    private void OnDestroy() => input.Dispose();

    private void FixedUpdate()
    {
        Vector2 moveDirection = input.@Player.@Movement.ReadValue<Vector2>();

        // 1. Считываем, нажата ли кнопка бега. 
        // Для типа Button значение равно 1f при нажатии и 0f при отпускании.
        bool isSprinting = input.@Player.@Sprint.ReadValue<float>() > 0f;

        Vector3 currentLinearVelocity = rb.linearVelocity;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        // Нормализуем, чтобы движение по диагонали не было быстрее
        Vector3 moveVector = (forward * moveDirection.y + right * moveDirection.x).normalized;

        // 2. Выбираем текущую скорость: если бежим и двигаемся, то sprintSpeed, иначе moveSpeed
        // Проверка moveDirection.magnitude > 0 нужна, чтобы анимации/звуки бега (если добавите) 
        // не срабатывали, когда игрок просто стоит и зажимает Shift.
        float currentSpeed = (isSprinting && moveDirection.magnitude > 0f) ? sprintSpeed : moveSpeed;

        Vector3 newLinearVelocity = new Vector3(
            moveVector.x * currentSpeed, // Используем динамическую скорость
            currentLinearVelocity.y,     // Сохраняем вертикальную скорость (гравитацию)
            moveVector.z * currentSpeed  // Используем динамическую скорость
        );

        rb.linearVelocity = newLinearVelocity;

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x, 
                jumpForce, 
                rb.linearVelocity.z
            );
            jumpRequested = false;
        }
    }
}