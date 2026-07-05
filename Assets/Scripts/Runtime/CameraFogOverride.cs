using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class CameraFogOverride : MonoBehaviour
{
    Camera cam;
    bool restoreFog;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderedCamera)
    {
        if (renderedCamera != cam) return;
        restoreFog = RenderSettings.fog;
        RenderSettings.fog = false;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera renderedCamera)
    {
        if (renderedCamera != cam) return;
        RenderSettings.fog = restoreFog;
    }
}
