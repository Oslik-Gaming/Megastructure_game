using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Состояния визуальной модели инструмента.
/// </summary>
public enum ToolState
{
    Hidden,
    Showing,
    Idle,
    Using,
    NoPaint,
    Hiding
}

/// <summary>
/// Общая основа для всех инструментов. Отвечает за их состояние и
/// анимацию появления/скрытия, реализованную перемещением и поворотом.
/// </summary>
public class ToolBase : MonoBehaviour
{
    [Header("Появление и скрытие")]
    [Tooltip("Длительность появления инструмента в секундах.")]
    [SerializeField, Min(0f)] private float showDuration = 0.25f;
    [Tooltip("Длительность скрытия инструмента в секундах.")]
    [SerializeField, Min(0f)] private float hideDuration = 0.25f;
    [Tooltip("Смещение от обычной позиции, в котором инструмент считается скрытым.")]
    [SerializeField] private Vector3 hiddenLocalPositionOffset = new Vector3(0f, -0.35f, 0.1f);
    [Tooltip("Дополнительный поворот инструмента в скрытом положении.")]
    [SerializeField] private Vector3 hiddenLocalEulerOffset = new Vector3(20f, 0f, 0f);

    public ToolState State { get; private set; }
    public bool IsTransitioning => State == ToolState.Showing || State == ToolState.Hiding;
    public bool CanUse => State == ToolState.Idle || State == ToolState.NoPaint;
    public bool IsVisible => State != ToolState.Hidden && State != ToolState.Hiding;

    /// <summary>Вызывается после полного появления инструмента.</summary>
    public event Action Shown;
    /// <summary>Вызывается после полного скрытия инструмента.</summary>
    public event Action Hidden;

    private Vector3 visibleLocalPosition;
    private Quaternion visibleLocalRotation;
    private Coroutine transitionRoutine;
    private Renderer[] renderers;
    private Collider[] colliders;

    protected virtual void Awake()
    {
        visibleLocalPosition = transform.localPosition;
        visibleLocalRotation = transform.localRotation;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        // Начальное состояние задаёт ToolController в Play Mode.
        // В редакторе компоненты не отключаются, чтобы модель оставалась видимой.
        State = ToolState.Idle;
    }

    /// <summary>
    /// Немедленно делает инструмент видимым. Используется контроллером при
    /// инициализации сцены, чтобы исправить случай, когда несколько инструментов
    /// случайно оставлены видимыми.
    /// </summary>
    public void SetVisibleImmediate()
    {
        StopTransition();
        transform.SetLocalPositionAndRotation(visibleLocalPosition, visibleLocalRotation);
        SetComponentsEnabled(true);
        State = ToolState.Idle;
    }

    /// <summary>
    /// Немедленно скрывает инструмент и отключает его Renderer и Collider.
    /// </summary>
    public void SetHiddenImmediate()
    {
        StopUsing();
        StopTransition();
        transform.SetLocalPositionAndRotation(HiddenLocalPosition, HiddenLocalRotation);
        SetComponentsEnabled(false);
        State = ToolState.Hidden;
    }

    /// <summary>
    /// Запускает анимацию появления. Вызов игнорируется, если инструмент уже
    /// показывается, виден или находится в использовании.
    /// </summary>
    public void Show()
    {
        if (State == ToolState.Showing || State == ToolState.Idle || State == ToolState.Using || State == ToolState.NoPaint)
            return;

        SetComponentsEnabled(true);
        StartTransition(visibleLocalPosition, visibleLocalRotation, showDuration, ToolState.Showing, ToolState.Idle, Shown);
    }

    /// <summary>
    /// Запускает анимацию скрытия. Использование при этом немедленно прекращается.
    /// </summary>
    public void Hide()
    {
        if (State == ToolState.Hidden || State == ToolState.Hiding)
            return;

        StopUsing();
        StartTransition(HiddenLocalPosition, HiddenLocalRotation, hideDuration, ToolState.Hiding, ToolState.Hidden, Hidden);
    }

    /// <summary>
    /// Пытается начать использование. Возвращает false во время анимации
    /// появления/скрытия либо когда инструмент полностью скрыт.
    /// </summary>
    public bool TryStartUsing()
    {
        if (!CanUse)
            return false;

        State = ToolState.Using;
        OnUseStarted();
        return true;
    }

    /// <summary>Завершает использование и возвращает инструмент в обычное состояние.</summary>
    public void StopUsing()
    {
        if (State != ToolState.Using)
            return;

        State = ToolState.Idle;
        OnUseStopped();
    }

    /// <summary>
    /// Устанавливает или снимает специальное состояние пустого краскопульта.
    /// Другие инструменты этот метод использовать не должны.
    /// </summary>
    public void SetNoPaint(bool noPaint)
    {
        if (IsTransitioning || State == ToolState.Hidden)
            return;

        if (noPaint)
        {
            StopUsing();
            State = ToolState.NoPaint;
            return;
        }

        if (State == ToolState.NoPaint)
            State = ToolState.Idle;
    }

    protected virtual void OnUseStarted() { }
    protected virtual void OnUseStopped() { }

    private Vector3 HiddenLocalPosition => visibleLocalPosition + hiddenLocalPositionOffset;
    private Quaternion HiddenLocalRotation => visibleLocalRotation * Quaternion.Euler(hiddenLocalEulerOffset);

    private void StartTransition(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        ToolState transitionState,
        ToolState completedState,
        Action completedCallback)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionRoutine(
            targetPosition,
            targetRotation,
            duration,
            transitionState,
            completedState,
            completedCallback));
    }

    private IEnumerator TransitionRoutine(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        ToolState transitionState,
        ToolState completedState,
        Action completedCallback)
    {
        State = transitionState;

        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;

        if (duration <= 0f)
        {
            transform.SetLocalPositionAndRotation(targetPosition, targetRotation);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.SetLocalPositionAndRotation(
                    Vector3.LerpUnclamped(startPosition, targetPosition, progress),
                    Quaternion.SlerpUnclamped(startRotation, targetRotation, progress));
                yield return null;
            }
        }

        transform.SetLocalPositionAndRotation(targetPosition, targetRotation);
        State = completedState;

        // Во время скрытия модель остаётся видимой до конца анимации, после чего
        // полностью перестаёт участвовать в рендеринге и физике.
        if (completedState == ToolState.Hidden)
            SetComponentsEnabled(false);

        transitionRoutine = null;
        completedCallback?.Invoke();
    }

    private void StopTransition()
    {
        if (transitionRoutine == null)
            return;

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private void SetComponentsEnabled(bool enabled)
    {
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    renderer.enabled = enabled;
            }
        }

        if (colliders != null)
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                    collider.enabled = enabled;
            }
        }
    }
}
