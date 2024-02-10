using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

[Serializable]
public class ContactShadowSettings
{
    /// <summary>
    /// When enabled, HDRP processes Contact Shadows for this Volume.
    /// </summary>
    public bool enable;

    /// <summary>
    /// Controls the length of the rays HDRP uses to calculate Contact Shadows. It is in meters, but it gets scaled by a factor depending on Distance Scale Factor
    /// and the depth of the point from where the contact shadow ray is traced.
    /// </summary>
    [Range(0f, 1f)]
    public float length = 0.15f;

    /// <summary>
    /// Controls the opacity of the contact shadows.
    /// </summary>
    [Range(0f, 1f)]
    public float opacity = 1.0f;

    /// <summary>
    /// Scales the length of the contact shadow ray based on the linear depth value at the origin of the ray.
    /// </summary>
    [Range(0f, 1f)]
    public float distanceScaleFactor = 0.5f;

    /// <summary>
    /// The distance from the camera, in meters, at which HDRP begins to fade out Contact Shadows.
    /// </summary>
    [Min(0f)]
    public float maxDistance = 50.0f;

    /// <summary>
    /// The distance from the camera, in meters, at which HDRP begins to fade in Contact Shadows.
    /// </summary>
    [Min(0f)]
    public float minDistance = 0.0f;

    /// <summary>
    /// The distance, in meters, over which HDRP fades Contact Shadows out when past the Max Distance.
    /// </summary>
    [Min(0f)]
    public float fadeDistance = 5.0f;

    /// <summary>
    /// The distance, in meters, over which HDRP fades Contact Shadows in when past the Min Distance.
    /// </summary>
    [Min(0f)]
    public float fadeInDistance = 0;

    /// <summary>
    /// Controls the bias applied to the screen space ray cast to get contact shadows.
    /// </summary>
    [Range(0f, 1f)]
    public float rayBias = 0.2f;

    /// <summary>
    /// Controls the thickness of the objects found along the ray, essentially thickening the contact shadows.
    /// </summary>
    [Range(0.02f, 1f)]
    public float thicknessScale = 0.15f;

    /// <summary>
    /// Controls the numbers of samples taken during the ray-marching process for shadows. Increasing this might lead to higher quality at the expenses of performance.
    /// </summary>
    [Range(8, 64)]
    public int sampleCount = 8;
}

public class ContactShadow : ScriptableRendererFeature
{
    private ContactShadowPass m_ContactShadowPass;

    public RenderPassEvent m_RenderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
    public int m_RenderPassEventOffset = 1;

    public ComputeShader contactShadowComputeShader;
    public ContactShadowSettings m_ContactShadowSettings = new ContactShadowSettings();

    public override void Create()
    {
        m_ContactShadowPass =
            new ContactShadowPass(
                m_RenderPassEvent + m_RenderPassEventOffset,
                m_ContactShadowSettings,
                contactShadowComputeShader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ContactShadowPass);
    }
}

public class ContactShadowPass : ScriptableRenderPass
{
    private string m_ContactShadowMapProfileTag = "ContactShadowMap";
    private ProfilingSampler m_ContactShadowMapProfile;

    private ComputeShader m_ContactShadowComputeShader;
    private int m_DeferredContactShadowKernel;

    private ContactShadowSettings m_ContactShadows;
    private RenderTexture m_ContactShadowMap;

    public static readonly int st_ContactShadowParamsParametersID =
        Shader.PropertyToID("_ContactShadowParamsParameters");

    public static readonly int st_ContactShadowParamsParameters2ID =
        Shader.PropertyToID("_ContactShadowParamsParameters2");

    public static readonly int st_ContactShadowParamsParameters3ID =
        Shader.PropertyToID("_ContactShadowParamsParameters3");

    public static readonly int st_ContactShadowTextureUAVID = Shader.PropertyToID("_ContactShadowTextureUAV");

