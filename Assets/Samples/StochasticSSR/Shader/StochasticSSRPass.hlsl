#ifndef _STOCHASTIC_SSR_PASS_INCLUDED
#define _STOCHASTIC_SSR_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

float4 _ProjectionParams2;
float4 _CameraViewTopLeftCorner;
float4 _CameraViewXExtent;
float4 _CameraViewYExtent;

/*
 * x : MaxDistance
 * y : Stride
 * z : StepCount
 * w : Thickness
 */
float4 _SSRParams0;
float4 _SSRParams1;

#define MAX_DISTANCE _SSRParams0.x
#define STRIDE       _SSRParams0.y
#define STEP_COUNT   _SSRParams0.z
#define THICKNESS    _SSRParams0.w

float4 _SourceSize;
float4 _SSR_NoiseSize;

// GBuffer
TEXTURE2D_X_HALF(_GBuffer2);

// Blue Noise
TEXTURE2D(_SSR_Noise);

half4 GetSource(half2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

half3 ReconstructViewPos(float2 uv, float linearEyeDepth)
{
    // Screen is y-inverted  
    uv.y = 1.0 - uv.y;

    float zScale = linearEyeDepth * _ProjectionParams2.x; // divide by near plane  
    float3 viewPos = _CameraViewTopLeftCorner.xyz + _CameraViewXExtent.xyz * uv.x + _CameraViewYExtent.xyz * uv.y;
    viewPos *= zScale;
    return viewPos;
}

float4 TransformViewToHScreen(float3 vpos, float2 screenSize)
{
    float4 cpos = mul(UNITY_MATRIX_P, vpos);
    cpos.xy = float2(cpos.x, cpos.y * _ProjectionParams.x) * 0.5 + 0.5 * cpos.w; // range [0, w]
    cpos.xy *= screenSize;
    return cpos;
}

void RayCasting_Linear(Varyings input, out float4 rayCasting : SV_Target0, out half rayPDFs : SV_Target1)
{
    half2 UV = input.texcoord;

    float sceneDepth = SampleSceneDepth(UV);
    float linearDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);

    // only need roughness info
    half4 gBuffer2 = SAMPLE_TEXTURE2D_LOD(_GBuffer2, sampler_PointClamp, UV, 0);
    half smoothness = gBuffer2.a;
    half perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    half roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);

    float3 sceneNormal = SampleSceneNormals(UV);
    float3 viewNormal = TransformWorldToView(sceneNormal);

    float3 vpos = ReconstructViewPos(UV, linearDepth);
    float3 vDir = normalize(vpos);
    float3 rDir = TransformWorldToViewDir(normalize(reflect(vDir, sceneNormal)));
    /* 加上相机世界空间坐标后得到世界空间坐标 */
    float3 worldPos = _WorldSpaceCameraPos + vpos;
    float3 viewPos = TransformWorldToView(worldPos);
    float3 startView = viewPos;

    // Property
    half Ray_HitMask = 0.0, Ray_NumMarch = 0.0;
    half2 Ray_HitUV = 0.0;
    half3 Ray_HitPoint = 0.0;


    rayCasting = float4(0.0f, 0.0f, 0.0f, 0.0f);
    rayPDFs = 0.0h;
}


#endif
