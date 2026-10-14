using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrashDemoSceneSetup : MonoBehaviour
{
    private const int TrashCount = 6;

    private void Awake()
    {
        CreateCamera();
        CreateLighting();

        GameObject locationObject = new GameObject("Demo Trash Location");
        CreateTrash(locationObject.transform);
        TrashLocation location = locationObject.AddComponent<TrashLocation>();

        CreateDisposeZone();
        CreateEnvironment();
        CreateHud(location);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(
            new Vector3(10f, 7f, -13f),
            Quaternion.Euler(20f, -37f, 0f));

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.backgroundColor = new Color(0.05f, 0.08f, 0.12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateDisposeZone()
    {
        GameObject zone = new GameObject("Trash Dispose Zone");
        zone.transform.position = new Vector3(0f, -1.5f, 0f);

        BoxCollider collider = zone.AddComponent<BoxCollider>();
        collider.size = new Vector3(20f, 1f, 20f);
        TrashDisposeZone disposeZone = zone.AddComponent<TrashDisposeZone>();
        disposeZone.name = "Trash Dispose Zone";
    }

    private static void CreateTrash(Transform parent)
    {
        for (int index = 0; index < TrashCount; index++)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = $"Trash {index + 1}";
            item.transform.SetParent(parent);
            item.transform.position = new Vector3(
                (index % 3 - 1) * 2.25f,
                3f + index * 1.5f,
                index / 3 * 2.25f);
            item.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Renderer renderer = item.GetComponent<Renderer>();
            renderer.material.color = Color.Lerp(new Color(0.25f, 0.8f, 0.45f), new Color(0.1f, 0.45f, 0.9f), index / (float)(TrashCount - 1));

            Rigidbody rigidbody = item.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            
            // Применяем свою силу гравитации каждый физический кадр
            Vector3 customGravity = Physics.gravity * 3.5f;
            rigidbody.AddForce(customGravity, ForceMode.Acceleration);
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            item.AddComponent<TrashItem>();
        }
    }

    private static void CreateEnvironment()
    {
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Demo Frame";
        frame.transform.position = new Vector3(0f, -2.1f, 0f);
        frame.transform.localScale = new Vector3(14f, 0.25f, 10f);
        Object.Destroy(frame.GetComponent<Collider>());
        frame.GetComponent<Renderer>().material.color = new Color(0.15f, 0.18f, 0.23f);
    }

    private static void CreateHud(TrashLocation location)
    {
        GameObject canvasObject = new GameObject("Trash Demo HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        TextMeshProUGUI title = CreateText("Title", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -45f), 28);
        title.text = "TRASH DISPOSAL DEMO";
        title.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI percent = CreateText("Remaining Percent", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f), 48);
        percent.alignment = TextAlignmentOptions.Center;
        percent.color = new Color(0.45f, 1f, 0.65f);
        percent.gameObject.AddComponent<TrashLocationHud>().Configure(location, percent, true);

        TextMeshProUGUI hint = CreateText("Hint", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f), 18);
        hint.text = "Objects fall into the disposal trigger one at a time.";
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.8f, 0.85f, 0.9f);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.sizeDelta = new Vector2(700f, 70f);
        rectTransform.anchoredPosition = anchoredPosition;
    
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = Resources.GetBuiltinResource<TMP_FontAsset>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        return text;
    }
}
