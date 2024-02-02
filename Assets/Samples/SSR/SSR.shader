Shader "Hidden/SSR"
{
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            Name "RayMarching"
            HLSLPROGRAM

            #include "SSRPass.hlsl"
            
            #pragma vertex Vert
            #pragma fragment SSRPassFragment
            
            ENDHLSL
        }
        
        Pass
        {
            Name "Blur"
            HLSLPROGRAM

            #include "Blur.hlsl"
            
            #pragma vertex Vert
            #pragma fragment BlurPassFragment
            
            ENDHLSL
        }

        Pass {
            Name "SSR Addtive Pass"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One One, One Zero

            HLSLPROGRAM

            #include "SSRPass.hlsl"
            #pragma vertex Vert
            #pragma fragment SSRFinalPassFragment
            
            ENDHLSL
        }

        Pass {
            Name "SSR Balance Pass"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend SrcColor OneMinusSrcColor, One Zero

            HLSLPROGRAM

            #include "SSRPass.hlsl"
            #pragma vertex Vert
            #pragma fragment SSRFinalPassFragment
            
            ENDHLSL
        }
    }
}
