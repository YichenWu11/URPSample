#ifndef _BLUR_PASS_INCLUDED
#define _BLUR_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

float4 _SSRBlurRadius;

half4 GetSource(half2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

half4 BlurPassFragment(Varyings input) : SV_Target
{
    float4 sum = float4(0, 0, 0, 0);
    float2 size = 1.0f / _ScreenParams.xy;

    sum += GetSource(input.texcoord - 4 * size) * 0.05;
    sum += GetSource(input.texcoord - 3 * size) * 0.09;
    sum += GetSource(input.texcoord - 2 * size) * 0.12;
    sum += GetSource(input.texcoord - 1 * size) * 0.15;
    sum += GetSource(input.texcoord) * 0.16;
    sum += GetSource(input.texcoord + 1 * size) * 0.15;
    sum += GetSource(input.texcoord + 2 * size) * 0.12;
    sum += GetSource(input.texcoord + 3 * size) * 0.09;
    sum += GetSource(input.texcoord + 4 * size) * 0.05;

    sum.rgb = sum.rgb * 0.6f;

    return sum;
}

half4 SSRGaussianBlur(half2 uv, half2 pixelOffset)
{
    half4 colOut = half4(0.0h, 0.0h, 0.0h, 0.0h);

    // Kernel width 7 x 7
    const int stepCount = 2;

    const half gWeights[stepCount] = {
        0.44908,
        0.05092
    };
    const half gOffsets[stepCount] = {
        0.53805,
        2.06278
    };

    UNITY_UNROLL
    for (int i = 0; i < stepCount; i++)
    {
        half2 texCoordOffset = gOffsets[i] * pixelOffset;
        half4 p1 = SAMPLE_BASEMAP(uv + texCoordOffset);
        half4 p2 = SAMPLE_BASEMAP(uv - texCoordOffset);
        half4 col = p1 + p2;
        colOut += gWeights[i] * col;
    }

    return colOut;
}

half4 SSRHorizontalGaussianBlur(Varyings input) : SV_Target
{
    half2 uv = input.texcoord;
    half2 delta = half2(_SourceSize.z * _SSRBlurRadius.x, HALF_ZERO);

    return SSRGaussianBlur(uv, delta);
}

half4 SSRVerticalGaussianBlur(Varyings input) : SV_Target
{
    half2 uv = input.texcoord;
    half2 delta = half2(HALF_ZERO, _SourceSize.w * _SSRBlurRadius.x);

    return SSRGaussianBlur(uv, delta);
}

#endif
