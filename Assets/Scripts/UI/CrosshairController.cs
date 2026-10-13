using UnityEngine;
using UnityEngine.UIElements;

public class CrosshairController : MonoBehaviour
{
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private float rayDistance = 4f;

    private VisualElement _crosshair;
    private bool _highlighted;

    private void Awake()
    {
        _crosshair = hudDocument.rootVisualElement.Q("crosshair");
    }

    private void Update()
    {
        bool looking = IsLookingAtInteractable();

        if (looking != _highlighted)
        {
            _highlighted = looking;
            // включаем/выключаем класс — транзишен в USS делает плавно
            _crosshair.EnableInClassList("highlight", _highlighted);
        }
    }

    private bool IsLookingAtInteractable()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // центр экрана
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            return hit.collider.TryGetComponent<IInteractable>(out _);
        }
        return false;
    }

    // Вызывай при открытии паузы/меню, чтобы скрыть прицел
    public void SetVisible(bool visible)
    {
        _crosshair.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}