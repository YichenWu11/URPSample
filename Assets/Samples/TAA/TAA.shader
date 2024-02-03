Shader "Hidden/AA/TAA"
{
    Properties
    {
    }
    SubShader
    {
        ZTest Always ZWrite Off Cull Off
        
        Pass
        {
            Name "TAAPass"
            HLSLPROGRAM

            #include "TAAPass.hlsl"
            
            #pragma vertex Vert
            #pragma fragment TAAPassFragment
            
            ENDHLSL
        }
    }
}
