#ifndef INSTANCE_DRAW_FORWARD_LIT_SHADOW_PASS_INCLUDED
#define INSTANCE_DRAW_FORWARD_LIT_SHADOW_PASS_INCLUDED

struct InstanceShadowAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    uint instanceID : SV_InstanceID;
};

StructuredBuffer<float4> _PositionBuffer;

float4 InstanceGetShadowPositionHClip(InstanceShadowAttributes input, float3 posOffset)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    positionWS = positionWS + posOffset;
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
    float3 lightDirectionWS = _LightDirection;
    #endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

Varyings InstanceShadowPassVertex(InstanceShadowAttributes input)
{
    Varyings output;

    float4 posOffset = _PositionBuffer[input.instanceID];

    input.positionOS.x = input.positionOS.x * posOffset.w;
    input.positionOS.y = input.positionOS.y * posOffset.w;
    input.positionOS.z = input.positionOS.z * posOffset.w;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionCS = InstanceGetShadowPositionHClip(input, posOffset.xyz);
    return output;
}

#endif
