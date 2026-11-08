using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// RetroBrightnessDim — затемнение экрана штатным параметром "Image Brightness"
/// Retro-пака (полноэкранный CRT-шейдер из Assets/Retro).
///
/// При зажатом Q (подсветка мусора) яркость плавно опускается до
/// DimBrightness × обычной; при отпускании — плавно возвращается.
///
/// Материал Retro ищется автоматически среди FullScreenPassRendererFeature
/// во всех рендерерах URP-ассета (BlitToMain). Если эффект ещё не добавлен
/// в активный рендерер — в консоли будет подсказка.
/// </summary>
[DisallowMultipleComponent]
public sealed class RetroBrightnessDim : MonoBehaviour
{
    private const string BrightnessProperty = "Vector1_5660967D"; // "Image Brightness" в Retro.shadergraph

    [Header("Затемнение")]
    [Tooltip("Множитель яркости Image Brightness при зажатом Q.\n0.35 = экран темнеет до 35% обычной яркости.")]
    [SerializeField, Range(0.05f, 1f)] private float dimBrightness = 0.35f;

    [Header("Скорость")]
    [SerializeField, Min(0.01f)] private float fadeIn = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeOut = 0.25f;

    private Material retroMaterial;
    private float normalBrightness = 1f;
    private float current;          // 0..1 — сила затемнения
    private bool targetOn;
    private bool initialized;

    /// <summary>Запрошено ли затемнение прямо сейчас.</summary>
    public bool IsDimmed => targetOn;

    // =====================================================================
    //  LIFECYCLE
    // =====================================================================

    private void OnEnable() => StartCoroutine(InitWhenReady());

    private void OnDisable()
    {
        StopAllCoroutines();
        // Восстановить яркость мгновенно, чтобы эффект не «залип»
        if (retroMaterial != null) retroMaterial.SetFloat(BrightnessProperty, normalBrightness);
        targetOn = false;
        current = 0f;
    }

    private void Update()
    {
        if (!initialized || retroMaterial == null) return;

        // Плавный фейд на unscaled time: корректно гаснет даже на паузе
        float target = targetOn ? 1f : 0f;
        float speed = (targetOn ? 1f / fadeIn : 1f / fadeOut) * Time.unscaledDeltaTime;
        current = Mathf.MoveTowards(current, target, speed);

        float brightness = Mathf.Lerp(normalBrightness, normalBrightness * dimBrightness, current);
        retroMaterial.SetFloat(BrightnessProperty, brightness);
    }

    // =====================================================================
    //  ПУБЛИЧНОЕ API
    // =====================================================================

    /// <summary>Включить/выключить затемнение (плавный фейд).</summary>
    public void SetDimmed(bool on)
    {
        targetOn = on;
    }

    // =====================================================================
    //  ПОИСК МАТЕРИАЛА RETRO
    // =====================================================================

    private IEnumerator InitWhenReady()
    {
        // URP-ассет и рендереры становятся доступны не мгновенно после загрузки сцены.
        // Поиск через reflection недешёвый — ретраим не каждый кадр, а раз в ~0.5с.
        var wait = new WaitForSecondsRealtime(0.5f);
        while (retroMaterial == null)
        {
            retroMaterial = FindRetroPassMaterial();
            if (retroMaterial != null) break;
            yield return wait;
        }

        normalBrightness = retroMaterial.GetFloat(BrightnessProperty);
        initialized = true;
    }

    /// <summary>
    /// Ищет материал Retro среди всех FullScreenPassRendererFeature
    /// всех рендереров активного URP-ассета.
    /// API URP между версиями различается, поэтому всё через reflection
    /// с несколькими запасными путями.
    /// </summary>
    private static Material FindRetroPassMaterial()
    {
        var asset = UniversalRenderPipeline.asset;
        if (asset == null) return null;

        foreach (var renderer in GetAllRenderers(asset))
        {
            if (renderer == null) continue;
            foreach (var feature in GetFeatures(renderer))
            {
                if (feature is FullScreenPassRendererFeature fullscreen && fullscreen.isActive)
                {
                    var mat = fullscreen.passMaterial;
                    if (mat != null && mat.HasProperty(BrightnessProperty))
                        return mat;
                }
            }
        }
        return null;
    }

    private const System.Reflection.BindingFlags Flags =
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;

    /// <summary>Все рендереры URP-ассета (тремя запасными путями).</summary>
    private static IEnumerable<ScriptableRenderer> GetAllRenderers(UniversalRenderPipelineAsset asset)
    {
        var seen = new HashSet<ScriptableRenderer>();

        // Вариант 1: публичные rendererCount/GetRenderer (старые версии URP)
        var countProp = asset.GetType().GetProperty("rendererCount", Flags);
        var getRenderer = asset.GetType().GetMethod("GetRenderer", Flags);
        if (countProp != null && getRenderer != null)
        {
            int count;
            try { count = (int)countProp.GetValue(asset); }
            catch { count = 0; }
            for (int i = 0; i < count; i++)
            {
                ScriptableRenderer r;
                try { r = getRenderer.Invoke(asset, new object[] { i }) as ScriptableRenderer; }
                catch { r = null; }
                if (r != null && seen.Add(r)) yield return r;
            }
        }

        // Вариант 2: приватное поле m_RendererDataList → data.scriptableRenderer (URP 17+)
        var listField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", Flags);
        if (listField != null && listField.GetValue(asset) is ScriptableRendererData[] datas)
        {
            var rendererProp = typeof(ScriptableRendererData).GetProperty("scriptableRenderer", Flags);
            foreach (var data in datas)
            {
                if (data == null) continue;
                var r = rendererProp != null ? rendererProp.GetValue(data) as ScriptableRenderer : null;
                if (r != null && seen.Add(r)) yield return r;
            }
        }

        // Вариант 3: дефолтный рендерер ассета
        var defaultProp = asset.GetType().GetProperty("scriptableRenderer", Flags);
        if (defaultProp != null && defaultProp.GetValue(asset) is ScriptableRenderer def && seen.Add(def))
            yield return def;
    }

    /// <summary>
    /// Список фич рендерера. Достаём через reflection: в разных версиях URP
    /// доступ к списку фич отличается, а нам нужна только совместимость.
    /// </summary>
    private static IEnumerable<ScriptableRendererFeature> GetFeatures(ScriptableRenderer renderer)
    {
        var property = typeof(ScriptableRenderer).GetProperty("features",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (property == null) yield break;

        if (property.GetValue(renderer) is IEnumerable<ScriptableRendererFeature> list)
            foreach (var feature in list)
                if (feature != null) yield return feature;
    }
}
