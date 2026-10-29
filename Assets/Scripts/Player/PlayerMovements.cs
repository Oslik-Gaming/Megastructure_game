using UnityEngine;

public class PlayerMovements : MonoBehaviour
{
    private PlayerInputActions input;
    private Rigidbody rb;
    
    [Header("Настройки")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 5f;

    [Header("Управление в воздухе")]
    [Tooltip("Скорость реакции направления в полёте (больше = резче управление). 0 = полная инерция.")]
    public float airControl = 8f;

    [Header("Проверка земли")]
    [Tooltip("Расстояние от пивота до низа капсулы (Body: height 2, center 0 → низ на 1.0 ниже пивота)")]
    public float capsuleBottomOffset = 1.0f;
    [Tooltip("Запас (skin) ниже капсулы, чтобы не терять землю на кочках/склонах")]
    public float groundSkin = 0.15f;
    [Tooltip("Слои, которые считаются землёй. ~0 = все слои")]
    public LayerMask groundLayers = ~0;

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

        // 3. Проверяем, стоим ли мы на земле.
        //    Луч от точки чуть выше низа капсулы строго вниз.
        //    Начало луча внутри собственной капсулы не задевает её саму (backfaces не ловятся).
        bool grounded = IsGrounded();

        if (grounded)
        {
            // На земле — полное управление горизонтальной скоростью
            Vector3 newLinearVelocity = new Vector3(
                moveVector.x * currentSpeed, // Используем динамическую скорость
                currentLinearVelocity.y,     // Сохраняем вертикальную скорость (гравитацию)
                moveVector.z * currentSpeed  // Используем динамическую скорость
            );

            rb.linearVelocity = newLinearVelocity;
        }
        else
        {
            // В ВОЗДУХЕ: направление движения управляется игроком,
            // но модуль скорости (инерция) сохраняется.
            //
            //  - Спринт в прыжке НЕ работает: целевая скорость — moveSpeed.
            //  - Но если игрок разогнался спринтом ДО прыжка, эта скорость
            //    НЕ гасится — инерция полёта сохраняется (Mathf.Max).
            //  - Без ввода горизонтальная скорость не трогается вообще.

            if (moveVector.sqrMagnitude > 0.0001f)
            {
                Vector3 horizontal = new Vector3(currentLinearVelocity.x, 0f, currentLinearVelocity.z);
                float currentHorizontalSpeed = horizontal.magnitude;

                // Целевая скорость: обычная ходьба, но набранная инерция (спринт до прыжка) сохраняется
                float targetSpeed = Mathf.Max(moveSpeed, currentHorizontalSpeed);

                // Плавно поворачиваем горизонтальный вектор к желаемому направлению,
                // сохраняя его длину — инерция есть, но направление ей не диктуется
                Vector3 desired = moveVector * targetSpeed;
                Vector3 steered = Vector3.Lerp(horizontal, desired, airControl * Time.fixedDeltaTime);

                currentLinearVelocity.x = steered.x;
                currentLinearVelocity.z = steered.z;
                rb.linearVelocity = currentLinearVelocity;
            }
            // Гравитация по Y работает как обычно.
        }

        if (jumpRequested)
        {
            // Сбрасываем флаг ВСЕГДА: нажатие в воздухе не должно "ждать" приземления
            jumpRequested = false;

            // Прыгаем ТОЛЬКО с земли — иначе получался бесконечный прыжок
            if (grounded)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    jumpForce,
                    rb.linearVelocity.z
                );
            }
        }
    }

    /// <summary>
    /// Стоит ли игрок на земле: луч вниз от точки чуть выше низа капсулы.
    /// Свои собственные коллайдеры (Body, меши робота и т.п.) игнорируем.
    /// </summary>
    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.down * (capsuleBottomOffset - 0.1f);
        float rayLength = 0.1f + groundSkin;

        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, rayLength, groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider col = groundHits[i].collider;
            // пропускаем коллайдеры самого игрока
            if (col.attachedRigidbody == rb || col.transform.IsChildOf(transform))
                continue;
            return true;
        }
        return false;
    }

    private readonly RaycastHit[] groundHits = new RaycastHit[8];
}