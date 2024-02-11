﻿#ifndef _SSR_PASS_INCLUDED
#define _SSR_PASS_INCLUDED  

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#include "Assets/Samples/HZB/DeclareHizBuffer.hlsl"

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

float4 _SSRBlurRadius;

float4 _SourceSize;

void swap(inout float v0, inout float v1)
{
    float temp = v0;
    v0 = v1;
    v1 = temp;
}

half4 GetSource(half2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

// 还原世界空间下，相对于相机的位置  
half3 ReconstructViewPos(float2 uv, float linearEyeDepth)
{
    // Screen is y-inverted  
    uv.y = 1.0 - uv.y;

    float zScale = linearEyeDepth * _ProjectionParams2.x; // divide by near plane  
    float3 viewPos = _CameraViewTopLeftCorner.xyz + _CameraViewXExtent.xyz * uv.x + _CameraViewYExtent.xyz * uv.y;
    viewPos *= zScale;
    return viewPos;
}

// 从视角坐标转裁剪屏幕ao坐标
float4 TransformViewToHScreen(float3 vpos, float2 screenSize)
{
    float4 cpos = mul(UNITY_MATRIX_P, vpos);
    cpos.xy = float2(cpos.x, cpos.y * _ProjectionParams.x) * 0.5 + 0.5 * cpos.w;
    cpos.xy *= screenSize;
    return cpos;
}

bool ScreenSpaceRayMarching(float3 startView, float3 rDir, inout float2 hitUV)
{
    float magnitude = _SSRParams0.x;
    float end = startView.z + rDir.z * magnitude;
    if (end > -_ProjectionParams.y)
        magnitude = (-_ProjectionParams.y - startView.z) / rDir.z;
    float3 endView = startView + rDir * magnitude;

    // 齐次屏幕空间坐标  
    float4 startHScreen = TransformViewToHScreen(startView, _SourceSize.xy);
    float4 endHScreen = TransformViewToHScreen(endView, _SourceSize.xy);

    // inverse w  
    float startK = 1.0 / startHScreen.w;
    float endK = 1.0 / endHScreen.w;

    //  结束屏幕空间坐标  
    float2 startScreen = startHScreen.xy * startK;
    float2 endScreen = endHScreen.xy * endK;

    // 经过齐次除法的视角坐标  
    float3 startQ = startView * startK;
    float3 endQ = endView * endK;

    // 根据斜率将dx=1 dy = delta  
    float2 diff = endScreen - startScreen;
    bool permute = false;
    if (abs(diff.x) < abs(diff.y))
    {
        permute = true;

        diff = diff.yx;
        startScreen = startScreen.yx;
        endScreen = endScreen.yx;
    }
    // 计算屏幕坐标、齐次视坐标、inverse-w的线性增量  
    float dir = sign(diff.x);
    float invdx = dir / diff.x;
    float2 dp = float2(dir, invdx * diff.y);
    float3 dq = (endQ - startQ) * invdx;
    float dk = (endK - startK) * invdx;

    dp *= _SSRParams0.y;
    dq *= _SSRParams0.y;
    dk *= _SSRParams0.y;

    // 缓存当前深度和位置  
    float rayZMin = startView.z;
    float rayZMax = startView.z;
    float preZ = startView.z;

    float2 P = startScreen;
    float3 Q = startQ;
    float K = startK;

    end = endScreen.x * dir;

    // 进行屏幕空间射线步近  
    UNITY_LOOP
    for (int i = 0; i < _SSRParams0.z && P.x * dir <= end; i++)
    {
        // 步近  
        P += dp;
        Q.z += dq.z;
        K += dk;
        // 得到步近前后两点的深度  
        rayZMin = preZ;
        rayZMax = (dq.z * 0.5 + Q.z) / (dk * 0.5 + K);
        preZ = rayZMax;
        if (rayZMin > rayZMax)
            swap(rayZMin, rayZMax);

        // 得到交点uv  
        hitUV = permute ? P.yx : P;
        hitUV *= _SourceSize.zw;
        if (any(hitUV < 0.0) || any(hitUV > 1.0))
            return false;
        float surfaceDepth = -LinearEyeDepth(SampleSceneDepth(hitUV), _ZBufferParams);
        bool isBehind = (rayZMin + 0.1 <= surfaceDepth); // 加一个bias 防止 _SSRParams0.y 过小，自反射  
        bool intersecting = isBehind && (rayZMax >= surfaceDepth - _SSRParams0.w);

        if (intersecting)
            return true;
    }

    return false;
}

bool HierarchicalZScreenSpaceRayMarching(float3 startView, float3 rDir, inout float2 hitUV)
{
    float magnitude = _SSRParams0.x;
    float end = startView.z + rDir.z * magnitude;
    if (end > -_ProjectionParams.y)
        magnitude = (-_ProjectionParams.y - startView.z) / rDir.z;
    float3 endView = startView + rDir * magnitude;

    // 齐次屏幕空间坐标  
    float4 startHScreen = TransformViewToHScreen(startView, _SourceSize.xy);
    float4 endHScreen = TransformViewToHScreen(endView, _SourceSize.xy);

    // inverse w  
    float startK = 1.0 / startHScreen.w;
    float endK = 1.0 / endHScreen.w;

    //  结束屏幕空间坐标  
    float2 startScreen = startHScreen.xy * startK;
    float2 endScreen = endHScreen.xy * endK;

    // 经过齐次除法的视角坐标  
    float3 startQ = startView * startK;
    float3 endQ = endView * endK;

    // 根据斜率将dx=1 dy = delta  
    float2 diff = endScreen - startScreen;
    bool permute = false;
    if (abs(diff.x) < abs(diff.y))
    {
        permute = true;

        diff = diff.yx;
        startScreen = startScreen.yx;
        endScreen = endScreen.yx;
    }
    // 计算屏幕坐标、齐次视坐标、inverse-w的线性增量  
    float dir = sign(diff.x);
    float invdx = dir / diff.x;
    float2 dp = float2(dir, invdx * diff.y);
    float3 dq = (endQ - startQ) * invdx;
    float dk = (endK - startK) * invdx;

    dp *= _SSRParams0.y;
    dq *= _SSRParams0.y;
    dk *= _SSRParams0.y;

    // 缓存当前深度和位置  
    float rayZMin = startView.z;
    float rayZMax = startView.z;
    float preZ = startView.z;

    float2 P = startScreen;
    float3 Q = startQ;
    float K = startK;

    float mipLevel = 0.0;

    end = endScreen.x * dir;

    // 进行屏幕空间射线步近
    UNITY_LOOP
    for (int i = 0; i < _SSRParams0.z; i++)
    {
        // 步近
        P += dp * exp2(mipLevel);
        Q += dq * exp2(mipLevel);
        K += dk * exp2(mipLevel);

        // 得到步近前后两点的深度
        rayZMin = preZ;
        rayZMax = (dq.z * exp2(mipLevel) * 0.5 + Q.z) / (dk * exp2(mipLevel) * 0.5 + K);
        preZ = rayZMax;
        if (rayZMin > rayZMax)
            swap(rayZMin, rayZMax);

        // 得到交点uv
        hitUV = permute ? P.yx : P;
        hitUV *= _SourceSize.zw;

        if (any(hitUV < 0.0) || any(hitUV > 1.0))
            return false;

        float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_HiZBuffer, sampler_HiZBuffer, hitUV,
                                                mipLevel);
        float surfaceDepth = -LinearEyeDepth(rawDepth, _ZBufferParams);

        bool behind = rayZMin + 0.1 <= surfaceDepth;

        if (!behind)
        {
            mipLevel = min(mipLevel + 1, _MaxHiZMipLevel);
        }
        else
        {
            if (mipLevel == 0)
            {
                if (abs(surfaceDepth - rayZMax) < _SSRParams0.w)
                    return true;
            }
            else
            {
                P -= dp * exp2(mipLevel);
                Q -= dq * exp2(mipLevel);
                K -= dk * exp2(mipLevel);
                preZ = Q.z / K;

                mipLevel--;
            }
        }
    }

    return false;
}

half4 SSRPassFragment(Varyings input) : SV_Target
{
    float rawDepth = SampleSceneDepth(input.texcoord);
    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

    float3 vpos = ReconstructViewPos(input.texcoord, linearDepth);
    float3 normal = SampleSceneNormals(input.texcoord);

    float3 vDir = normalize(vpos);
    float3 rDir = TransformWorldToViewDir(normalize(reflect(vDir, normal)));

    /* 加上相机世界空间坐标后得到世界空间坐标 */
    vpos = _WorldSpaceCameraPos + vpos;

    float3 startView = TransformWorldToView(vpos);
    float2 hitUV;

    // if (HierarchicalZScreenSpaceRayMarching(startView, rDir, hitUV))
    if (ScreenSpaceRayMarching(startView, rDir, hitUV))
    {
        return GetSource(hitUV) + GetSource(input.texcoord);
    }

    return GetSource(input.texcoord);
}

// 叠加
half4 SSRFinalPassFragment(Varyings input) : SV_Target
{
    return half4(GetSource(input.texcoord).rgb, 1.0);
}

#endif
