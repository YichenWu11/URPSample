using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StochasticSSR : ScriptableRendererFeature
{
    [Serializable]
    internal class StochasticSSRSettings
    {
        [SerializeField] [Range(0.0f, 1.0f)] internal float Intensity = 0.8f;
        [SerializeField] internal float MaxDistance = 10.0f;
        [SerializeField] internal int Stride = 30;
        [SerializeField] internal int StepCount = 12;
        [SerializeField] internal float Thickness = 0.5f;
        [SerializeField] internal bool JitterDither = true;
        [SerializeField] internal Texture2D BlueNoise_LUT = null;
    }

    [SerializeField] private StochasticSSRSettings settings = new StochasticSSRSettings();

    private CustomRenderPass m_renderPass;
    private Shader m_shader;
    private Material m_material;

    private const string k_shaderName = "Hidden/StochasticSSR";

    public override void Create()
    {
        if (m_renderPass == null)
        {
            m_renderPass = new CustomRenderPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1
            };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.postProcessEnabled)
        {
            if (!LoadMaterial())
            {
                Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                    GetType().Name, name);
                return;
            }

            // use Deferred Rendering Path !!!
            bool shouldAdd = m_renderPass.Setup(ref settings, ref m_material);

            if (shouldAdd)
                renderer.EnqueuePass(m_renderPass);
        }
    }

    bool LoadMaterial()
    {
        if (m_shader == null)
            m_shader = Shader.Find(k_shaderName);
        if (m_material == null)
            m_material = CoreUtils.CreateEngineMaterial(m_shader);
        return m_material != null;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_material);

        m_renderPass?.Dispose();
        m_renderPass = null;
    }

    private class CustomRenderPass : ScriptableRenderPass
    {
        internal enum ShaderPass
        {
            RayCasting = 0,
            Resolve,
            Temporal,
            Combine
        }

        private StochasticSSRSettings m_settings = new StochasticSSRSettings();

        private Material m_stochasticSSRMaterial;
        private ProfilingSampler m_profilingSampler = new ProfilingSampler("StochasticSSR");

        private RTHandle m_sourceTexture;
        private RTHandle m_destinationTexture;

        private static readonly int s_projectionParams2ID = Shader.PropertyToID("_ProjectionParams2"),
            s_cameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner"),
            s_cameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent"),
            s_cameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent"),
            s_sourceSizeID = Shader.PropertyToID("_SourceSize"),
            s_SSRParams0ID = Shader.PropertyToID("_SSRParams0"),
            s_SSRParams1ID = Shader.PropertyToID("_SSRParams1"),
            s_BlueNoiseID = Shader.PropertyToID("_SSR_Noise"),
            s_BlueNoiseSizeID = Shader.PropertyToID("_SSR_NoiseSize");

        private const string k_jitterKeyword = "_JITTER_ON";

        internal bool Setup(ref StochasticSSRSettings featureSettings, ref Material material)
        {
            m_stochasticSSRMaterial = material;
            m_settings = featureSettings;

            ConfigureInput(ScriptableRenderPassInput.Depth |
                           ScriptableRenderPassInput.Normal |
                           ScriptableRenderPassInput.Motion);

            return m_stochasticSSRMaterial != null;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;

            Matrix4x4 view = renderingData.cameraData.GetViewMatrix();
            Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix();
            Matrix4x4 vp = proj * view;

            Matrix4x4 cview = view;
            cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            Matrix4x4 cviewProj = proj * cview;

            Matrix4x4 cviewProjInv = cviewProj.inverse;

            var near = renderingData.cameraData.camera.nearClipPlane;
            Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, 1.0f, -1.0f, 1.0f));
            Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1.0f, 1.0f, -1.0f, 1.0f));
            Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f));

            Vector4 cameraXExtent = topRightCorner - topLeftCorner;
            Vector4 cameraYExtent = bottomLeftCorner - topLeftCorner;

            near = renderingData.cameraData.camera.nearClipPlane;

            // ReconstructViewPos 参数
            m_stochasticSSRMaterial.SetVector(s_cameraViewTopLeftCornerID, topLeftCorner);
            m_stochasticSSRMaterial.SetVector(s_cameraViewXExtentID, cameraXExtent);
            m_stochasticSSRMaterial.SetVector(s_cameraViewYExtentID, cameraYExtent);
            m_stochasticSSRMaterial.SetVector(s_projectionParams2ID,
                new Vector4(1.0f / near, renderingData.cameraData.worldSpaceCameraPos.x,
                    renderingData.cameraData.worldSpaceCameraPos.y,
                    renderingData.cameraData.worldSpaceCameraPos.z));

            if (m_settings.BlueNoise_LUT != null)
            {
                m_stochasticSSRMaterial.SetTexture(s_BlueNoiseID, m_settings.BlueNoise_LUT);
                m_stochasticSSRMaterial.SetVector(s_BlueNoiseSizeID, new Vector4(1024, 1024, 0, 0));
            }

            // set SourceSize !!!

            ConfigureTarget(renderer.cameraColorTargetHandle);
            ConfigureClear(ClearFlag.None, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
        }
    }
}