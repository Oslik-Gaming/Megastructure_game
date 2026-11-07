using UnityEngine;
using UnityEngine.UIElements;

namespace Progression
{
    /// <summary>
    /// Компонент стрелки направления в HUD.
    /// Указывает на цель в мировом пространстве.
    /// </summary>
    public class DirectionArrow : MonoBehaviour
    {
        [SerializeField] private UIDocument hudDocument;
        [SerializeField] private Camera playerCamera;

        private VisualElement arrowElement;
        private Transform target;
        private bool isVisible = false;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (hudDocument != null)
            {
                InitializeArrow();
            }
        }

        private void InitializeArrow()
        {
            var root = hudDocument.rootVisualElement;
            arrowElement = root.Q("direction-arrow");

            if (arrowElement == null)
            {
                // Создать стрелку программно
                arrowElement = new VisualElement();
                arrowElement.name = "direction-arrow";
                arrowElement.AddToClassList("direction-arrow");
                arrowElement.style.position = Position.Absolute;
                arrowElement.style.right = 20;
                arrowElement.style.bottom = 80;
                arrowElement.style.width = 40;
                arrowElement.style.height = 40;
                arrowElement.style.display = DisplayStyle.None;
                
                // Простой треугольник через CSS
                arrowElement.style.borderLeftWidth = 20;
                arrowElement.style.borderRightWidth = 20;
                arrowElement.style.borderBottomWidth = 30;
                arrowElement.style.borderLeftColor = Color.clear;
                arrowElement.style.borderRightColor = Color.clear;
                arrowElement.style.borderBottomColor = new Color(0.22f, 0.74f, 0.97f, 1f); // #38BDF8

                root.Add(arrowElement);
            }

            arrowElement.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (isVisible && target != null && playerCamera != null && arrowElement != null)
            {
                UpdateArrowDirection();
            }
        }

        public void Show(Transform newTarget)
        {
            target = newTarget;
            isVisible = true;

            if (arrowElement != null)
            {
                arrowElement.style.display = DisplayStyle.Flex;
            }
        }

        public void Hide()
        {
            isVisible = false;
            target = null;

            if (arrowElement != null)
            {
                arrowElement.style.display = DisplayStyle.None;
            }
        }

        private void UpdateArrowDirection()
        {
            if (target == null || playerCamera == null) return;

            // Вычислить направление к цели
            Vector3 directionToTarget = (target.position - playerCamera.transform.position).normalized;
            
            // Проецировать на плоскость камеры
            Vector3 forward = playerCamera.transform.forward;
            Vector3 right = playerCamera.transform.right;
            
            float x = Vector3.Dot(directionToTarget, right);
            float y = Vector3.Dot(directionToTarget, forward);
            
            // Вычислить угол поворота стрелки (в градусах)
            float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;
            
            // Применить поворот через CSS transform
            arrowElement.style.rotate = new Rotate(angle);
        }
    }
}
