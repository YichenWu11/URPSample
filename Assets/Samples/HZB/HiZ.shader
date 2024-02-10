Shader "Hidden/HiZ"
{
    SubShader
    {
        Pass
        {
            Name "CopyDepth"
            ZTest Always ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float frag(Varyings input) : SV_Target
            {
                return SampleSceneDepth(input.texcoord);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HiZ"
            ZTest Always ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            float frag(Varyings input) : SV_Target
            {
                float4 depthSample4 = GATHER_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                float2 depthSample2 = min(depthSample4.xy, depthSample4.zw);
                float depthSample = min(depthSample2.x, depthSample2.y);
                return depthSample;
            }   
            ENDHLSL
        }
    }
}
