using UnityEngine;
using UnityEngine.Rendering;

// Fakes a visible "volumetric" flashlight beam: a soft glowing cone of light hanging in the air,
// like a real torch beam catching dust/fog. URP does NOT render light in mid-air, so instead we
// build a translucent cone MESH and let an additive fresnel shader (FlashlightBeam.shader) make
// it glow at the edges so it reads as a shaft of light.
//
// SETUP: put this on a child object of the flashlight, sitting at the lens and pointing along its
// local +Z (forward) — the same direction the Spot Light beam points. Then assign that object to
// the Flashlight script's "Beam Cone" slot so it shows/hides with the light. This is purely
// cosmetic; the real "safe light" is still the LightZone + Spot Light.
[ExecuteAlways] // rebuild in the Editor too, so you can tune the cone without pressing Play
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FlashlightBeamCone : MonoBehaviour
{
    [Header("Shape (apex at this object, opening along local +Z)")]
    [Tooltip("How far the beam reaches, in metres. Match this roughly to the Spot Light's Range.")]
    public float length = 18f;
    [Tooltip("Full cone angle in degrees. Match this roughly to the Spot Light's Spot Angle.")]
    [Range(1f, 179f)] public float coneAngle = 40f;
    [Tooltip("How many sides the cone has. 24 is smooth and cheap.")]
    [Range(6, 64)] public int segments = 24;

    [Header("Look")]
    [Tooltip("Beam colour/tint. Slightly warm reads like a real bulb.")]
    public Color color = new Color(1f, 0.95f, 0.8f, 1f);
    [Tooltip("Overall brightness of the glow.")]
    [Range(0f, 5f)] public float intensity = 1f;
    [Tooltip("Higher = the glow hugs the silhouette edges more (thinner, sharper shaft).")]
    [Range(0.25f, 8f)] public float edgeSoftness = 2.5f;
    [Tooltip("Glow strength at the lens (near) end.")]
    [Range(0f, 1f)] public float nearAlpha = 0.9f;
    [Tooltip("Glow strength at the far end (fades into the dark).")]
    [Range(0f, 1f)] public float farAlpha = 0.05f;

    Mesh mesh;
    Material mat;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int FresnelId = Shader.PropertyToID("_FresnelPower");

    void OnEnable() { Rebuild(); }

    void OnValidate()
    {
        length = Mathf.Max(0.1f, length);
        if (isActiveAndEnabled) Rebuild();
    }

    public void Rebuild()
    {
        EnsureRefs();
        BuildConeMesh();
        PushMaterial();
    }

    void EnsureRefs()
    {
        var mf = GetComponent<MeshFilter>();
        var rend = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh { name = "FlashlightBeamCone" };
            mesh.hideFlags = HideFlags.DontSave; // don't leave a stray mesh asset in the project
        }
        mf.sharedMesh = mesh;

        if (mat == null)
        {
            Shader sh = Shader.Find("Darkless/FlashlightBeam");
            if (sh == null)
            {
                Debug.LogWarning("FlashlightBeamCone: shader 'Darkless/FlashlightBeam' not found. " +
                    "Make sure Assets/Shaders/FlashlightBeam.shader exists (and add it to " +
                    "Project Settings ▸ Graphics ▸ Always Included Shaders for builds).", this);
            }
            else
            {
                mat = new Material(sh) { name = "FlashlightBeam (runtime)", hideFlags = HideFlags.DontSave };
            }
        }

        if (mat != null) rend.sharedMaterial = mat;
        // A glow never casts/receives shadows or lighting probes.
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.lightProbeUsage = LightProbeUsage.Off;
        rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    // Builds a cone as a fan of flat triangles: a shared apex at the origin, opening out to a ring
    // of radius (length * tan(halfAngle)) at z = length. Flat per-facet normals feed the fresnel.
    void BuildConeMesh()
    {
        if (mesh == null) return;

        float endRadius = length * Mathf.Tan(coneAngle * 0.5f * Mathf.Deg2Rad);

        Vector3[] verts = new Vector3[segments * 3];
        Vector3[] normals = new Vector3[segments * 3];
        Color[] colors = new Color[segments * 3];
        int[] tris = new int[segments * 3];

        Color nearC = new Color(1f, 1f, 1f, nearAlpha);
        Color farC = new Color(1f, 1f, 1f, farAlpha);

        for (int i = 0; i < segments; i++)
        {
            float t0 = (float)i / segments * Mathf.PI * 2f;
            float t1 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector3 apex = Vector3.zero;
            Vector3 f0 = new Vector3(Mathf.Cos(t0) * endRadius, Mathf.Sin(t0) * endRadius, length);
            Vector3 f1 = new Vector3(Mathf.Cos(t1) * endRadius, Mathf.Sin(t1) * endRadius, length);

            int b = i * 3;
            verts[b + 0] = apex;
            verts[b + 1] = f0;
            verts[b + 2] = f1;

            colors[b + 0] = nearC;
            colors[b + 1] = farC;
            colors[b + 2] = farC;

            // Flat outward normal for this facet (pointing away from the cone's axis).
            Vector3 n = Vector3.Cross(f1 - apex, f0 - apex).normalized;
            Vector3 mid = (apex + f0 + f1) / 3f;
            Vector3 outward = new Vector3(mid.x, mid.y, 0f);
            if (Vector3.Dot(n, outward) < 0f) n = -n;
            normals[b + 0] = n;
            normals[b + 1] = n;
            normals[b + 2] = n;

            tris[b + 0] = b + 0;
            tris[b + 1] = b + 1;
            tris[b + 2] = b + 2;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
    }

    void PushMaterial()
    {
        if (mat == null) return;
        mat.SetColor(ColorId, color);
        mat.SetFloat(IntensityId, intensity);
        mat.SetFloat(FresnelId, edgeSoftness);
    }
}
