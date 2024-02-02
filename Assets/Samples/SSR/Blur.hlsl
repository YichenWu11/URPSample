#ifndef _BLUR_PASS_INCLUDED
#define _BLUR_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

float4 _SSRBlurRadius;

half4 GetSource(half2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

half4 BlurPassFragment(Varyings input) : SV_Target
{
    float4 sum = float4(0, 0, 0, 0);
    float2 size = 0.5f / _ScreenParams.xy;

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

#endif
