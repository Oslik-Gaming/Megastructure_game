using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed partial class VRPlayerRig
{
    [SerializeField] private UniversalRendererData vrRenderer;

    private void ConfigureRendering()
    {
        var data = gameplayCamera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.allowXRRendering = !VRMode.Simulated;
        data.renderShadows = Application.platform != RuntimePlatform.Android;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.requiresDepthOption = CameraOverrideOption.Off;
        gameplayCamera.allowHDR = false;
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) return;
        var renderers = urp.rendererDataList;
        for (int i = 0; i < renderers.Length; ++i)
        {
            if (renderers[i] != vrRenderer) continue;
            data.SetRenderer(i);
            return;
        }
        data.SetRenderer(-1);
        Debug.LogWarning("VR renderer not found in the active URP asset. Run Tools/VR/Setup.", this);
    }
}
