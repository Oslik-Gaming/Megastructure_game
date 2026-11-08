using UnityEngine;
using UnityEngine.UIElements;

namespace Progression
{
    /// <summary>
    /// Компонент стрелки направления в HUD.
    /// Стрелка внизу по центру экрана, указывает на цель в мире.
    /// Рисуется процедурной текстурой: объёмная заливка, тёмная обводка и
    /// отдельная тень-силуэт со сдвигом — псевдо-3D вид. Дистанция до цели
    /// выводится увеличенным текстом справа от стрелки.
    /// Элемент direction-arrow берётся из GameHud.uxml; если его нет — строится кодом.
    /// </summary>
    public class DirectionArrow : MonoBehaviour
    {
        [SerializeField] private UIDocument hudDocument;
        [SerializeField, Tooltip("Точка отсчёта для расчёта направления стрелки. Пусто — используется Camera.main.")]
        private Transform referenceTransform;
        [SerializeField, Tooltip("Показывать дистанцию до цели рядом со стрелкой.")]
        private bool showDistance = true;

        private VisualElement arrowElement;
        private VisualElement arrowBody;
        private Label distanceLabel;
        private Transform target;
        private Transform baseTarget;
        private Transform overrideTarget;
        private bool isVisible = false;

        private const float ArrowSize = 58f;   // размер элемента стрелки на экране
        private const int TextureSize = 256;   // разрешение процедурной текстуры

        private static Texture2D arrowTexture;

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
                // Fallback: собрать такую же стрелку кодом (для сцен без GameHud.uxml).
                arrowElement = BuildArrowElement();
                root.Add(arrowElement);
            }
            else
            {
                arrowBody = arrowElement.Q("direction-arrow-body");
                distanceLabel = arrowElement.Q<Label>("direction-distance");
            }

            ApplyArrowTextures();
            ApplyDistanceVisibility();

            // Восстанавливаем актуальное состояние видимости: Show() мог быть
            // вызван до инициализации (порядок Start() не гарантирован),
            // поэтому нельзя безусловно скрывать элемент.
            arrowElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Назначить процедурную текстуру корпусу стрелки.</summary>
        private void ApplyArrowTextures()
        {
            if (arrowBody == null)
                return;

            arrowBody.style.backgroundImage = new StyleBackground(GetArrowTexture());
        }

        private void ApplyDistanceVisibility()
        {
            if (distanceLabel != null)
                distanceLabel.style.display = showDistance ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Собрать стрелку кодом: тень, корпус, дистанция (для сцен без GameHud.uxml).</summary>
        private VisualElement BuildArrowElement()
        {
            var container = new VisualElement();
            container.name = "direction-arrow";
            container.AddToClassList("direction-arrow");
            container.style.position = Position.Absolute;
            container.style.left = Length.Percent(50f);
            container.style.bottom = 40f;
            container.style.width = ArrowSize;
            container.style.height = ArrowSize;
            container.style.translate = new Translate(new Length(-50f, LengthUnit.Percent), 0f);
            container.style.display = DisplayStyle.None;

            var body = new VisualElement();
            body.name = "direction-arrow-body";
            body.AddToClassList("arrow-body");
            body.style.position = Position.Absolute;
            body.style.left = 0f;
            body.style.top = 0f;
            body.style.width = ArrowSize;
            body.style.height = ArrowSize;
            container.Add(body);
            arrowBody = body;

            var label = new Label("0 M");
            label.name = "direction-distance";
            label.AddToClassList("direction-distance");
            label.style.position = Position.Absolute;
            label.style.left = ArrowSize + 12f;
            label.style.top = Length.Percent(50f);
            label.style.translate = new Translate(0f, new Length(-50f, LengthUnit.Percent));
            label.style.fontSize = 19f;
            label.style.color = new Color(0f, 1f, 0.43f);
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.backgroundColor = new Color(0f, 0.07f, 0.04f, 0.72f);
            container.Add(label);
            distanceLabel = label;

            return container;
        }

        private static Texture2D GetArrowTexture()
        {
            if (arrowTexture == null)
                arrowTexture = CreateArrowTexture();
            return arrowTexture;
        }

        // Контур стрелки (наконечник + стержень), указывающей вверх, в пикселях текстуры.
        private static readonly Vector2[] ArrowOutline =
        {
            new Vector2(0.500f, 0.020f), // остриё
            new Vector2(0.830f, 0.430f), // правый нижний угол наконечника
            new Vector2(0.640f, 0.430f), // правый верх стержня
            new Vector2(0.640f, 0.940f), // правый низ стержня
            new Vector2(0.360f, 0.940f), // левый низ стержня
            new Vector2(0.360f, 0.430f), // левый верх стержня
            new Vector2(0.170f, 0.430f), // левый нижний угол наконечника
        };

        private static readonly Color FaceColor = new Color(0f, 1f, 0.43f, 1f);   // как остальные элементы HUD
        private static readonly Color OutlineColor = new Color(0f, 0.09f, 0.04f, 1f);

        /// <summary>Нарисовать стрелку в текстуру: однотонная заливка с тёмной обводкой.</summary>
        private static Texture2D CreateArrowTexture()
        {
            int size = TextureSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DirectionArrow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[size * size];
            float outlinePx = size * 0.02f; // толщина обводки

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // UV в системе текстуры: Y снизу вверх, как у контура.
                    float u = (x + 0.5f) / size;
                    float v = 1f - (y + 0.5f) / size;
                    var p = new Vector2(u, v);

                    Color32 c;
                    if (IsInsideArrow(p))
                        c = FaceColor;
                    else if (IsNearArrow(p, outlinePx))
                        c = OutlineColor; // тёмная обводка вокруг стрелки
                    else
                        c = new Color32(0, 0, 0, 0);

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static bool IsInsideArrow(Vector2 p)
        {
            // Классический тест точки в полигоне (ray casting).
            bool inside = false;
            int n = ArrowOutline.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var a = ArrowOutline[i];
                var b = ArrowOutline[j];
                if ((a.y > p.y) != (b.y > p.y) &&
                    p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static bool IsNearArrow(Vector2 p, float radius)
        {
            // Есть ли граница полигона в пределах radius от точки (грубая проверка
            // соседей по квадратной маске) — используется для обводки.
            int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * Mathf.PI * 2f / steps;
                var sample = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (!IsInsideArrow(sample))
                    continue;
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (arrowElement == null)
                return;

            // Синхронизируем видимость каждый кадр — это делает показ стрелки
            // независимым от порядка вызова Start() у DirectionArrow и
            // GameProgressionManager (Show() мог сработать до инициализации).
            bool shouldBeVisible = isVisible && target != null && Reference != null;
            arrowElement.style.display = shouldBeVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (shouldBeVisible)
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

            // Угол поворота: 0 — цель прямо впереди (стрелка вверх),
            // положительный — цель справа.
            float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;

            var rotation = new Rotate(angle);
            if (arrowBody != null)
                arrowBody.style.rotate = rotation;

            if (distanceLabel != null && showDistance)
            {
                float distance = Vector3.Distance(target.position, Reference.position);
                distanceLabel.text = $"{distance:0} M";
            }
        }
    }
}
