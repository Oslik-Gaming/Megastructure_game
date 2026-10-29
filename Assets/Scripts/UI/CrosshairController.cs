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

        // ВАЖНО: корень HUD-документа занимает весь экран и по умолчанию
        // перехватывает ВСЕ pointer-события — из-за этого не работали ховер и
        // клики в меню паузы. Делаем корень и прицел прозрачными для ввода.
        hudDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
        if (_crosshair != null)
            _crosshair.pickingMode = PickingMode.Ignore;
    }

    private void Update()
    {
        // На паузе прицел не нужен — меню его перекрывает,
        // но явно прячем для надёжности.
        if (PauseMenuController.GamePaused)
        {
            if (_crosshair != null && _crosshair.style.display != DisplayStyle.None)
                _crosshair.style.display = DisplayStyle.None;
            return;
        }

        // возвращаем прицел, если он был спрятан паузой
        if (_crosshair != null && _crosshair.style.display == DisplayStyle.None)
            _crosshair.style.display = DisplayStyle.Flex;

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