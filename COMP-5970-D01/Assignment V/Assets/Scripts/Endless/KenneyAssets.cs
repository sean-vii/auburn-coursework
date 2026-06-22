using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads and instantiates models from the bundled Kenney Platformer Kit (CC0).
/// All Kenney models share a single palette texture (variation-a), so one URP
/// material renders every model with its intended colours. Models live in a
/// Resources folder so they can be loaded at runtime by name with no scene wiring.
/// </summary>
public static class KenneyAssets
{
    static Material sharedMaterial;
    static readonly Dictionary<string, Object> prefabCache = new Dictionary<string, Object>();

    /// <summary>True when the kit's models are present in Resources.</summary>
    public static bool Available
    {
        get { return Load("block-grass") != null; }
    }

    static Object Load(string modelName)
    {
        if (prefabCache.TryGetValue(modelName, out Object cached))
        {
            return cached;
        }
        Object prefab = Resources.Load<GameObject>(modelName);
        prefabCache[modelName] = prefab;
        return prefab;
    }

    static Material SharedMaterial()
    {
        if (sharedMaterial != null)
        {
            return sharedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        sharedMaterial = new Material(shader);

        Texture texture = Resources.Load<Texture2D>("variation-a");
        if (texture != null)
        {
            if (sharedMaterial.HasProperty("_BaseMap")) sharedMaterial.SetTexture("_BaseMap", texture);
            sharedMaterial.mainTexture = texture;
        }
        // Matte-ish so the flat palette colours read cleanly.
        if (sharedMaterial.HasProperty("_Smoothness")) sharedMaterial.SetFloat("_Smoothness", 0.05f);
        if (sharedMaterial.HasProperty("_Glossiness")) sharedMaterial.SetFloat("_Glossiness", 0.05f);
        return sharedMaterial;
    }

    /// <summary>
    /// Instantiates <paramref name="modelName"/> as a child of <paramref name="parent"/>,
    /// scaled to fill a box of <paramref name="targetSize"/> centred at
    /// <paramref name="localCenter"/> (parent-local). Returns null if the model
    /// is missing so callers can fall back to a primitive.
    /// </summary>
    public static GameObject SpawnFitted(string modelName, Transform parent, Vector3 localCenter, Vector3 targetSize, bool uniform)
    {
        Object prefab = Load(modelName);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate((GameObject)prefab);
        instance.name = "Model_" + modelName;

        StripColliders(instance);
        ApplyMaterial(instance);

        instance.transform.SetParent(parent, false);
        FitToBox(instance.transform, targetSize, localCenter, uniform);
        return instance;
    }

    static void StripColliders(GameObject go)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
        {
            Object.Destroy(c);
        }
    }

    static void ApplyMaterial(GameObject go)
    {
        Material mat = SharedMaterial();
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            r.sharedMaterials = mats;
        }
    }

    static Bounds WorldBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one);
        }
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    static void FitToBox(Transform t, Vector3 targetSize, Vector3 localCenter, bool uniform)
    {
        Bounds b = WorldBounds(t);
        Vector3 size = b.size;

        Vector3 scale = new Vector3(
            size.x > 1e-4f ? targetSize.x / size.x : 1f,
            size.y > 1e-4f ? targetSize.y / size.y : 1f,
            size.z > 1e-4f ? targetSize.z / size.z : 1f);

        if (uniform)
        {
            float m = Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
            scale = new Vector3(m, m, m);
        }

        t.localScale = Vector3.Scale(t.localScale, scale);

        // Re-centre the (now scaled) model on the target box centre.
        Bounds after = WorldBounds(t);
        Vector3 worldCenter = (t.parent != null) ? t.parent.TransformPoint(localCenter) : localCenter;
        t.position += worldCenter - after.center;
    }
}
