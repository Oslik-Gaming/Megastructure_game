using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TrashDisposeZone : MonoBehaviour
{
    [Tooltip("Уничтожить объект мусора после учёта в локации.")]
    [SerializeField] private bool destroyGameObject = true;
    [Min(0f)]
    [SerializeField] private float destroyDelay;
    [Tooltip("Если задано, зона принимает мусор только этой локации.")]
    [SerializeField] private TrashLocation requiredLocation;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TrashItem item = other.GetComponentInParent<TrashItem>();
        if (item == null || item.IsRemoved)
            return;

        if (requiredLocation != null && item.HomeLocation != requiredLocation)
            return;

        item.Remove();

        if (destroyGameObject)
            Destroy(item.gameObject, destroyDelay);
    }
}