    public ContactShadowPass(RenderPassEvent rpe, ContactShadowSettings contactShadows, ComputeShader computeShader)
    {
        renderPassEvent = rpe;
        m_ContactShadows = contactShadows;
        m_ContactShadowComputeShader = computeShader;
        m_DeferredContactShadowKernel = m_ContactShadowComputeShader.FindKernel("ContactShadowMap");
        m_ContactShadowMapProfile = new ProfilingSampler(m_ContactShadowMapProfileTag);
    }

    public void UpdateContactShadowParams()
    {
        if (m_ContactShadows.enable)
            Shader.EnableKeyword("_CONTACT_SHADOW");
        else
            Shader.DisableKeyword("_CONTACT_SHADOW");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        UpdateContactShadowParams();
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!m_ContactShadows.enable)
            return;

        var cameraData = renderingData.cameraData;
        var camera = renderingData.cameraData.camera;

        if (m_ContactShadowMap == null || m_ContactShadowMap.height != camera.pixelHeight ||
            m_ContactShadowMap.width != camera.pixelWidth)
        {
            if (m_ContactShadowMap != null)
                m_ContactShadowMap.Release();

            m_ContactShadowMap = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, GraphicsFormat.R8_UNorm);
            m_ContactShadowMap.name = "contactShadowMap";
            m_ContactShadowMap.useMipMap = false;
            m_ContactShadowMap.autoGenerateMips = false;
            m_ContactShadowMap.enableRandomWrite = true;
            m_ContactShadowMap.wrapMode = TextureWrapMode.Clamp;
            m_ContactShadowMap.filterMode = FilterMode.Point;
            m_ContactShadowMap.Create();

            Shader.SetGlobalTexture("_ContactShadowMap", m_ContactShadowMap);
        }

        float contactShadowRange = Mathf.Clamp(m_ContactShadows.fadeDistance, 0.0f, m_ContactShadows.maxDistance);
        float contactShadowFadeEnd = m_ContactShadows.maxDistance;
        float contactShadowOneOverFadeRange = 1.0f / Mathf.Max(1e-6f, contactShadowRange);

        float contactShadowMinDist = Mathf.Min(m_ContactShadows.minDistance, contactShadowFadeEnd);
        float contactShadowFadeIn = Mathf.Clamp(m_ContactShadows.fadeInDistance, 1e-6f, contactShadowFadeEnd);

        var params1 = new Vector4(m_ContactShadows.length, m_ContactShadows.distanceScaleFactor, contactShadowFadeEnd,
            contactShadowOneOverFadeRange);
        var params2 = new Vector4(0, contactShadowMinDist, contactShadowFadeIn, m_ContactShadows.rayBias * 0.01f);
        var params3 = new Vector4(m_ContactShadows.sampleCount, m_ContactShadows.thicknessScale * 10.0f,
            Time.renderedFrameCount % 8, 0.0f);

        CommandBuffer cmd = CommandBufferPool.Get(m_ContactShadowMapProfileTag);
        using (new ProfilingScope(cmd, m_ContactShadowMapProfile))
        {
            cmd.SetComputeVectorParam(m_ContactShadowComputeShader, st_ContactShadowParamsParametersID, params1);
            cmd.SetComputeVectorParam(m_ContactShadowComputeShader, st_ContactShadowParamsParameters2ID, params2);
            cmd.SetComputeVectorParam(m_ContactShadowComputeShader, st_ContactShadowParamsParameters3ID, params3);
            cmd.SetComputeTextureParam(m_ContactShadowComputeShader, m_DeferredContactShadowKernel,
                st_ContactShadowTextureUAVID, m_ContactShadowMap);

            cmd.DispatchCompute(m_ContactShadowComputeShader, m_DeferredContactShadowKernel,
                Mathf.CeilToInt(camera.pixelWidth / 8.0f), Mathf.CeilToInt(camera.pixelHeight / 8.0f), 1);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}