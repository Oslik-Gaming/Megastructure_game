using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Постоянные глитч-полосы по левому и правому краю экрана.
/// Вешать на объект с UIDocument.
/// </summary>
public class EdgeGlitchEffect : MonoBehaviour
{
    [Header("Плотность")]
    [SerializeField] private int bandsPerSide = 9;
    [SerializeField, Range(0f, 1f)] private float activity = 0.85f;
    [SerializeField, Min(0.01f)] private float tickInterval = 0.04f;

    [Header("Размеры полос, px (дизайн 1920x1080)")]
    [SerializeField] private float minWidth = 20f;
    [SerializeField] private float maxWidth = 160f;
    [SerializeField, Range(0f, 1f)] private float longStreakChance = 0.10f;
    [SerializeField] private float maxStreakWidth = 500f;
    [SerializeField] private float minHeight = 2f;
    [SerializeField] private float maxHeight = 18f;

    private VisualElement host;
    private VisualElement[] bands;

    private const float FIELD_HEIGHT = 1080f; // MainContainer фиксированный 1920x1080

    private static readonly Color[] Palette =
    {
        new Color(0.00f, 1.00f, 0.25f),  // основной зелёный
        new Color(0.35f, 1.00f, 0.55f),  // светлый
        new Color(0.85f, 1.00f, 0.90f),  // яркая вспышка
        new Color(0.00f, 0.60f, 0.15f),
        new Color(0.00f, 0.20f, 0.05f),  // тёмный провал
    };

    void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogWarning("[EdgeGlitchEffect] На этом объекте нет UIDocument! Повесь скрипт туда же, где UIDocument.", this);
            return;
        }
        StartCoroutine(InitWhenReady(doc));
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (host != null) { host.RemoveFromHierarchy(); host = null; bands = null; }
    }

    private IEnumerator InitWhenReady(UIDocument doc)
    {
        // UIDocument может включиться позже — ждём корень, а не выходим молча
        while (doc.rootVisualElement == null)
            yield return null;

        var main = doc.rootVisualElement.Q("MainContainer") ?? doc.rootVisualElement;

        host = new VisualElement { name = "EdgeGlitchHost" };
        host.pickingMode = PickingMode.Ignore;
        host.style.position = Position.Absolute;
        host.style.left = 0;
        host.style.top = 0;
        host.style.width = Length.Percent(100f);
        host.style.height = Length.Percent(100f);
        main.Add(host); // последним ребёнком = поверх всего, точно видно

        bands = new VisualElement[bandsPerSide * 2];
        for (int i = 0; i < bands.Length; i++)
        {
            var band = new VisualElement();
            band.pickingMode = PickingMode.Ignore;
            band.style.position = Position.Absolute;
            band.style.opacity = 0f;
            if (i < bandsPerSide) band.style.left = 0;  // левый край
            else band.style.right = 0;                  // правый край
            host.Add(band);
            bands[i] = band;
        }

        Debug.Log($"[EdgeGlitchEffect] Готово, {bands.Length} полос создано. Если полос не видно — увеличь activity.", this);
        StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                var b = bands[i];
                if (Random.value > activity) { b.style.opacity = 0f; continue; }

                b.style.top = Random.Range(0f, FIELD_HEIGHT - maxHeight);
                b.style.height = Random.Range(minHeight, maxHeight);
                b.style.width = Random.value < longStreakChance
                    ? Random.Range(maxWidth, maxStreakWidth)  // редкий выброс внутрь
                    : Random.Range(minWidth, maxWidth);       // обычная полоса у края

                var c = Palette[Random.Range(0, Palette.Length)];
                c.a = Random.Range(0.35f, 0.95f);
                b.style.backgroundColor = c;
                b.style.opacity = 1f;
            }
            yield return wait;
        }
    }

    [ContextMenu("Тест: вспышка один раз")]
    private void TestFlash()
    {
        if (bands == null) { Debug.LogWarning("[EdgeGlitchEffect] Ещё не инициализирован, нажми в Play mode.", this); return; }
        foreach (var b in bands)
        {
            b.style.top = Random.Range(0f, FIELD_HEIGHT - maxHeight);
            b.style.height = Random.Range(minHeight, maxHeight);
            b.style.width = Random.Range(minWidth, maxStreakWidth);
            var c = Palette[Random.Range(0, Palette.Length)];
            c.a = 0.9f;
            b.style.backgroundColor = c;
            b.style.opacity = 1f;
        }
    }
}