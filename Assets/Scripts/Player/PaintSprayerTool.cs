using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Краскопульт.
/// ЛКМ (ToolPrimary) — втягивание жидкости.
/// ПКМ (ToolSecondary) — распыление краски.
/// </summary>
public class PaintSprayerTool : MonoBehaviour
{
    [Header("Запас краски")]
    [SerializeField, Min(0.01f)] private float maxPaint = 100f;
    [SerializeField, Min(0f)] private float initialPaint = 100f;
    [SerializeField, Min(0f)] private float absorbRate = 25f;
    [SerializeField, Min(0f)] private float sprayRate = 20f;
    [SerializeField] private GameHud gameHud;

    [Header("Вибрация при попытке распыления без краски")]
    [SerializeField, Min(0f)] private float shakePositionAmplitude = 0.008f;
    [SerializeField, Min(0f)] private float shakeRotationAmplitude = 1.5f;
    [SerializeField, Min(0f)] private float shakeFrequency = 25f;

    public float CurrentPaint { get; private set; }
    public float MaxPaint => maxPaint;

    private ToolBase toolBase;
    private ToolController toolController;
    private PlayerInputActions input;

    private bool primaryHeld;
    private bool secondaryHeld;
    private bool primaryBlockedUntilRelease;
    private bool secondaryBlockedUntilRelease;
    private bool noPaintSprayAttempt;
    private float shakeTime;
    private Vector3 frameShakePosition;
    private Quaternion frameShakeRotation = Quaternion.identity;

    private void Awake()
    {
        toolBase = GetComponent<ToolBase>();
        toolController = GetComponentInParent<ToolController>();
        input = new PlayerInputActions();
        CurrentPaint = Mathf.Clamp(initialPaint, 0f, maxPaint);

        if (toolBase == null)
            Debug.LogError("[PaintSprayerTool] На объекте не найден ToolBase.", this);
        if (toolController == null)
            Debug.LogError("[PaintSprayerTool] В родителях не найден ToolController.", this);
        if (gameHud == null)
            Debug.LogWarning("[PaintSprayerTool] GameHud не назначен. Запас краски будет работать без обновления HUD.", this);
    }

    private void Start()
    {
        RefreshHud();
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.ToolPrimary.started += OnPrimaryStarted;
        input.Player.ToolPrimary.canceled += OnPrimaryCanceled;
        input.Player.ToolSecondary.started += OnSecondaryStarted;
        input.Player.ToolSecondary.canceled += OnSecondaryCanceled;
    }

    private void OnDisable()
    {
        input.Player.ToolPrimary.started -= OnPrimaryStarted;
        input.Player.ToolPrimary.canceled -= OnPrimaryCanceled;
        input.Player.ToolSecondary.started -= OnSecondaryStarted;
        input.Player.ToolSecondary.canceled -= OnSecondaryCanceled;
        input.Player.Disable();

        primaryHeld = false;
        secondaryHeld = false;
        StopActiveUse();
    }

    private void OnDestroy() => input.Dispose();

    private void Update()
    {
        if (toolController == null || toolBase == null)
            return;

        // Если кнопка была зажата до выбора инструмента, не начинаем действие
        // автоматически после его появления. Ожидаем отпускания и нового нажатия.
        if (toolController.ActiveTool != toolBase || toolController.IsSwitching)
        {
            if (primaryHeld) primaryBlockedUntilRelease = true;
            if (secondaryHeld) secondaryBlockedUntilRelease = true;
            StopActiveUse();
            return;
        }

        if (primaryHeld && !primaryBlockedUntilRelease)
        {
            StopNoPaintAttempt();
            AbsorbPaint();
        }
        else if (secondaryHeld && !secondaryBlockedUntilRelease)
        {
            SprayPaint();
        }
        else
        {
            StopActiveUse();
        }
    }

