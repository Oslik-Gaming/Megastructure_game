using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет свойствами материала тумана (AERO Volumetric Fog).
/// На сцене должен быть один такой объект. FogZone'ы сообщают ему,
/// какие параметры задать, а он плавно интерполирует материал.
/// </summary>
public class FogZoneManager : MonoBehaviour
{
    public static FogZoneManager Instance { get; private set; }

    [Header("Материал тумана (fog_mat)")]
    [SerializeField] private Material fogMaterial;

    [Header("Дефолтные значения (используются, когда игрок вне всех зон)")]
    [SerializeField] private float defaultDensity = 0.018f;
    [SerializeField] private Color defaultColour = new Color(0.2754f, 0.2385f, 0.2385f, 1f);
    [SerializeField] private float defaultMaxDistance = 150f;

    [Header("Время возврата к дефолту, сек")]
    [SerializeField, Min(0.01f)] private float defaultTransitionTime = 2f;

    // --- текущее состояние (то, что реально в материале) ---
    private float currentDensity;
    private Color currentColour;
    private float currentMaxDistance;

    // --- цель, к которой движемся ---
    private float targetDensity;
    private Color targetColour;
    private float targetMaxDistance;
    private float transitionSpeed = 1f; // за сколько секунд проходит переход

    // Исходные значения материала (для отката в редакторе после Play-режима)
    private float startDensity;
    private Color startColour;
    private float startMaxDistance;
    private bool initialValuesCaptured;
    private bool materialWarningShown;

    // стек активных зон (игрок может стоять в нескольких)
    private readonly List<FogZone> activeZones = new List<FogZone>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // читаем фактические значения из материала, чтобы стартовать без скачка
        if (fogMaterial != null && fogMaterial.HasProperty("_Density"))
        {
            currentDensity = fogMaterial.GetFloat("_Density");
            currentColour = fogMaterial.GetColor("_Colour");
            currentMaxDistance = fogMaterial.GetFloat("_Max_Distance");
        }
        else
        {
            currentDensity = defaultDensity;
            currentColour = defaultColour;
            currentMaxDistance = defaultMaxDistance;
        }

        targetDensity = currentDensity;
        targetColour = currentColour;
        targetMaxDistance = currentMaxDistance;
    }

    private void Update()
    {
        if (fogMaterial == null)
        {
            if (!materialWarningShown)
            {
                materialWarningShown = true;
                Debug.LogWarning($"[{nameof(FogZoneManager)}] Материал тумана не указан! Перетащи fog_mat.mat в поле Material в инспекторе.");
            }
            return;
        }

        float t = transitionSpeed > 0f ? Time.deltaTime / transitionSpeed : 1f;
        if (t > 1f) t = 1f;

        currentDensity = Mathf.Lerp(currentDensity, targetDensity, t);
        currentColour = Color.Lerp(currentColour, targetColour, t);
        currentMaxDistance = Mathf.Lerp(currentMaxDistance, targetMaxDistance, t);

        fogMaterial.SetFloat("_Density", currentDensity);
        fogMaterial.SetColor("_Colour", currentColour);
        fogMaterial.SetFloat("_Max_Distance", currentMaxDistance);

        if (!initialValuesCaptured)
        {
            // Запоминаем исходные значения материала на момент старта игры —
            // чтобы вернуть их при выходе из Play-режима в редакторе.
            // Иначе изменения тумана, сделанные во время игры, НАВСЕГДА сохранятся
            // в ассет (известная ловушка Unity), и материал можно случайно испортить.
            startDensity = currentDensity;
            startColour = currentColour;
            startMaxDistance = currentMaxDistance;
            initialValuesCaptured = true;
        }
    }

    public void EnterZone(FogZone zone)
    {
        if (zone.Mode == FogZoneMode.Persistent)
        {
            //Persistent: значения записываются насовсем — становятся новыми дефолтными.
            //Убираем их из стека активных, чтобы они не сбрасывались при выходе.
            activeZones.Remove(zone);
            defaultDensity = zone.Density;
            defaultColour = zone.Colour;
            defaultMaxDistance = zone.MaxDistance;
        }
        else if (!activeZones.Contains(zone))
        {
            activeZones.Add(zone);
        }

        RecalculateTarget();
    }

    public void ExitZone(FogZone zone)
    {
        activeZones.Remove(zone);
        RecalculateTarget();
    }

    /// <summary>
    /// Цель — настройки зоны с наивысшим приоритетом среди активных (WhileInside),
    /// иначе — дефолт (который мог быть перезаписан Persistent-зоной).
    /// </summary>
    private void RecalculateTarget()
    {
        FogZone best = null;
        foreach (var z in activeZones)
        {
            if (best == null || z.Priority > best.Priority)
                best = z;
        }

        if (best != null)
        {
            targetDensity = best.Density;
            targetColour = best.Colour;
            targetMaxDistance = best.MaxDistance;
            transitionSpeed = best.Smooth ? best.TransitionTime : 0.01f;

            Debug.Log($"[FogZoneManager] Цель из зоны '{best.name}': density={targetDensity}, colour={targetColour}, maxDist={targetMaxDistance}");
        }
        else
        {
            targetDensity = defaultDensity;
            targetColour = defaultColour;
            targetMaxDistance = defaultMaxDistance;
            transitionSpeed = defaultTransitionTime;

            Debug.Log($"[FogZoneManager] Возврат к дефолту: density={targetDensity}, maxDist={targetMaxDistance}");
        }
    }

#if UNITY_EDITOR
    private void OnDisable()
    {
        // Выход из Play-режима в редакторе — откатываем материал к исходным значениям,
        // чтобы тестовые изменения не сохранились в ассет fog_mat.mat
        if (Application.isPlaying && initialValuesCaptured && fogMaterial != null)
        {
            fogMaterial.SetFloat("_Density", startDensity);
            fogMaterial.SetColor("_Colour", startColour);
            fogMaterial.SetFloat("_Max_Distance", startMaxDistance);
            UnityEditor.EditorUtility.SetDirty(fogMaterial);
            Debug.Log($"[{nameof(FogZoneManager)}] Материал тумана возвращён к исходным значениям после выхода из Play-режима.");
        }
    }
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fogMaterial != null && !fogMaterial.HasProperty("_Density"))
            Debug.LogWarning($"[{nameof(FogZoneManager)}] Материал не содержит свойства _Density — проверьте, что это fog_mat (AERO).");
    }
#endif
}
