using UnityEngine;

/// <summary>
/// Зона появления щупальца (дочерний объект TriggerZone у POINT_HORROR_N).
/// Игрок входит → запускает HorrorTentacleEvent на родителе.
/// Отдельный объект, чтобы зону было видно в иерархии и легко растягивать.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class HorrorTentacleTrigger : MonoBehaviour
{
    private void Awake() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovements>() == null)
            return;
        GetComponentInParent<HorrorTentacleEvent>()?.StartEvent();
    }
}