    private void LateUpdate()
    {
        if (!noPaintSprayAttempt || toolBase == null || toolBase.State != ToolState.NoPaint)
            return;

        shakeTime += Time.deltaTime * shakeFrequency;
        Vector3 offset = new Vector3(
            Mathf.Sin(shakeTime * 1.7f),
            Mathf.Cos(shakeTime * 2.3f),
            Mathf.Sin(shakeTime * 2.9f)) * shakePositionAmplitude;
        Quaternion rotation = Quaternion.Euler(
            Mathf.Sin(shakeTime * 2.1f) * shakeRotationAmplitude,
            Mathf.Cos(shakeTime * 1.8f) * shakeRotationAmplitude,
            Mathf.Sin(shakeTime * 2.7f) * shakeRotationAmplitude);

        // Добавляем небольшой offset к трансформации текущего кадра. ToolBobbing
        // задаёт базовое положение заново каждый кадр, поэтому shake не накапливается.
        toolBase.transform.localPosition += offset - frameShakePosition;
        toolBase.transform.localRotation = toolBase.transform.localRotation *
                                            Quaternion.Inverse(frameShakeRotation) * rotation;
        frameShakePosition = offset;
        frameShakeRotation = rotation;
    }

    private void OnPrimaryStarted(InputAction.CallbackContext context)
    {
        if (!IsActiveAndReady())
            primaryBlockedUntilRelease = true;
        primaryHeld = true;
    }

    private void OnPrimaryCanceled(InputAction.CallbackContext context)
    {
        primaryHeld = false;
        primaryBlockedUntilRelease = false;
        StopActiveUse();
    }

    private void OnSecondaryStarted(InputAction.CallbackContext context)
    {
        if (!IsActiveAndReady())
            secondaryBlockedUntilRelease = true;
        secondaryHeld = true;
    }

    private void OnSecondaryCanceled(InputAction.CallbackContext context)
    {
        secondaryHeld = false;
        secondaryBlockedUntilRelease = false;
        StopActiveUse();
        StopNoPaintAttempt();
    }

    private bool IsActiveAndReady()
    {
        return toolBase != null && toolController != null &&
               toolController.ActiveTool == toolBase &&
               !toolController.IsSwitching &&
               (toolBase.State == ToolState.Idle || toolBase.State == ToolState.NoPaint);
    }

    private void AbsorbPaint()
    {
        if (!toolBase.TryStartUsing() && toolBase.State != ToolState.Using)
            return;

        SetPaint(CurrentPaint + absorbRate * Time.deltaTime);
        if (CurrentPaint > 0f)
            toolBase.SetNoPaint(false);
    }

    private void SprayPaint()
    {
        if (CurrentPaint <= 0f)
        {
            StopActiveUse();
            toolBase.SetNoPaint(true);
            noPaintSprayAttempt = true;
            return;
        }

        StopNoPaintAttempt();
        if (!toolBase.TryStartUsing() && toolBase.State != ToolState.Using)
            return;

        SetPaint(CurrentPaint - sprayRate * Time.deltaTime);
        if (CurrentPaint <= 0f)
        {
            StopActiveUse();
            toolBase.SetNoPaint(true);
        }
    }

    private void SetPaint(float value)
    {
        CurrentPaint = Mathf.Clamp(value, 0f, maxPaint);
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (gameHud != null)
            gameHud.SetPaint(maxPaint <= 0f ? 0f : CurrentPaint / maxPaint * 100f);
    }

    private void StopActiveUse()
    {
        if (toolBase != null && toolBase.State == ToolState.Using)
            toolBase.StopUsing();
    }

    private void StopNoPaintAttempt()
    {
        if (toolBase != null)
        {
            // Убираем offset последнего кадра до возврата в Idle, чтобы он не
            // остался на модели, пока ToolBobbing был приостановлен в NoPaint.
            toolBase.transform.localPosition -= frameShakePosition;
            toolBase.transform.localRotation = toolBase.transform.localRotation * Quaternion.Inverse(frameShakeRotation);
        }

        noPaintSprayAttempt = false;
        shakeTime = 0f;
        frameShakePosition = Vector3.zero;
        frameShakeRotation = Quaternion.identity;
        if (toolBase != null && toolBase.State == ToolState.NoPaint)
            toolBase.SetNoPaint(false);
    }
}
