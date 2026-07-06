using UnityEngine;
using UnityEngine.Rendering;

// Disables scene fog ONLY while this camera renders, then restores it.
// Fog (RenderSettings.fog) is global, so without this the top-down mini-map
// camera gets fogged out even though fog looks fine in the main view.
// Attach this to the MiniMap Camera (alongside MiniMapFollow).
[RequireComponent(typeof(Camera))]
public class MiniMapNoFog : MonoBehaviour
{
    private Camera cam;
    private bool previousFog;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        // URP fires these around every camera's render.
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        // Only act for our mini-map camera.
        if (renderingCamera != cam)
            return;

        previousFog = RenderSettings.fog; // remember the real setting
        RenderSettings.fog = false;       // no fog for the mini-map
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera != cam)
            return;

        RenderSettings.fog = previousFog; // restore for every other camera
    }
}
