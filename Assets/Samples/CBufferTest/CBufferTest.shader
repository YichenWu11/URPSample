Shader "Unlit/CBufferTest"
{
    Properties
    {
        _Color("Color", Color) = (0.2, 0.2, 0.2)
        _TintColor("TintColor", Color) = (0.2, 0.2, 0.2)
        _Intensity("Intensity", Color) = (1.0, 1.0, 1.0)
        _AIntensity("AIntensity", Range(0.0, 1.0)) = 1.0
        _BIntensity("BIntensity", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position     : POSITION;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                output.uv = input.texcoord;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            CBUFFER_START(MyCBuffer)
                half3 _Color;
                half3 _Intensity;
                float _AIntensity;
                float _BIntensity;
            CBUFFER_END

            CBUFFER_START(MyCBuffer1)
                half4 _TintColor;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = half4(1.0h, 1.0h, 1.0h, 1.0h);
                half4 _ColorH4 = half4(_Color, 1.0h);
                return color * _ColorH4 * _TintColor * _Intensity.x * _AIntensity * _BIntensity;
            }
            ENDHLSL
        }
    }
}
