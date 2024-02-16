Shader "Unlit/MatCap"
{
    Properties
    {
        _MatCap ("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
//        [ToggleOff] _IterationSample("Use Interation Sample", Float) = 0.0
        [Toggle] _IterationSample("Use Interation Sample", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ITERATIONSAMPLE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 MainUVAndMatCapUV : TEXCOORD0;
                float3 viewNormal : TEXCOORD1;
                float3 positionVS : TEXCOORD2;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D( _MatCap);
            SAMPLER(sampler_MatCap);
            
            float4 _BaseColor;

            Varyings vert (Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
                output.positionCS = vertexInput.positionCS;
                output.positionVS = vertexInput.positionVS;
                output.MainUVAndMatCapUV.xy = input.texcoord;
                float3 viewNormal = TransformObjectToWorldDir(TransformWorldToViewDir(input.normalOS));
                output.MainUVAndMatCapUV.zw = viewNormal * 0.5f + 0.5f;
                output.viewNormal = viewNormal;
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.positionVS);
                float3 reflectDir = reflect(viewDir, input.viewNormal);
                float3 helper = float3(reflectDir.x, reflectDir.y, reflectDir.z + 1.0f);
                float res0 = sqrt(dot(helper, helper));
                float3 sampleNormal =  helper * rcp(res0);
                sampleNormal = sampleNormal * 0.5f + 0.5f;
                
                float2 reflectUV = reflectDir * 0.5f + 0.5f;

                #if _ITERATIONSAMPLE_ON
                float2 matCapUV = sampleNormal;
                #else
                float2 matCapUV = input.MainUVAndMatCapUV.zw;
                #endif
                half3 matCapColor = SAMPLE_TEXTURE2D(_MatCap, sampler_MatCap, matCapUV).rgb;
                half4 color = half4(matCapColor.rgb, _BaseColor.a);
                half4 debugColor = half4(sampleNormal.xy, 0.0h, 1.0h);
                
                return color;
            }
            
            ENDHLSL
        }
    }
}
