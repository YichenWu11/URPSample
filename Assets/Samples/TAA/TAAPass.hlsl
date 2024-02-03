﻿#ifndef _TAA_PASS_INCLUDED
#define _TAA_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

TEXTURE2D(_TaaAccumulationTexture);
SAMPLER(sampler_TaaAccumulationTexture);

half4 GetSource(half2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, 0);
}

half4 GetAccumulation(half2 uv)
{
    return SAMPLE_TEXTURE2D(_TaaAccumulationTexture, sampler_LinearClamp, uv);
}

static const float2 kOffsets3x3[9] =
{
    float2(-1.0f, -1.0f),
    float2(-1.0f, 0.0f),
    float2(-1.0f, 1.0f),
    float2(0.0f, -1.0f),
    float2(0.0f, 0.0f),
    float2(0.0f, 1.0f),
    float2(1.0f, -1.0f),
    float2(1.0f, 0.0f),
    float2(1.0f, 1.0f),
};

float4 _SourceSize;
float _FrameInfluence;

float4x4 _ViewProjMatrixWithoutJitter;
float4x4 _LastViewProjMatrix;

float2 ComputeVelocity(float2 uv)
{
    float depth = SampleSceneDepth(uv).x;

    #if !UNITY_REVERSED_Z
    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv).x);  
    #endif

    // 还原世界坐标  
    float3 posWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

    // 还原本帧和上帧没有Jitter的裁剪坐标  
    float4 posCS = mul(_ViewProjMatrixWithoutJitter, float4(posWS.xyz, 1.0));
    float4 prevPosCS = mul(_LastViewProjMatrix, float4(posWS.xyz, 1.0));

    // 计算出本帧和上帧没有Jitter的NDC坐标 [-1, 1]
    float2 posNDC = posCS.xy * rcp(posCS.w);
    float2 prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

    // 计算NDC位置差  
    float2 velocity = posNDC - prevPosNDC;
    #if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
    #endif

    // 将速度从[-1, 1]映射到[0, 1]  
    // ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)    velocity.xy *= 0.5;  

    return velocity;
}

// 取得采样点附近距离相机最近的点偏移
float2 AdjustBestDepthOffset(float2 uv)
{
    half bestDepth = 1.0f;
    float2 uvOffset = 0.0f;

    UNITY_UNROLL
    for (int k = 0; k < 9; k++)
    {
        half depth = SampleSceneDepth(uv + kOffsets3x3[k] * _SourceSize.zw);
        #if UNITY_REVERSED_Z
        depth = 1.0 - depth;
        #endif

        if (depth < bestDepth)
        {
            bestDepth = depth;
            uvOffset = kOffsets3x3[k] * _SourceSize.zw;
        }
    }
    return uvOffset;
}

// 取得在YCoCg色彩空间下，Clip的范围
void AdjustColorBox(float2 uv, inout half3 boxMin, inout half3 boxMax)
{
    boxMin = 1.0;
    boxMax = 0.0;

    UNITY_UNROLL
    for (int k = 0; k < 9; k++)
    {
        float3 C = RGBToYCoCg(GetSource(uv + kOffsets3x3[k] * _SourceSize.zw));
        boxMin = min(boxMin, C);
        boxMax = max(boxMax, C);
    }
}

// 将accumulationTexture进行clip，进一步减少ghosting
// https://zhuanlan.zhihu.com/p/425233743
float3 ClipToAABBCenter(half3 accum, half3 boxMin, half3 boxMax)
{
    accum = RGBToYCoCg(accum);
    float3 filtered = (boxMin + boxMax) * 0.5f;
    float3 ori = accum;
    float3 dir = filtered - accum;
    dir = abs(dir) < (1.0 / 65536.0) ? (1.0 / 65536.0) : dir;
    float3 invDir = rcp(dir);

    // 获取与box相交的位置
    float3 minIntersect = (boxMin - ori) * invDir;
    float3 maxIntersect = (boxMax - ori) * invDir;
    float3 enterIntersect = min(minIntersect, maxIntersect);
    float clipBlend = max(enterIntersect.x, max(enterIntersect.y, enterIntersect.z));
    clipBlend = saturate(clipBlend);

    // 取得与box的相交点
    float3 intersectionYCoCg = lerp(accum, filtered, clipBlend);
    // 还原到rgb空间，得到最终结果
    return YCoCgToRGB(intersectionYCoCg);
}

half4 TAAPassFragment(Varyings input) : SV_Target
{
    // 计算出上一帧的位置
    float2 depthOffsetUV = AdjustBestDepthOffset(input.texcoord);
    float2 velocity = ComputeVelocity(input.texcoord + depthOffsetUV);
    float2 historyUV = input.texcoord - velocity;

    // 采样上一帧和这一帧
    float4 accum = GetAccumulation(historyUV);
    float4 source = GetSource(input.texcoord);

    // 得到这一帧的颜色范围，防止ghosting
    half3 boxMin, boxMax;
    AdjustColorBox(input.texcoord, boxMin, boxMax);

    // clip current frame color
    accum.rgb = ClipToAABBCenter(accum, boxMin, boxMax);

    // 与上帧相比移动距离越远，就越倾向于使用当前的像素的值
    float frameInfluence = saturate(_FrameInfluence + length(velocity) * 100);

    // lerp
    return accum * (1.0 - frameInfluence) + source * frameInfluence;
}

#endif
