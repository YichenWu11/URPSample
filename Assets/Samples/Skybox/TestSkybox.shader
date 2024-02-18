Shader "Unlit/TestSkybox"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "PreviewType"="Sphere" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // return half4(i.uv.x, 0.0h, 0.0h, 1.0h);
                return half4(i.uv.x * 0.5h + 0.5h, 0.0h, 0.0h, 1.0h);
                // return half4(i.uv.y, 0.0h, 0.0h, 1.0h);
                // return half4(i.uv.z, 0.0h, 0.0h, 1.0h);
                // return half4(i.uv, 1.0h);
            }
            ENDCG
        }
    }
}
