#ifndef _SSPRREFLECTOR_PASS_INCLUDED
#define _SSPRREFLECTOR_PASS_INCLUDED

struct SSPRAttributes
{
    float4 positionOS : POSITION;
};

struct SSPRVaryings
{
    float4 positionCS : SV_POSITION;
    float4 positionNDC : TEXCOORD0;
};

TEXTURE2D(_SSPRReflectionTexture);
SAMPLER(sampler_SSPRReflectionTexture);

SSPRVaryings SSPRReflectorPassVertex(SSPRAttributes input)
{
    SSPRVaryings output;

    VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInputs.positionCS;
    output.positionNDC = vertexInputs.positionNDC;

    return output;
}

half4 SSPRReflectorPassFragment(SSPRVaryings input) : SV_Target
{
    float2 screenspace_uv = input.positionNDC.xy / input.positionNDC.w;
    half3 finalCol = SAMPLE_TEXTURE2D(_SSPRReflectionTexture, sampler_SSPRReflectionTexture, screenspace_uv);

    return half4(finalCol, 1.0);
}

#endif
