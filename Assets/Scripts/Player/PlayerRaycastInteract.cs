using UnityEngine;
using UnityEngine.InputSystem; // Обязательно добавь этот using для New Input System!

public class PlayerRaycastInteract : MonoBehaviour
{
    private enum GrabState
    {
        None,
        Docking,
        Holding
    }

    [Header("Настройки взаимодействия")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Transform rayOrigin;

    [Header("Захват предметов")]
    [SerializeField] private float holdDistance = 1.2f;
    [SerializeField] private float dockPositionSpeed = 10f;
    [SerializeField] private float holdPositionSpeed = 14f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float weightSlowdown = 0.35f;
    [SerializeField] private float movementOffsetDistance = 0.25f;
    [SerializeField] private float maxSpeedForOffset = 4f;

    [Header("Бросок и опускание")]
    [SerializeField] private float baseThrowSpeed = 8f;
    [SerializeField] private float throwWeightSlowdown = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float playerVelocityTransfer = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float placeVelocityTransfer = 0.2f;
    [SerializeField] private float throwSpin = 2f;

    // Ссылка на наш сгенерированный класс ввода
    private PlayerInputActions inputActions;
    private GrabState grabState;
    private Grabable carriedItem;
    private Transform grabSide;
    private Vector3 smoothedMovementOffset;
    private Rigidbody playerBody;
    private Collider[] playerColliders;

    private void Awake()
    {
        // Создаем экземпляр сгенерированного класса
        inputActions = new PlayerInputActions();
        playerBody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable()
    {
        // Включаем карту действий (Action Map)
        // Примечание: "Player" и "Interact" - это имена, которые ты задал в Input Action Asset
        inputActions.Player.Enable();
        
        // Подписываемся на событие нажатия кнопки Interact
        inputActions.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        // Отписываемся от события, чтобы избежать утечек памяти и ошибок
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        
        // Выключаем карту действий
        inputActions.Player.Disable();

        if (carriedItem != null)
            Release(Vector3.zero, false);
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void Update()
    {
        if (carriedItem == null)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            ThrowCurrent();
        else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            PlaceCurrent();
    }

    private void FixedUpdate()
    {
        if (carriedItem == null || carriedItem.Body == null)
            return;

        Rigidbody body = carriedItem.Body;
        float weightFactor = 1f + carriedItem.Weight * weightSlowdown;
        float positionSpeed = (grabState == GrabState.Docking ? dockPositionSpeed : holdPositionSpeed) / weightFactor;
        float turnSpeed = rotationSpeed / weightFactor;
        Vector3 targetPosition = GetHoldTargetPosition();
        Quaternion targetRotation = GetHoldTargetRotation();

        body.MovePosition(Vector3.Lerp(body.position, targetPosition, positionSpeed * Time.fixedDeltaTime));
        body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));

        if (grabState == GrabState.Docking &&
            Vector3.Distance(body.position, targetPosition) < 0.08f &&
            Quaternion.Angle(body.rotation, targetRotation) < 12f)
        {
            grabState = GrabState.Holding;
        }
    }

    // Этот метод вызывается каждый раз, когда нажимается кнопка Interact
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    // Логика рейкаста остается точно такой же, как и была!
    private void TryInteract()
    {
        if (carriedItem != null)
        {
            PlaceCurrent();
            return;
        }

        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;
        Vector3 direction = rayOrigin != null ? rayOrigin.forward : transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactDistance, interactLayer))
        {
            Grabable grabable = hit.collider.GetComponentInParent<Grabable>();
            if (grabable != null && !grabable.IsCarried)
            {
                StartGrab(grabable);
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }

    private void StartGrab(Grabable item)
    {
        if (item.Body == null)
            return;

        carriedItem = item;
        grabSide = ChooseGrabSide(item);
        grabState = GrabState.Docking;
        smoothedMovementOffset = Vector3.zero;

        Rigidbody body = item.Body;
        body.isKinematic = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        item.BeginCarry();
        item.IgnoreCollisionsWith(playerColliders, true);
    }

    private void ThrowCurrent()
    {
        float throwSpeed = baseThrowSpeed / (1f + carriedItem.Weight * throwWeightSlowdown);
        Release(GetPlayerVelocity() * playerVelocityTransfer + GetRayDirection() * throwSpeed, true);
    }

    private void PlaceCurrent()
    {
        Release(GetPlayerVelocity() * placeVelocityTransfer, false);
    }

    private void Release(Vector3 velocity, bool thrown)
    {
        if (carriedItem == null)
            return;

        Grabable item = carriedItem;
        Rigidbody body = item.Body;
        item.IgnoreCollisionsWith(playerColliders, false);
        item.EndCarry();

        if (body != null)
        {
            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.angularVelocity = thrown
                ? Random.insideUnitSphere * throwSpin
                : Vector3.zero;

            if (!thrown)
                item.EnablePlaceBounce();
        }

        carriedItem = null;
        grabSide = null;
        grabState = GrabState.None;
    }

    private Vector3 GetHoldTargetPosition()
    {
        Vector3 velocity = GetPlayerVelocity();
        velocity.y = 0f;
        Vector3 desiredOffset = velocity.sqrMagnitude > 0.001f
            ? -velocity.normalized * movementOffsetDistance * Mathf.Clamp01(velocity.magnitude / maxSpeedForOffset)
            : Vector3.zero;

        smoothedMovementOffset = Vector3.Lerp(smoothedMovementOffset, desiredOffset, 8f * Time.fixedDeltaTime);
        return GetRayOrigin() + GetRayDirection() * holdDistance + smoothedMovementOffset;
    }

    private Quaternion GetHoldTargetRotation()
    {
        Vector3 directionToPlayer = GetRayOrigin() - carriedItem.Body.position;
        directionToPlayer.y = 0f;
        if (directionToPlayer.sqrMagnitude < 0.001f)
        {
            directionToPlayer = GetRayDirection();
            directionToPlayer.y = 0f;
        }

        Quaternion sideRotation = Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up);
        return grabSide == null || grabSide == carriedItem.transform
            ? sideRotation
            : sideRotation * Quaternion.Inverse(grabSide.localRotation);
    }

    private Transform ChooseGrabSide(Grabable item)
    {
        Transform[] sides = item.GrabSides;
        if (sides == null || sides.Length == 0)
            return item.transform;

        Vector3 toPlayer = (GetRayOrigin() - item.transform.position).normalized;
        Transform best = null;
        float bestScore = float.NegativeInfinity;
        foreach (Transform side in sides)
        {
            if (side == null)
                continue;

            float score = Vector3.Dot(side.forward, toPlayer);
            if (score > bestScore)
            {
                bestScore = score;
                best = side;
            }
        }

        return best == null ? item.transform : best;
    }

    private Vector3 GetPlayerVelocity()
    {
        return playerBody == null ? Vector3.zero : playerBody.linearVelocity;
    }

    private Vector3 GetRayOrigin()
    {
        return rayOrigin != null ? rayOrigin.position : transform.position;
    }

    private Vector3 GetRayDirection()
    {
        return rayOrigin != null ? rayOrigin.forward : transform.forward;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = GetRayOrigin();
        Vector3 direction = GetRayDirection();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, direction * interactDistance);
    }
}
