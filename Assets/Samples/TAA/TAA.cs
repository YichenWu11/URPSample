using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TAA
{
    [Serializable]
    internal class TAASettings
    {
        // 填当前feature的参数
        [SerializeField] internal float JitterScale = 1.0f;
    }

    [DisallowMultipleRendererFeature("TAA")]
    public class TAA : ScriptableRendererFeature
    {
        [SerializeField] private TAASettings mSettings = new TAASettings();


        private Shader mShader;
        private const string mShaderName = "Hidden/AA/TAA";

        private TAAPass mTaaPass;
        private JitterPass mJitterPass;
        private Material mMaterial;

        public override void Create()
        {
            if (mJitterPass == null)
            {
                mJitterPass = new JitterPass();
                // 在渲染场景前抖动相机
                mJitterPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            }

            if (mTaaPass == null)
            {
                mTaaPass = new TAAPass();
                // 修改注入点 这里先不考虑tonemapping和bloom等
                mTaaPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 如果使用第二个分支 则SceneView和Game相机同时出现时，会有点bug（一直重新分配desc），所以非测试时可以用第一个分支
            // if (renderingData.cameraData.camera.cameraType == CameraType.Game) {
            if (renderingData.cameraData.postProcessEnabled)
            {
                if (!GetMaterials())
                {
                    Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                        GetType().Name, name);
                    return;
                }

                bool shouldAdd = mJitterPass.Setup(ref mSettings) && mTaaPass.Setup(ref mSettings, ref mMaterial);

                if (shouldAdd)
                {
                    renderer.EnqueuePass(mJitterPass);
                    renderer.EnqueuePass(mTaaPass);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(mMaterial);

            mJitterPass?.Dispose();

            mTaaPass?.Dispose();
            mTaaPass = null;
        }

        private bool GetMaterials()
        {
            if (mShader == null)
                mShader = Shader.Find(mShaderName);
            if (mMaterial == null && mShader != null)
                mMaterial = CoreUtils.CreateEngineMaterial(mShader);
            return mMaterial != null;
        }

        class JitterPass : ScriptableRenderPass
        {
            private TAASettings mSettings;

            private ProfilingSampler mProfilingSampler = new ProfilingSampler("Jitter");

            internal JitterPass()
            {
                mSettings = new TAASettings();
            }

            internal bool Setup(ref TAASettings featureSettings)
            {
                mSettings = featureSettings;

                return true;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                using (new ProfilingScope(cmd, mProfilingSampler))
                {
                    cmd.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(),
                        Jitter.CalculateJitterProjectionMatrix(ref renderingData.cameraData, mSettings.JitterScale));
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
            }
        }

        class TAAPass : ScriptableRenderPass
        {
            private TAASettings mSettings;

            private Material mMaterial;

            private ProfilingSampler mProfilingSampler = new ProfilingSampler("TAA");
            private RenderTextureDescriptor mTAADescriptor;

            private RTHandle mSourceTexture;
            private RTHandle mDestinationTexture;

            private static readonly int mTaaAccumulationTexID = Shader.PropertyToID("_TaaAccumulationTexture"),
                mPrevViewProjMatrixID = Shader.PropertyToID("_LastViewProjMatrix"),
                mViewProjMatrixWithoutJitterID = Shader.PropertyToID("_ViewProjMatrixWithoutJitter");

            private Matrix4x4 mPrevViewProjMatrix, mViewProjMatrix;

            private RTHandle mTAATexture0, mTAATexture1;

            private const string mAccumulationTextureName = "_TaaAccumulationTexture",
                mTaaTemporaryTextureName = "_TaaTemporaryTexture";

            private RTHandle mAccumulationTexture;

            private RTHandle mTaaTemporaryTexture;

            private bool mResetHistoryFrames;

            private const string mTAATexture0Name = "_TAATexture0",
                mTAATexture1Name = "_TAATexture1";


            internal TAAPass()
            {
                mSettings = new TAASettings();
            }

            internal bool Setup(ref TAASettings featureSettings, ref Material material)
            {
                mMaterial = material;
                mSettings = featureSettings;

                ConfigureInput(ScriptableRenderPassInput.Normal);

                return mMaterial != null;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                mTAADescriptor = renderingData.cameraData.cameraTargetDescriptor;
                mTAADescriptor.msaaSamples = 1;
                mTAADescriptor.depthBufferBits = 0;

                // 设置Material属性
                mMaterial.SetVector("_SourceSize",
                    new Vector4(mTAADescriptor.width, mTAADescriptor.height, 1.0f / mTAADescriptor.width,
                        1.0f / mTAADescriptor.height));

                // 分配RTHandle
                mResetHistoryFrames = RenderingUtils.ReAllocateIfNeeded(ref mAccumulationTexture, mTAADescriptor,
                    FilterMode.Bilinear, TextureWrapMode.Clamp, name: mAccumulationTextureName);
                if (mResetHistoryFrames)
                {
                    // 初始化上一帧的vp矩阵
                    mPrevViewProjMatrix = renderingData.cameraData.GetProjectionMatrix() *
                                          renderingData.cameraData.GetViewMatrix();
                }
                else
                {
                    mPrevViewProjMatrix = mViewProjMatrix;
                }

                RenderingUtils.ReAllocateIfNeeded(ref mTaaTemporaryTexture, mTAADescriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: mTaaTemporaryTextureName);

                // 配置目标和清除
                var renderer = renderingData.cameraData.renderer;
                ConfigureTarget(renderer.cameraColorTargetHandle);
                ConfigureClear(ClearFlag.None, Color.white);

                mViewProjMatrix = renderingData.cameraData.GetProjectionMatrix() *
                                  renderingData.cameraData.GetViewMatrix();
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (mMaterial == null)
                {
                    Debug.LogErrorFormat(
                        "{0}.Execute(): Missing material. ScreenSpaceAmbientOcclusion pass will not execute. Check for missing reference in the renderer resources.",
                        GetType().Name);
                    return;
                }

                var cmd = CommandBufferPool.Get();
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                mSourceTexture = renderingData.cameraData.renderer.cameraColorTargetHandle;
                mDestinationTexture = renderingData.cameraData.renderer.cameraColorTargetHandle;

                using (new ProfilingScope(cmd, mProfilingSampler))
                {
                    cmd.SetGlobalTexture(mTaaAccumulationTexID, mAccumulationTexture);
                    cmd.SetGlobalFloat("_FrameInfluence", mResetHistoryFrames ? 1.0f : 0.05f);
                    cmd.SetGlobalMatrix(mPrevViewProjMatrixID, mPrevViewProjMatrix); // 上一帧没有jitter的vp矩阵
                    cmd.SetGlobalMatrix(mViewProjMatrixWithoutJitterID, mViewProjMatrix); // 这一帧没有Jitter的vp矩阵

                    // TAA
                    Blitter.BlitCameraTexture(cmd, mSourceTexture, mTaaTemporaryTexture, mMaterial, 0);

                    // Copy History
                    Blitter.BlitCameraTexture(cmd, mTaaTemporaryTexture, mAccumulationTexture);

                    // FinalPass
                    Blitter.BlitCameraTexture(cmd, mTaaTemporaryTexture, mDestinationTexture);

                    mResetHistoryFrames = false;
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                mSourceTexture = null;
                mDestinationTexture = null;
            }

            public void Dispose()
            {
                // 释放RTHandle
                mAccumulationTexture?.Release();
                mAccumulationTexture = null;

                mTaaTemporaryTexture?.Release();
                mTaaTemporaryTexture = null;
            }
        }
    }
}