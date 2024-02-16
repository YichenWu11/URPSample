Shader "Hidden/StochasticSSR"
{
    HLSLINCLUDE
        #include "StochasticSSRPass.hlsl"
    ENDHLSL
    
    SubShader
    {
        Pass
        {
            Name "RayCasting"
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment RayCasting_Linear
            
            ENDHLSL
        }
        
        Pass
        {
            Name "Resolve"
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment RayCasting_Linear
            
            ENDHLSL
        }
        
        Pass
        {
            Name "Temporal"
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment RayCasting_Linear
            
            ENDHLSL
        }
        
        Pass
        {
            Name "Combine"
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment RayCasting_Linear
            
            ENDHLSL
        }
    }
}
