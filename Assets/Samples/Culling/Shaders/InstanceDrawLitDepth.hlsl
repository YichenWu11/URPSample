#ifndef INSTANCE_DRAW_FORWARD_LIT_DEPTH_PASS_INCLUDED
#define INSTANCE_DRAW_FORWARD_LIT_DEPTH_PASS_INCLUDED

struct InstanceDepthAttributes
{
    float4 position : POSITION;
    float2 texcoord : TEXCOORD0;
    uint instanceID : SV_InstanceID;
};

StructuredBuffer<float4> _PositionBuffer;

Varyings InstanceDepthOnlyVertex(InstanceDepthAttributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    float4 posOffset = _PositionBuffer[input.instanceID];
    input.position.x = input.position.x * posOffset.w;
    input.position.y = input.position.y * posOffset.w;
    input.position.z = input.position.z * posOffset.w;
    float3 positionWS = TransformObjectToWorld(input.position.xyz);
    positionWS = positionWS + posOffset;

    output.positionCS = TransformWorldToHClip(positionWS);
    return output;
}

#endif
