using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
public class HZBSettings
{
}

public class HZBGenerator : ScriptableRendererFeature
{
    public HZBSettings m_settings = new HZBSettings();

    private HZBPass m_hzbPass;

    private const string k_shaderName = "Hidden/HiZ";
    private Shader m_hzbShader;
    private Material m_hzbMaterial;

    private static readonly int s_depthTex = Shader.PropertyToID("_DepthTex");

    public override void Create()
    {
        if (m_hzbPass == null)
        {
            m_hzbPass = new HZBPass();
            m_hzbPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!LoadMaterial())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                GetType().Name, name);
            return;
        }

        bool shouldAdd = m_hzbPass.Setup(ref m_settings, ref m_hzbMaterial);

        if (shouldAdd)
            renderer.EnqueuePass(m_hzbPass);
    }

    private bool LoadMaterial()
    {
        if (m_hzbShader == null)
            m_hzbShader = Shader.Find(k_shaderName);
        if (m_hzbMaterial == null)
            m_hzbMaterial = CoreUtils.CreateEngineMaterial(m_hzbShader);
        return m_hzbMaterial != null;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_hzbMaterial);
        m_hzbPass?.Dispose();
        m_hzbPass = null;
    }

    class HZBPass : ScriptableRenderPass
    {
        private Material m_hzbMaterial;
        private HZBSettings m_hzbSettings = new HZBSettings();

        private Vector2Int m_hzbSize = new Vector2Int(-1, -1);
        private Vector2Int m_screenSize = new Vector2Int(-1, -1);
        private int m_maxMips;

        private ProfilingSampler m_profilingSampler = new ProfilingSampler("HiZPass");

        private const string k_hizBufferName = "_HizBuffer";
        private RenderTextureDescriptor m_hizBufferDesc;
        private RTHandle m_hizBuffer;

        private const string k_hizBufferIntermediatesNamePrefix = "_HizBufferIntermediates";
        private RTHandle[] m_hiZBufferIntermediates;
        private RenderTextureDescriptor[] m_hiZBufferIntermediatesDesc;

        public bool Setup(ref HZBSettings settings, ref Material material)
        {
            m_hzbSettings = settings;
            m_hzbMaterial = material;

            ConfigureInput(ScriptableRenderPassInput.Depth);

            return true;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var width = renderingData.cameraData.camera.pixelWidth;
            var height = renderingData.cameraData.camera.pixelHeight;
            var currSize = new Vector2Int(width, height);

            if (m_screenSize == currSize)
            {
                ConfigureTarget(m_hizBuffer);
                ConfigureClear(ClearFlag.None, Color.clear);
                return;
            }

            m_screenSize = currSize;
            var currHZBSize = new Vector2Int(
                Mathf.NextPowerOfTwo(currSize.x) / 2,
                Mathf.NextPowerOfTwo(currSize.y) / 2);

            // no need to re-alloc hizBuffer
            if (m_hzbSize == currHZBSize)
            {
                ConfigureTarget(m_hizBuffer);
                ConfigureClear(ClearFlag.None, Color.clear);
                return;
            }

            m_hzbSize = currHZBSize;
            var max = Mathf.Max(m_hzbSize.x, m_hzbSize.y);
            m_maxMips = 0;
            while (max > 2)
            {
                max /= 2;
                m_maxMips++;
            }

            m_hizBufferDesc = renderingData.cameraData.cameraTargetDescriptor;
            m_hizBufferDesc.width = m_hzbSize.x;
            m_hizBufferDesc.height = m_hzbSize.y;
            m_hizBufferDesc.useMipMap = true;
            m_hizBufferDesc.autoGenerateMips = false;
            m_hizBufferDesc.msaaSamples = 1;
            m_hizBufferDesc.depthBufferBits = 0;
            m_hizBufferDesc.graphicsFormat = GraphicsFormat.R16_UNorm;

            RenderingUtils.ReAllocateIfNeeded(ref m_hizBuffer, m_hizBufferDesc, FilterMode.Point,
                TextureWrapMode.Clamp, name: k_hizBufferName);

            if (m_hiZBufferIntermediates != null)
            {
                foreach (var handle in m_hiZBufferIntermediates)
                {
                    if (handle != null)
                    {
                        RTHandles.Release(handle);
                    }
                }
            }

            m_hiZBufferIntermediates = new RTHandle[m_maxMips];
            m_hiZBufferIntermediatesDesc = new RenderTextureDescriptor[m_maxMips];
            for (int i = 0, w = m_hzbSize.x, h = m_hzbSize.y; i < m_maxMips; i++)
            {
                w = Mathf.Max(w / 2, 1);
                h = Mathf.Max(h / 2, 1);
                m_hiZBufferIntermediatesDesc[i] = m_hizBufferDesc;
                m_hiZBufferIntermediatesDesc[i].useMipMap = false;
                m_hiZBufferIntermediatesDesc[i].width = w;
                m_hiZBufferIntermediatesDesc[i].height = h;
                RenderingUtils.ReAllocateIfNeeded(
                    ref m_hiZBufferIntermediates[i],
                    m_hiZBufferIntermediatesDesc[i],
                    FilterMode.Point,
                    TextureWrapMode.Clamp,
                    name: $"{k_hizBufferIntermediatesNamePrefix}(mip{i + 1})");
            }

            ConfigureTarget(m_hizBuffer);
            ConfigureClear(ClearFlag.None, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_hzbMaterial == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. HZB pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }

            var cmd = CommandBufferPool.Get();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            using (new ProfilingScope(cmd, m_profilingSampler))
            {
                // Copy Depth First
                Blitter.BlitCameraTexture(cmd,
                    renderingData.cameraData.renderer.cameraDepthTargetHandle, m_hizBuffer, m_hzbMaterial, 0);


                // Generate Hi-Z Intermediates And Copy To Hi-Z Buffer
                for (int i = 0; i < m_maxMips; i++)
                {
                    RTHandle dst = m_hiZBufferIntermediates[i];
                    Blitter.BlitCameraTexture(cmd,
                        i == 0 ? m_hizBuffer : m_hiZBufferIntermediates[i - 1],
                        m_hiZBufferIntermediates[i], m_hzbMaterial, 1);
                    cmd.CopyTexture(dst, 0, 0, m_hizBuffer, 0, i + 1);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public void Dispose()
        {
            RTHandles.Release(m_hizBuffer);
            m_hizBuffer = null;

            if (m_hiZBufferIntermediates != null)
            {
                foreach (var handle in m_hiZBufferIntermediates)
                {
                    if (handle != null)
                    {
                        RTHandles.Release(handle);
                    }
                }
            }
        }
    }
}