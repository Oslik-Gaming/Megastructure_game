using UnityEngine;

/// <summary>
/// Procedurally generates a cone mesh for the heater tool visual effect.
/// The cone is generated at runtime based on length and opening angle.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HeaterConeVisual : MonoBehaviour
{
    [Header("Cone Parameters")]
    [Tooltip("Length of the cone from apex to base.")]
    [SerializeField, Min(0.1f)] private float coneLength = 3f;

    [Tooltip("Full opening angle of the cone in degrees.")]
    [SerializeField, Range(5f, 120f)] private float coneOpeningAngle = 45f;

    [Tooltip("Number of segments around the cone circumference.")]
    [SerializeField, Range(8, 64)] private int segments = 24;

    private MeshFilter meshFilter;
    private Mesh coneMesh;

    /// <summary>Current cone length. Use this to keep visual and logic in sync.</summary>
    public float ConeLength => coneLength;

    /// <summary>Current opening angle. Use this to keep visual and logic in sync.</summary>
    public float ConeOpeningAngle => coneOpeningAngle;

    /// <summary>Radius at the base of the cone.</summary>
    public float BaseRadius => coneLength * Mathf.Tan(coneOpeningAngle * 0.5f * Mathf.Deg2Rad);

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        GenerateConeMesh();
    }

    /// <summary>
    /// Set cone parameters from external source (e.g., HeaterTool).
    /// </summary>
    public void SetConeParameters(float length, float openingAngle)
    {
        coneLength = length;
        coneOpeningAngle = Mathf.Clamp(openingAngle, 5f, 120f);
        GenerateConeMesh();
    }

    /// <summary>
    /// Generate the cone mesh procedurally.
    /// </summary>
    private void GenerateConeMesh()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (coneMesh == null)
        {
            coneMesh = new Mesh();
            coneMesh.name = "HeaterCone";
        }
        else
        {
            coneMesh.Clear();
        }

        float radius = BaseRadius;
        int vertexCount = segments + 2; // apex + base vertices + center of base
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * 6]; // side triangles + base cap

        // Apex vertex at origin (local space)
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0f);

        // Base center vertex
        int baseCenterIndex = segments + 1;
        vertices[baseCenterIndex] = new Vector3(0f, 0f, coneLength);
        uvs[baseCenterIndex] = new Vector2(0.5f, 1f);

        // Generate base circle vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, y, coneLength);
            uvs[i + 1] = new Vector2((float)i / segments, 1f);
        }

        // Generate side triangles (cone surface)
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = (i + 1) % segments + 1;

            // Triangle from apex to base edge
            triangles[triIndex++] = 0;
            triangles[triIndex++] = current;
            triangles[triIndex++] = next;
        }

        // Generate base cap triangles
        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = (i + 1) % segments + 1;

            // Triangle from base center to edge (facing backward)
            triangles[triIndex++] = baseCenterIndex;
            triangles[triIndex++] = next;
            triangles[triIndex++] = current;
        }

        coneMesh.vertices = vertices;
        coneMesh.uv = uvs;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();

        meshFilter.sharedMesh = coneMesh;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && meshFilter != null)
        {
            GenerateConeMesh();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw cone wireframe for visualization
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        float radius = BaseRadius;
        Vector3 apex = Vector3.zero;
        Vector3 baseCenter = new Vector3(0f, 0f, coneLength);

        // Draw cone edges
        int gizmoSegments = 16;
        for (int i = 0; i < gizmoSegments; i++)
        {
            float angle1 = (float)i / gizmoSegments * Mathf.PI * 2f;
            float angle2 = (float)(i + 1) / gizmoSegments * Mathf.PI * 2f;

            Vector3 p1 = new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, coneLength);
            Vector3 p2 = new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, coneLength);

            // Base circle
            Gizmos.DrawLine(p1, p2);

            // Lines from apex to base
            if (i % 4 == 0)
                Gizmos.DrawLine(apex, p1);
        }

        // Draw axis
        Gizmos.color = Color.red;
        Gizmos.DrawLine(apex, baseCenter);
    }
#endif
}
