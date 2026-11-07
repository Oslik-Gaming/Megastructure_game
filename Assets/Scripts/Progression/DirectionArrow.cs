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
        [SerializeField, Tooltip("Точка отсчёта для расчёта направления стрелки. Пусто — используется Camera.main.")]
        private Transform referenceTransform;

        private VisualElement arrowElement;
        private Transform target;
        private Transform baseTarget;
        private Transform overrideTarget;
        private bool isVisible = false;

        private Transform Reference => referenceTransform != null ? referenceTransform : Camera.main != null ? Camera.main.transform : null;

        private void Start()
        {
            if (Reference == null)
            {
                Debug.LogWarning($"{name}: не задана точка отсчёта (Reference Transform) и не найдена Camera.main. Стрелка направления работать не будет.");
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
            if (isVisible && target != null && Reference != null && arrowElement != null)
            {
                UpdateArrowDirection();
            }
        }

        public void Show(Transform newTarget)
        {
            baseTarget = newTarget;
            if (overrideTarget == null)
                SetTarget(newTarget);
        }

        public void ShowOverride(Transform newTarget)
        {
            overrideTarget = newTarget;
            SetTarget(newTarget);
        }

        public void HideOverride()
        {
            overrideTarget = null;
            SetTarget(baseTarget);
        }

        private void SetTarget(Transform newTarget)
        {
            target = newTarget;
            isVisible = newTarget != null;

            if (arrowElement != null)
            {
                arrowElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void Hide()
        {
            baseTarget = null;
            if (overrideTarget == null) SetTarget(null);
        }

        private void UpdateArrowDirection()
        {
            if (target == null || Reference == null) return;

            // Вычислить направление к цели
            Vector3 directionToTarget = (target.position - Reference.position).normalized;
            
            // Проецировать на плоскость отсчёта
            Vector3 forward = Reference.forward;
            Vector3 right = Reference.right;
            
            float x = Vector3.Dot(directionToTarget, right);
            float y = Vector3.Dot(directionToTarget, forward);
            
            // Вычислить угол поворота стрелки (в градусах)
            float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;
            
            // Применить поворот через CSS transform
            arrowElement.style.rotate = new Rotate(angle);
        }
    }
}
