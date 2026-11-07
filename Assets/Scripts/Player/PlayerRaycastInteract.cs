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

    [Header("Анимация")]
    [Tooltip("Аниматор робота (триггеры Grab/Release). Если пуст — найдётся в детях.")]
    [SerializeField] private Animator animator;

    [Tooltip("Анимация поднятия предмета (Grab/Release) robot_done. Сейчас отключена — включите галочкой, если понадобится снова.")]
    [SerializeField] private bool grabAnimationEnabled = false;

    private static readonly int GrabTrigger = Animator.StringToHash("Grab");
    private static readonly int ReleaseTrigger = Animator.StringToHash("Release");

    // Ссылка на наш сгенерированный класс ввода
    private PlayerInputActions inputActions;
    private GrabState grabState;
    private Grabable carriedItem;
    private Transform grabSide;
    private Vector3 smoothedMovementOffset;
    private Rigidbody playerBody;
    private Collider[] playerColliders;

    // Данные удерживаемого предмета для проверки стен (чтобы предмет не проходил сквозь геометрию)
    private Collider[] heldColliders;
    private float heldRadius;
    private readonly RaycastHit[] holdHitBuffer = new RaycastHit[16];
    private const float HoldObstacleSkin = 0.05f;

    private void Awake()
    {
        // Создаем экземпляр сгенерированного класса
        inputActions = new PlayerInputActions();
        playerBody = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
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

    private void FixedUpdate()
    {
        if (carriedItem == null || carriedItem.Body == null)
            return;

        Rigidbody body = carriedItem.Body;
        float weightFactor = 1f + carriedItem.Weight * weightSlowdown;
        float positionSpeed = (grabState == GrabState.Docking ? dockPositionSpeed : holdPositionSpeed) / weightFactor;
        float turnSpeed = rotationSpeed / weightFactor;
        Vector3 targetPosition = ClampHoldTarget(body.position, GetHoldTargetPosition());
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

    private void TryInteract()
    {
        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;
        Vector3 direction = rayOrigin != null ? rayOrigin.forward : transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactDistance, interactLayer))
        {
            Grabable grabable = hit.collider.GetComponentInParent<Grabable>();
            if (grabable != null)
                return;

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }

    /// <summary>
    /// ЛКМ гравизахвата: захватывает объект или устанавливает удерживаемый объект.
    /// </summary>
    public void TryGrabOrPlace()
    {
        if (carriedItem != null)
        {
            PlaceCurrent();
            return;
        }

        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;
        Vector3 direction = rayOrigin != null ? rayOrigin.forward : transform.forward;

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, interactDistance, interactLayer))
            return;

        Grabable grabable = hit.collider.GetComponentInParent<Grabable>();
        if (grabable != null && !grabable.IsCarried)
            StartGrab(grabable);
    }

    /// <summary>Бросает удерживаемый объект.</summary>
    public void ThrowHeldObject()
    {
        if (carriedItem != null)
            ThrowCurrent();
    }

    /// <summary>Устанавливает удерживаемый объект.</summary>
    public void PlaceHeldObject()
    {
        if (carriedItem != null)
            PlaceCurrent();
    }

    public bool HasCarriedItem => carriedItem != null;

    private void StartGrab(Grabable item)
    {
        if (item.Body == null)
            return;

        carriedItem = item;
        grabSide = ChooseGrabSide(item);
        grabState = GrabState.Docking;
        smoothedMovementOffset = Vector3.zero;

        // Запоминаем коллайдеры и радиус предмета для проверки стен при переносе
        heldColliders = item.GetComponentsInChildren<Collider>();
        heldRadius = ComputeBoundingRadius(heldColliders);

        Rigidbody body = item.Body;
        body.isKinematic = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        item.BeginCarry();
        item.IgnoreCollisionsWith(playerColliders, true);

        // Вытягиваем руку (телекинез)
        if (animator != null && grabAnimationEnabled) animator.SetTrigger(GrabTrigger);
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
        heldColliders = null;
        heldRadius = 0f;

        // Убираем руку (телекинез завершается)
        if (animator != null && grabAnimationEnabled) animator.SetTrigger(ReleaseTrigger);
    }

    /// <summary>
    /// Отпускает удерживаемый объект без броска — он просто падает.
    /// Вызывается при переключении инструмента с гравизахвата.
    /// </summary>
    public void DropHeldObject()
    {
        if (carriedItem != null)
            PlaceCurrent();
    }

    /// <summary>
    /// Ограничивает целевую точку удержания, чтобы предмет не проходил сквозь стены/пол:
    /// пускаем луч от текущей позиции предмета к цели и останавливаем предмет перед препятствием.
    /// </summary>
    private Vector3 ClampHoldTarget(Vector3 from, Vector3 target)
    {
        if (heldColliders == null || heldColliders.Length == 0)
            return target;

        Vector3 delta = target - from;
        float distance = delta.magnitude;
        if (distance < 0.01f)
            return target;

        Vector3 direction = delta / distance;
        float clearance = heldRadius + HoldObstacleSkin;

        int hitCount = Physics.RaycastNonAlloc(from, direction, holdHitBuffer, distance + clearance,
            ~0, QueryTriggerInteraction.Ignore);

        float nearestBlocking = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = holdHitBuffer[i].collider;

            // Свои коллайдеры и коллайдеры игрока препятствиями не считаем
            if (IsOwnOrPlayerCollider(hitCollider))
                continue;

            if (holdHitBuffer[i].distance < nearestBlocking)
                nearestBlocking = holdHitBuffer[i].distance;
        }

        // Препятствие дальше, чем точка удержания с запасом — ничего не режем
        if (nearestBlocking >= distance + clearance)
            return target;

        float allowedDistance = Mathf.Clamp(nearestBlocking - clearance, 0f, distance);
        return from + direction * allowedDistance;
    }

    private bool IsOwnOrPlayerCollider(Collider collider)
    {
        if (collider == null)
            return true;

        foreach (Collider held in heldColliders)
            if (held == collider)
                return true;

        if (playerColliders != null)
        {
            foreach (Collider playerCollider in playerColliders)
                if (playerCollider == collider)
                    return true;
        }

        return false;
    }

    private float ComputeBoundingRadius(Collider[] colliders)
    {
        Bounds bounds = default;
        bool hasBounds = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds ? bounds.extents.magnitude : 0.5f;
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
