using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Unity.XR.CoreUtils;

public static class VRSetup
{
    private const string Output = "Assets/VRGenerated";
    private const string TestScene = Output + "/VR_TestScene.unity";
    private const string Simulator = "Assets/Samples/XR Interaction Toolkit/3.3.2/XR Interaction Simulator/XR Interaction Simulator.prefab";

    [MenuItem("Tools/VR/Setup")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || scene.path == TestScene) return;
        var players = InScene<PlayerMovements>(scene).Where(p => p.gameObject.activeInHierarchy).ToArray();
        if (players.Length != 1)
        {
            Debug.LogError("Open the gameplay scene with one active PlayerMovements before Tools/VR/Setup.");
            return;
        }
        var player = players[0];
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets", "VRGenerated");
        var rig = GetOrCreateRig();
        var renderer = ConfigureRenderers();
        if (!WirePlayer(player, rig, renderer, false)) return;

        var hud = InScene<GameHud>(scene).FirstOrDefault();
        var pause = InScene<PauseMenuController>(scene).FirstOrDefault();
        var worldUI = player.GetComponent<VRWorldUI>() ?? Undo.AddComponent<VRWorldUI>(player.gameObject);
        var ui = new SerializedObject(worldUI);
        var hudDocument = hud != null ? new SerializedObject(hud).FindProperty("document").objectReferenceValue as UIDocument : null;
        Reference(ui, "hud", hudDocument);
        Reference(ui, "pause", pause);
        ui.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);

        foreach (var target in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (settings == null) continue;
            settings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            if (target == BuildTargetGroup.Android && settings.GetFeature<MetaQuestFeature>() is { } quest)
            {
                quest.enabled = true;
                EditorUtility.SetDirty(quest);
            }
            EditorUtility.SetDirty(settings);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("VR wired. Review the scene overrides, then save the scene. Use Tools/VR/Create Test Scene separately.", player);
    }

    private static T[] InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static VRRigReferences GetOrCreateRig()
    {
        string path = Output + "/VRRig.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<VRRigReferences>(path);
        if (existing != null) return existing;
        var root = new GameObject("XR Origin");
        root.SetActive(false); // the gameplay camera is supplied before activation
        try
        {
            var refs = root.AddComponent<VRRigReferences>();
            refs.origin = root.AddComponent<XROrigin>();
            refs.origin.Origin = root;
            refs.cameraOffset = Child(root.transform, "Camera Offset");
            refs.origin.CameraFloorOffsetObject = refs.cameraOffset.gameObject;
            refs.leftController = Child(refs.cameraOffset, "Left Controller");
            refs.rightController = Child(refs.cameraOffset, "Right Controller");
            VRPlayerRig.AddTracking(refs.leftController, "<XRController>{LeftHand}", "device");
            VRPlayerRig.AddTracking(refs.rightController, "<XRController>{RightHand}", "device");
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<VRRigReferences>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static UniversalRendererData ConfigureRenderers()
    {
        // Reuse the feature-free Mobile renderer on both pipelines, without replacing desktop renderers.
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
        var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (pc != null && renderer != null && !pc.rendererDataList.ToArray().Contains(renderer))
        {
            var so = new SerializedObject(pc);
            var list = so.FindProperty("m_RendererDataList");
            int index = list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            so.ApplyModifiedProperties();
        }
        return renderer;
    }

    [MenuItem("Tools/VR/Create Quest Pipeline")]
    public static void CreateQuestPipeline()
    {
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets", "VRGenerated");
        const string path = Output + "/Quest_RPAsset.asset";
        if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) != null) return;
        if (!AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset", path))
        {
            Debug.LogError("Could not create Quest pipeline from Mobile_RPAsset.");
            return;
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        var settings = new SerializedObject(pipeline);
        settings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
        settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
        settings.FindProperty("m_SupportsHDR").boolValue = false;
        settings.FindProperty("m_MSAA").intValue = 4;
        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Selection.activeObject = pipeline;
        Debug.Log("Quest pipeline created. Assign it to a dedicated Android Quality level.");
    }

    private static bool WirePlayer(PlayerMovements player, VRRigReferences rig, UniversalRendererData renderer, bool simulation)
    {
        var cameras = player.GetComponentsInChildren<Camera>(true);
        var main = cameras.FirstOrDefault(c => c.targetTexture == null);
        var capsule = player.GetComponentsInChildren<CapsuleCollider>(true).FirstOrDefault(c => c.attachedRigidbody == player.GetComponent<Rigidbody>() && c.direction == 1);
        if (main == null || capsule == null)
        {
            Debug.LogError("VR requires a gameplay camera and a Y-aligned Rigidbody capsule.", player);
            return false;
        }
        var component = player.GetComponent<VRPlayerRig>() ?? Undo.AddComponent<VRPlayerRig>(player.gameObject);
        var so = new SerializedObject(component);
        Reference(so, "rigPrefab", rig);
        Reference(so, "gameplayCamera", main);
        Reference(so, "capsule", capsule);
        Reference(so, "vrRenderer", renderer);
        Reference(so, "tools", player.GetComponentInChildren<ToolController>(true));
        so.FindProperty("simulatorOnly").boolValue = simulation;
        var hidden = cameras.Where(c => c != main).Select(c => c.gameObject)
            .Concat(simulation ? Enumerable.Empty<GameObject>() : InScene<Camera>(player.gameObject.scene)
                .Where(c => c.transform.root.name == "DevCamera").Select(c => c.transform.root.gameObject))
            .Distinct().ToArray();
        var array = so.FindProperty("hideInVR");
        array.arraySize = hidden.Length;
        for (int i = 0; i < hidden.Length; ++i) array.GetArrayElementAtIndex(i).objectReferenceValue = hidden[i];
        so.ApplyModifiedProperties();
        return true;
    }

    [MenuItem("Tools/VR/Create Test Scene")]
    public static void CreateTestScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) != null)
        {
            Debug.Log("VR_TestScene already exists: " + TestScene);
            return;
        }
        if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder("Assets", "VRGenerated");
        Scene previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = Vector3.down * 0.25f;
            floor.transform.localScale = new Vector3(12, 0.5f, 12);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Room Scale Collision Wall";
            wall.transform.position = new Vector3(0, 1.5f, 3);
            wall.transform.localScale = new Vector3(6, 3, 0.25f);
            new GameObject("Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            var player = new GameObject("VR Test Player", typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerMovements));
            player.tag = "Player";
            player.transform.position = Vector3.up;
            player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            var camera = Child(player.transform, "Main Camera");
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<Camera>();
            camera.gameObject.AddComponent<AudioListener>();
            WirePlayer(player.GetComponent<PlayerMovements>(), GetOrCreateRig(), ConfigureRenderers(), true);
            player.AddComponent<VRWorldUI>();
            var simulator = AssetDatabase.LoadAssetAtPath<GameObject>(Simulator);
            if (simulator != null) PrefabUtility.InstantiatePrefab(simulator, scene);
            else Debug.LogWarning("Import the XRI XR Interaction Simulator sample, then add its prefab to VR_TestScene.");
            EditorSceneManager.SaveScene(scene, TestScene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (previous.IsValid() && previous != scene) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        Debug.Log("Created " + TestScene + ". Open it on its own with desktop XR startup disabled for simulated tracking.");
    }

    private static void Reference(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;

    private static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
}
