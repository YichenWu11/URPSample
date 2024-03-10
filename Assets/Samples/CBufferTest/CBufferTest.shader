Shader "Unlit/CBufferTest"
{
    Properties
    {
        _Color("Color", Color) = (0.2, 0.2, 0.2)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            CBUFFER_START(MyCBuffer)
                half4 _Color;
            CBUFFER_END

            half4 frag (v2f i) : SV_Target
            {
                half4 color = half4(1.0h, 1.0h, 1.0h, 1.0h);
                return color * _Color;
            }
            ENDCG
        }
    }
}
