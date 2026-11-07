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
    [Tooltip("Расстояние от пивота до низа коллайдера. Если 0 — вычисляется автоматически из коллайдера при старте (рекомендуется).")]
    public float capsuleBottomOffset = 0f;
    [Tooltip("Запас (skin) ниже капсулы, чтобы не терять землю на кочках/склонах")]
    public float groundSkin = 0.15f;
    [Tooltip("Слои, которые считаются землёй. ~0 = все слои")]
    public LayerMask groundLayers = ~0;

    [Header("Толкание предметов")]
    [Tooltip("Сила толкания тела (Н). Предмет массой M можно толкать не быстрее pushForce / M м/с,\nпоэтому тяжёлые предметы почти не двигаются, лёгкие — как обычно.")]
    public float pushForce = 100f;
    [Tooltip("Слои предметов, которые игрок может толкать телом. ~0 = все слои")]
    public LayerMask pushLayers = ~0;

    [Header("Анимация")]
    [Tooltip("Аниматор робота. Если пуст — найдётся в детях.")]
    public Animator animator;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    // Флаг: "игрок хочет прыгнуть"
    private bool jumpRequested = false;

    // Публичное состояние для других скриптов (звуки шагов и т.п.)
    public bool IsOnGround { get; private set; }
    public bool IsSprinting { get; private set; }

    // События для звука/эффектов: прыжок и приземление (с силой удара, м/с)
    public event System.Action Jumped;
    public event System.Action<float> Landed;

    // Максимальная скорость падения за время полёта (для силы приземления)
    private float airPeakFallSpeed;

    // Был ли на земле в прошлом физическом кадре (для гашения подбросов)
    private bool wasGrounded;
    // Совершён ли прыжок в текущем/прошлом кадре (чтобы не гасить собственный прыжок)
    private bool justJumped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = new PlayerInputActions();

        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Подписываемся на событие прыжка.
        // Это сработает МГНОВЕННО в момент нажатия, независимо от FixedUpdate.
        input.@Player.@Jump.performed += ctx => jumpRequested = true;

        AutoDetectCapsuleBottom();
    }

    /// <summary>
    /// Автоматически вычисляем расстояние от пивота до низа коллайдера.
    /// Это спасает от рассинхрона при смене скейла/размера капсулы:
    /// если offset задан вручную больше реального, луч земли уходит ПОД землю
    /// и прыжок перестаёт работать (grounded всегда false).
    /// </summary>
    private void AutoDetectCapsuleBottom()
    {
        if (capsuleBottomOffset > 0.001f)
            return; // значение задано вручную — не трогаем

        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[PlayerMovements] Не найден коллайдер — проверка земли не будет работать!", this);
            return;
        }

        float distanceToBottom = transform.position.y - col.bounds.min.y;
        if (distanceToBottom > 0.01f)
        {
            capsuleBottomOffset = distanceToBottom;
            Debug.Log($"[PlayerMovements] capsuleBottomOffset вычислен автоматически: {distanceToBottom:0.000}", this);
        }
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

        // Ограничиваем скорость толкания массой предмета перед нами:
        // тяжёлый предмет нельзя разогнать до скорости ходьбы.
        currentSpeed = GetPushLimitedSpeed(moveVector, currentSpeed);

        // 3. Проверяем, стоим ли мы на земле.
        //    Луч от точки чуть выше низа капсулы строго вниз.
        //    Начало луча внутри собственной капсулы не задевает её саму (backfaces не ловятся).
        bool grounded = IsGrounded();

        // Публичное состояние для других скриптов (звуки шагов и т.п.)
        IsOnGround = grounded;
        IsSprinting = isSprinting && moveDirection.magnitude > 0f;

        if (grounded)
        {
            // На земле — полное управление горизонтальной скоростью.
            // Вертикальную скорость сохраняем ТОЛЬКО сразу после прыжка.
            // Иначе при наезде на наклонную поверхность коллизия каждый кадр
            // добавляет +Y, скорость накапливается и игрока подбрасывает как на батуте.
            float verticalVelocity = justJumped
                ? currentLinearVelocity.y
                : Mathf.Min(currentLinearVelocity.y, 0f); // вверх не даём, вниз (гравитация) — можно

            Vector3 newLinearVelocity = new Vector3(
                moveVector.x * currentSpeed, // Используем динамическую скорость
                verticalVelocity,            // Гравитация/прыжок, но без подбросов от коллизий
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

            // Только что оторвались от земли БЕЗ прыжка — это подброс
            // от наклонной поверхности (края пандуса, порога и т.п.). Гасим взлёт.
            if (wasGrounded && !justJumped && currentLinearVelocity.y > 0f)
            {
                currentLinearVelocity.y = 0f;
                rb.linearVelocity = currentLinearVelocity;
            }
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
                justJumped = true;
                Jumped?.Invoke();
            }
        }

        // Сбрасываем флаг прыжка, как только реально оторвались от земли
        // Приземление: были в воздухе — стали на землю.
        // Отдаём силу удара (пиковая скорость падения) — звук/эффекты могут от неё зависеть.
        if (grounded && !wasGrounded)
        {
            Landed?.Invoke(airPeakFallSpeed);
            airPeakFallSpeed = 0f;
        }

        // В полёте запоминаем, насколько сильно падаем
        if (!grounded)
            airPeakFallSpeed = Mathf.Max(airPeakFallSpeed, -currentLinearVelocity.y);

        if (!grounded) justJumped = false;
        wasGrounded = grounded;

        // Кормим аниматор горизонтальной скоростью: Idle <-> Walk
        if (animator != null)
        {
            float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            animator.SetFloat(SpeedParam, horizontalSpeed);
        }
    }

    /// <summary>
    /// Если прямо по ходу движения стоит динамический Rigidbody, ограничивает скорость игрока
    /// величиной pushForce / масса предмета. Чем тяжелее предмет — тем медленнее его можно толкать.
    /// Стены (статичные коллайдеры) не ограничивают — их обрабатывает физика как раньше.
    /// </summary>
    private float GetPushLimitedSpeed(Vector3 moveDirection, float speed)
    {
        if (rb == null || moveDirection.sqrMagnitude < 0.0001f || speed <= 0f)
            return speed;

        // SweepTest — заметающий луч по всем коллайдерам игрока вперёд по движению.
        // Свои коллайдеры игнорируются автоматически.
        float probeDistance = speed * Time.fixedDeltaTime + 0.05f;
        if (rb.SweepTest(moveDirection, out RaycastHit hit, probeDistance, QueryTriggerInteraction.Ignore))
        {
            Rigidbody other = hit.rigidbody;
            if (other != null && !other.isKinematic &&
                ((1 << other.gameObject.layer) & pushLayers.value) != 0)
            {
                float maxPushSpeed = pushForce / Mathf.Max(0.01f, other.mass);
                return Mathf.Min(speed, maxPushSpeed);
            }
        }

        return speed;
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