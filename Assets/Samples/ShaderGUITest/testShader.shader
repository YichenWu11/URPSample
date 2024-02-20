Shader "Unlit/testShader"
{
    Properties
    {
        [Title(Main Samples)]
        [Main(Group0)] _group ("Group0", float) = 0
        [Sub(Group0)] _float ("Float", float) = 0
        

        [Main(Group1, _KEYWORD, on, on)] _group1 ("Group - Default Open", float) = 1
        [Sub(Group1)] _float1 ("Sub Float", float) = 0
        [Sub(Group1)] _vector1 ("Sub Vector", vector) = (1, 1, 1, 1)
        [Sub(Group1)] [HDR] _color1 ("Sub HDR Color", color) = (0.7, 0.7, 1, 1)
        [SubToggle(Group1, _SUBKEYWORD)] _subKeyword ("Sub Keyword", float) = 1
        [SubPowerSlider(Group1, 2)] _PowerSliderFloat ("Power Slider Float", Range(0, 10)) = 0
        [SubIntRange(Group1)] _Int0 ("Sub Int Range", Range(0, 10)) = 0
        
        [Title(Group1, Conditional Display Samples       Enum)]
        [KWEnum(Group1, Name 1, _KEY1, Name 2, _KEY2, Name 3, _KEY3)]
        _enum ("Sub Enum", float) = 0
        [Sub(Group1_KEY1)] _key1_Float1 ("Key1 Float", float) = 0
        [Sub(Group1_KEY2)] _key2_Float2 ("Key2 Float", float) = 0
        [Sub(Group1_KEY3)] _key3_Float3 ("Key3 Float", float) = 0
        [SubPowerSlider(Group1_KEY3, 10)] _key3_Float4_PowerSlider ("Key3 Power Slider", Range(0, 1)) = 0
        
        
        [Title(_, MinMaxSlider Samples)]

        [MinMaxSlider(_, _rangeStart, _rangeEnd)] _minMaxSlider("Min Max Slider (0 - 1)", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _rangeStart("Range Start", Range(0.0, 0.5)) = 0.0
        [HideInInspector] _rangeEnd("Range End", Range(0.5, 1.0)) = 1.0
        
        [Space(50)]
        [Title(_, Tex and Color Samples)]

        [Tex(_, _color)] _tex ("Tex with Color", 2D) = "white" { }
        [HideInInspector] _color (" ", Color) = (1, 0, 0, 1)

        // Display up to 4 colors in a single line (Unity 2019.2+)
        [Color(_, _mColor1, _mColor2, _mColor3)]
        _mColor ("Multi Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _mColor1 (" ", Color) = (1, 0, 0, 1)
        [HideInInspector] _mColor2 (" ", Color) = (0, 1, 0, 1)
        [HideInInspector] [HDR] _mColor3 (" ", Color) = (0, 0, 1, 1)
        
        [Space(50)]
        [Title(_, Ramp Samples)]
        [Ramp] _Ramp ("Ramp Map", 2D) = "white" { }
        
        [Title(_, Channel Samples)]
        [Channel(_)]_textureChannelMask("Texture Channel Mask (Default G)", Vector) = (0,1,0,0)
        
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        
//        [Preset(LWGUI_BlendModePreset)] _BlendMode ("Blend Mode Preset", float) = 0 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _KEYWORD
            #pragma shader_feature_local_fragment _SUBKEYWORD
            #pragma shader_feature_local_fragment _KEY1 _KEY2 _KEY3

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

            float4 _color1;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
            #if _KEYWORD
                half4 color = 1.0h;
            #else
                half4 color = 0.0h;
            #endif

            #if _SUBKEYWORD
                half4 tint_color = half4(0.5h, 0.5h, 0.5h, 1.0h);
            #else
                half4 tint_color = 1.0h;
            #endif
                return color * tint_color;
            }
            ENDCG
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}
