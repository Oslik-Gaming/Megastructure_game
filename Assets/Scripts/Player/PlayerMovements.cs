using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions input;
    private Rigidbody rb;
    
    [Header("Настройки")]
    public float moveSpeed = 5f;
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
        // 1. Плавное движение (опрос — правильно для непрерывного ввода)
        Vector2 moveDirection = input.@Player.@Movement.ReadValue<Vector2>();

        Vector3 currentLinearVelocity = rb.linearVelocity;

        Vector3 newLinearVelocity = new Vector3(
            moveDirection.x * moveSpeed, 
            currentLinearVelocity.y, 
            moveDirection.y * moveSpeed
        );

        rb.linearVelocity = newLinearVelocity;

        // 2. Прыжок (проверяем флаг, который поставило событие)
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x, 
                jumpForce, 
                rb.linearVelocity.z
            );
            
            // Сбрасываем флаг, чтобы не прыгать бесконечно
            jumpRequested = false;
        }
    }
}