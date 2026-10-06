using UnityEngine;
using UnityEngine.InputSystem; // Обязательно добавь этот using для New Input System!

public class PlayerRaycastInteract : MonoBehaviour
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private Transform rayOrigin;

    // Ссылка на наш сгенерированный класс ввода
    private PlayerInputActions inputActions;

    private void Awake()
    {
        // Создаем экземпляр сгенерированного класса
        inputActions = new PlayerInputActions();
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
    }

    // Этот метод вызывается каждый раз, когда нажимается кнопка Interact
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    // Логика рейкаста остается точно такой же, как и была!
    private void TryInteract()
    {
        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;
        Vector3 direction = rayOrigin != null ? rayOrigin.forward : transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = rayOrigin != null ? rayOrigin.position : transform.position;
        Vector3 direction = rayOrigin != null ? rayOrigin.forward : transform.forward;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, direction * interactDistance);
    }
}