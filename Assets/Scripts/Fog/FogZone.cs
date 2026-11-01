using UnityEngine;

/// <summary>
/// Режим работы зоны:
/// WhileInside — параметры действуют только пока игрок в зоне (вышел — туман вернулся к дефолту);
/// Persistent — параметры применяются один раз при входе и остаются до входа в другую зону.
/// </summary>
public enum FogZoneMode
{
    WhileInside,
    Persistent
}

/// <summary>
/// Триггерная зона тумана. Вешается на объект с BoxCollider (isTrigger = true).
/// При входе игрока FogZoneManager начинает менять параметры тумана
/// на заданные здесь значения.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class FogZone : MonoBehaviour
{
    [Header("Режим зоны")]
    [SerializeField] private FogZoneMode mode = FogZoneMode.WhileInside;

    [Header("Параметры тумана в этой зоне")]
    [SerializeField] private float density = 0.05f;
    [SerializeField] private Color colour = new Color(0.2754f, 0.2385f, 0.2385f, 1f);
    [SerializeField] private float maxDistance = 150f;

    [Header("Переход")]
    [Tooltip("Если выключено — параметры применяются мгновенно при входе.")]
    [SerializeField] private bool smooth = true;
    [Tooltip("Время плавного перехода в секундах.")]
    [SerializeField, Min(0.01f)] private float transitionTime = 2f;

    [Header("Приоритет (если зоны перекрываются, побеждает с большим числом)")]
    [SerializeField] private int priority = 0;

    public float Density => density;
    public Color Colour => colour;
    public float MaxDistance => maxDistance;
    public bool Smooth => smooth;
    public float TransitionTime => transitionTime;
    public int Priority => priority;
    public FogZoneMode Mode => mode;

    private void Reset()
    {
        // при добавлении компонента сразу настраиваем коллайдер как триггер
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (FogZoneManager.Instance != null)
            FogZoneManager.Instance.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Persistent-зона «пропитывает» туман: при выходе ничего не откатываем
        if (mode == FogZoneMode.Persistent)
            return;

        if (FogZoneManager.Instance != null)
            FogZoneManager.Instance.ExitZone(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;
        Gizmos.color = new Color(colour.r, colour.g, colour.b, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = new Color(colour.r, colour.g, colour.b, 1f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
#endif
}
