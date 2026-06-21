using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Single entry point for the endless mode. Sits on one GameObject in the
/// EndlessMode scene and constructs the entire game at runtime: lighting, the
/// player, a follow camera (reusing the class's <see cref="CameraFollow"/>),
/// the procedural <see cref="PlatformGenerator"/>, the <see cref="GameManager"/>,
/// and the <see cref="ScoreUI"/>. Building in code keeps the scene self-contained
/// and reproducible for anyone who re-downloads the project.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    void Start()
    {
        GameManager gameManager = gameObject.AddComponent<GameManager>();

        SetupLighting();
        Transform player = CreatePlayer();
        CreateCamera(player);
        PlatformGenerator generator = CreateGenerator(player);
        CreateUI();
        EnsureEventSystem();

        gameManager.Init(player, PlatformGenerator.SectionLength);
    }

    void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.45f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.2f);

        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.4f;
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    Transform CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.GetComponent<Renderer>().sharedMaterial =
            MakeMaterial(new Color(0.95f, 0.95f, 0.98f));

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;

        player.AddComponent<PlayerController>();
        return player.transform;
    }

    void CreateCamera(Transform player)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.16f, 0.20f, 0.28f);
        cam.fieldOfView = 60f;
        camGo.AddComponent<AudioListener>();

        Vector3 offset = new Vector3(0f, 7f, -10f);
        camGo.transform.position = player.position + offset;
        camGo.transform.rotation =
            Quaternion.LookRotation((player.position + Vector3.forward * 6f) - camGo.transform.position);

        CameraFollow follow = camGo.AddComponent<CameraFollow>();
        follow.target = player;
        follow.offset = offset;
    }

    PlatformGenerator CreateGenerator(Transform player)
    {
        GameObject genGo = new GameObject("PlatformGenerator");
        PlatformGenerator generator = genGo.AddComponent<PlatformGenerator>();
        generator.Initialize(player);
        return generator;
    }

    void CreateUI()
    {
        GameObject uiGo = new GameObject("UI");
        ScoreUI ui = uiGo.AddComponent<ScoreUI>();
        ui.Build();
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ----------------------------------------------------------------------

    /// <summary>
    /// Creates a material using the URP Lit shader when available (the project
    /// uses URP) and falls back to the built-in pipeline shader otherwise.
    /// </summary>
    public static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.color = color;
        return material;
    }
}
