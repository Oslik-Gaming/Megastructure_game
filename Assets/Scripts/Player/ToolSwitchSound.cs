using UnityEngine;

/// <summary>
/// Звуковое сопровождение переключения инструментов.
/// Вешается на тот же объект, что и ToolController (или рядом — тогда укажите ToolController вручную).
/// При каждом появлении нового активного инструмента проигрывает звук.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ToolSwitchSound : MonoBehaviour
{
    [Tooltip("Наблюдаемый ToolController. Если пусто — берётся из родителей.")]
    [SerializeField] private ToolController toolController;

    [Tooltip("Звук переключения инструмента.")]
    [SerializeField] private AudioClip switchSound;

    [Tooltip("Громкость звука переключения.")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource audioSource;
    private ToolBase lastActiveTool;
    private bool primed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (toolController == null)
            toolController = GetComponentInParent<ToolController>();

        if (toolController == null)
            Debug.LogError("[ToolSwitchSound] Не найден ToolController (ни на объекте, ни в родителях).", this);
    }

    private void Update()
    {
        if (toolController == null)
            return;

        ToolBase activeTool = toolController.ActiveTool;

        if (activeTool == lastActiveTool)
            return;

        lastActiveTool = activeTool;

        // Первый обнаруженный инструмент — это просто стартовое состояние,
        // а не переключение, поэтому звук в этот момент не играем.
        if (!primed)
        {
            primed = true;
            return;
        }

        if (activeTool == null)
            return;

        if (switchSound != null && audioSource != null)
            audioSource.PlayOneShot(switchSound, volume);
    }
}
