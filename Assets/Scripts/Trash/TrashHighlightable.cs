using UnityEngine;

/// <summary>Renders an outline proxy for every visual renderer in a trash item.</summary>
[DisallowMultipleComponent]
public sealed class TrashHighlightable : MonoBehaviour
{
    private sealed class Proxy
    {
        public Renderer Source;
        public GameObject Object;
        public Mesh Mesh;
        public SkinnedMeshRenderer Skinned;
        public Material[] Materials;
    }

    private readonly System.Collections.Generic.List<Proxy> proxies = new();
    private Color color = Color.green;
    private float intensity;
    private float targetIntensity;
    private float fadeDuration = 0.3f;

    public void Initialize(Color outlineColor, float fadeOutDuration)
    {
        color = outlineColor;
        fadeDuration = Mathf.Max(0.01f, fadeOutDuration);
        if (proxies.Count == 0) BuildProxies();
        SetIntensity(0f);
    }

    public void SetHighlighted(bool value) => targetIntensity = value ? 1f : 0f;

    private void Awake() => BuildProxies();

    private void BuildProxies()
    {
        if (proxies.Count > 0) return;
        foreach (Renderer source in GetComponentsInChildren<Renderer>(true))
        {
            if (source is ParticleSystemRenderer) continue;
            var proxyObject = new GameObject(source.name + "_TrashOutline");
            proxyObject.transform.SetParent(source.transform, false);
            proxyObject.layer = source.gameObject.layer;
            var proxy = new Proxy { Source = source, Object = proxyObject };
            if (source is SkinnedMeshRenderer skinned)
            {
                proxy.Skinned = skinned;
                proxy.Mesh = new Mesh { name = source.name + "_OutlineMesh" };
                proxyObject.AddComponent<MeshFilter>().sharedMesh = proxy.Mesh;
                proxyObject.AddComponent<MeshRenderer>();
            }
            else if (source is MeshRenderer meshRenderer && source.GetComponent<MeshFilter>()?.sharedMesh != null)
            {
                proxy.Mesh = source.GetComponent<MeshFilter>().sharedMesh;
                proxyObject.AddComponent<MeshFilter>().sharedMesh = proxy.Mesh;
                proxyObject.AddComponent<MeshRenderer>();
            }
            else
            {
                Destroy(proxyObject);
                continue;
            }
            var meshRendererProxy = proxyObject.GetComponent<MeshRenderer>();
            int materialCount = Mathf.Max(1, proxy.Mesh.subMeshCount);
            proxy.Materials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
                proxy.Materials[i] = CreateMaterial();
            meshRendererProxy.sharedMaterials = proxy.Materials;
            proxies.Add(proxy);
            proxyObject.SetActive(false);
        }
    }

    private Material CreateMaterial()
    {
        var shader = Shader.Find("Megastructure/TrashOutline");
        var material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        material.hideFlags = HideFlags.HideAndDontSave;
        material.SetColor("_Color", color);
        return material;
    }

    private void LateUpdate()
    {
        if (Mathf.Abs(intensity - targetIntensity) > 0.001f)
        {
            float speed = targetIntensity > intensity ? 10f : 1f / fadeDuration;
            SetIntensity(Mathf.MoveTowards(intensity, targetIntensity, speed * Time.deltaTime));
        }
        foreach (var proxy in proxies)
        {
            if (proxy.Skinned != null && proxy.Object.activeSelf)
                proxy.Skinned.BakeMesh(proxy.Mesh);
        }
    }

    private void SetIntensity(float value)
    {
        intensity = value;
        foreach (var proxy in proxies)
        {
            proxy.Object.SetActive(value > 0.001f);
            foreach (var material in proxy.Materials)
            {
                material.SetColor("_Color", new Color(color.r, color.g, color.b, value));
                material.SetFloat("_Intensity", value);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var proxy in proxies)
        {
            foreach (var material in proxy.Materials) Destroy(material);
            if (proxy.Mesh != null && proxy.Skinned != null) Destroy(proxy.Mesh);
            if (proxy.Object != null) Destroy(proxy.Object);
        }
    }
}
