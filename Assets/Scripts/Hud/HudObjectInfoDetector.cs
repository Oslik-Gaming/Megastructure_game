using UnityEngine;

public sealed class HudObjectInfoDetector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float distance = 5f;
    [SerializeField] private GameHud hud;

    public void Configure(GameHud gameHud, Camera cameraToUse = null)
    {
        hud = gameHud;
        targetCamera = cameraToUse;
    }

    private void Update()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null || hud == null)
            return;

        Ray ray = cameraToUse.ViewportPointToRay(new Vector3(.5f, .5f));
        if (Physics.Raycast(ray, out RaycastHit hit, distance) &&
            hit.collider.GetComponentInParent<HudObjectInfo>() is HudObjectInfo info)
        {
            hud.ShowObject(info);
            return;
        }

        hud.HideObject();
    }
}
