using UnityEngine;

/// <summary>
/// Общее покачивание активного инструмента при ходьбе и беге.
/// Компонент размещается на том же объекте, что и ToolController.
/// </summary>
public class ToolBobbing : MonoBehaviour
{
    [Header("Покачивание при ходьбе")]
    [Tooltip("Частота покачивания при ходьбе.")]
    [SerializeField, Min(0f)] private float walkFrequency = 7f;
    [Tooltip("Вертикальная амплитуда покачивания.")]
    [SerializeField, Min(0f)] private float verticalAmplitude = 0.025f;
    [Tooltip("Горизонтальная амплитуда покачивания.")]
    [SerializeField, Min(0f)] private float horizontalAmplitude = 0.015f;
    [Tooltip("Амплитуда поворота инструмента в градусах.")]
    [SerializeField, Min(0f)] private float rotationAmplitude = 2f;

    [Header("Покачивание при беге")]
    [Tooltip("Во сколько раз увеличиваются амплитуда и частота при беге.")]
    [SerializeField, Min(1f)] private float sprintMultiplier = 1.4f;

    [Header("Возврат в исходное положение")]
    [Tooltip("Скорость возврата инструмента к исходной позиции и повороту.")]
    [SerializeField, Min(0.01f)] private float returnSpeed = 8f;

    [Header("Определение движения")]
    [Tooltip("Минимальная горизонтальная скорость, при которой начинается bobbing.")]
    [SerializeField, Min(0f)] private float movementThreshold = 0.05f;

    private ToolController toolController;
    private PlayerMovements playerMovements;
    private Rigidbody playerRigidbody;

    private ToolBase trackedTool;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float phase;
    private bool hasBaseTransform;

    private void Awake()
    {
        toolController = GetComponent<ToolController>();
        playerMovements = GetComponentInParent<PlayerMovements>();
        playerRigidbody = GetComponentInParent<Rigidbody>();

        if (toolController == null)
            Debug.LogError("[ToolBobbing] На этом объекте не найден ToolController.", this);

        if (playerMovements == null)
            Debug.LogError("[ToolBobbing] Не найден PlayerMovements в родителях ToolAnchor.", this);

        if (playerRigidbody == null)
            Debug.LogError("[ToolBobbing] Не найден Rigidbody игрока в родителях ToolAnchor.", this);
    }

    private void LateUpdate()
    {
        if (toolController == null)
            return;

        ToolBase activeTool = toolController.ActiveTool;
        if (activeTool == null)
            return;

        if (activeTool != trackedTool)
        {
            trackedTool = activeTool;
            hasBaseTransform = false;
            phase = 0f;
        }

        // ToolBase сам управляет Transform во время появления и скрытия.
        // Не вмешиваемся, пока переход не завершён.
        if (toolController.IsSwitching || activeTool.State != ToolState.Idle)
        {
            phase = 0f;
            return;
        }

        if (!hasBaseTransform)
        {
            baseLocalPosition = activeTool.transform.localPosition;
            baseLocalRotation = activeTool.transform.localRotation;
            hasBaseTransform = true;
        }

        bool grounded = playerMovements != null && playerMovements.IsOnGround;
        Vector3 horizontalVelocity = playerRigidbody != null
            ? new Vector3(playerRigidbody.linearVelocity.x, 0f, playerRigidbody.linearVelocity.z)
            : Vector3.zero;
        bool moving = horizontalVelocity.sqrMagnitude > movementThreshold * movementThreshold;

        // В воздухе bobbing не накапливается. При следующем движении после
        // приземления фаза начнётся заново с нуля.
        if (!grounded)
        {
            phase = 0f;
            ReturnToBase(activeTool.transform);
            return;
        }

        if (!moving)
        {
            phase = 0f;
            ReturnToBase(activeTool.transform);
            return;
        }

        bool sprinting = playerMovements != null && playerMovements.IsSprinting;
        float multiplier = sprinting ? sprintMultiplier : 1f;
        float frequency = walkFrequency * multiplier;
        float amplitude = multiplier;

        phase += frequency * Time.deltaTime;

        Vector3 offset = new Vector3(
            Mathf.Cos(phase * 0.5f) * horizontalAmplitude * amplitude,
            Mathf.Sin(phase) * verticalAmplitude * amplitude,
            0f);

        Quaternion rotationOffset = Quaternion.Euler(
            Mathf.Sin(phase * 0.5f) * rotationAmplitude * amplitude,
            0f,
            Mathf.Cos(phase * 0.5f) * rotationAmplitude * amplitude);

        activeTool.transform.SetLocalPositionAndRotation(
            baseLocalPosition + offset,
            baseLocalRotation * rotationOffset);
    }

    private void ReturnToBase(Transform toolTransform)
    {
        float t = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);
        toolTransform.localPosition = Vector3.Lerp(toolTransform.localPosition, baseLocalPosition, t);
        toolTransform.localRotation = Quaternion.Slerp(toolTransform.localRotation, baseLocalRotation, t);
    }
}
