using UnityEngine;

public class PlayerMovements : MonoBehaviour
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
        Vector2 moveDirection = input.@Player.@Movement.ReadValue<Vector2>();

        Vector3 currentLinearVelocity = rb.linearVelocity;

        // Получаем локальные оси игрока
        Vector3 forward = transform.forward;  // Куда смотрит игрок (вперёд)
        Vector3 right = transform.right;      // Вправо относительно игрока

        // Формируем направление движения относительно игрока
        // moveDirection.y — это вперёд/назад (W/S)
        // moveDirection.x — это вправо/влево (A/D)
        Vector3 moveVector = (forward * moveDirection.y + right * moveDirection.x).normalized;

        Vector3 newLinearVelocity = new Vector3(
            moveVector.x * moveSpeed, 
            currentLinearVelocity.y, 
            moveVector.z * moveSpeed
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