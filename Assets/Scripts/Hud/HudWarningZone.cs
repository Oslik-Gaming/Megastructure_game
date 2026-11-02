using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class HudWarningZone : MonoBehaviour
{
    [SerializeField] private GameHud hud;
    public void Configure(GameHud gameHud) => hud = gameHud;

    private void Awake() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            hud?.SetOutsideZone(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            hud?.SetOutsideZone(true);
    }

    private static bool IsPlayer(Collider other) => other.GetComponentInParent<PlayerMovements>() != null;
}
