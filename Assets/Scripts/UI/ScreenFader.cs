using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Затемнение экрана (плавный фейд в чёрный и обратно).
/// Сам создаёт свой Canvas поверх всего UI при старте — достаточно добавить
/// компонент на ПУСТОЙ объект сцены (на объект с UIDocument добавлять нельзя).
///
/// Все параметры (длительности, цвет, порядок отрисовки) настраиваются в инспекторе.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    [Header("Длительности (секунды)")]
    [Tooltip("Время затемнения экрана в чёрный")]
    [Min(0f)] public float fadeOutDuration = 0.3f;

    [Tooltip("Время проявления из чёрного")]
    [Min(0f)] public float fadeInDuration = 0.3f;

    [Header("Вид")]
    [Tooltip("Цвет затемнения")]
    public Color fadeColor = Color.black;

    [Tooltip("Порядок отрисовки: больше = выше остальных UI (должен перекрывать меню)")]
    [SerializeField] private int sortingOrder = 1000;

    private Image overlay;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        EnsureOverlay();
        SetAlpha(0f);
    }

    /// <summary>Затемнить экран. duration < 0 — взять fadeOutDuration из инспектора.</summary>
    public Coroutine FadeOut(float duration = -1f) => StartFade(1f, duration);

    /// <summary>Проявить экран. duration < 0 — взять fadeInDuration из инспектора.</summary>
    public Coroutine FadeIn(float duration = -1f) => StartFade(0f, duration);

    /// <summary>Мгновенно установить прозрачность (0 = прозрачно, 1 = полностью затемнено).</summary>
    public void SetAlphaImmediate(float alpha) => SetAlpha(Mathf.Clamp01(alpha));

    private Coroutine StartFade(float targetAlpha, float duration)
    {
        if (duration < 0f)
            duration = targetAlpha > 0.5f ? fadeOutDuration : fadeInDuration;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
        return fadeRoutine;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            fadeRoutine = null;
            yield break;
        }

        float startAlpha = overlay.color.a;
        float progress = 0f;

        // unscaledDeltaTime: затемнение работает даже при Time.timeScale = 0 (пауза)
        while (progress < 1f)
        {
            progress += Time.unscaledDeltaTime / duration;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(progress)));
            yield return null;
        }

        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (overlay == null) return;

        Color color = fadeColor;
        color.a = alpha;
        overlay.color = color;
    }

    private void EnsureOverlay()
    {
        if (overlay != null) return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        overlay = GetComponent<Image>();
        if (overlay == null)
            overlay = gameObject.AddComponent<Image>(); // CanvasRenderer добавится автоматически

        overlay.raycastTarget = false; // не блокируем клики по остальному UI
        overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        // Растягиваем на весь экран
        RectTransform rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
