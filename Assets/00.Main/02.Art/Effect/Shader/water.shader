Shader "Custom/UIWaterWobble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _NoiseTex ("Deform Noise", 2D) = "gray" {}
        _NoiseScale ("Deform Tiling", Range(0.1, 20)) = 5
        _FlowDir ("Flow Direction (XY)", Vector) = (1, 0.3, 0, 0)
        _Strength ("Deform Strength", Range(0, 0.08)) = 0.012
        _Speed ("Flow Speed", Range(0, 5)) = 1

        [Header(Water Sparkle)]
        [HDR] _SparkleColor ("Sparkle Color", Color) = (1,1,1,1)
        _SparkleStrength ("Sparkle Strength", Range(0, 2)) = 0.6
        _SparkleScale ("Sparkle Scale", Range(1, 40)) = 10
        _SparkleSpeed ("Sparkle Speed", Range(0, 5)) = 1
        _SparkleThreshold ("Sparkle Threshold (높을수록 얇고 드물게)", Range(0.3, 0.95)) = 0.72

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float _NoiseScale;
            float4 _FlowDir;
            float _Strength;
            float _Speed;
            fixed4 _SparkleColor;
            float _SparkleStrength;
            float _SparkleScale;
            float _SparkleSpeed;
            float _SparkleThreshold;

            // 부드러운 값 노이즈
            float hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 디폼 노이즈 텍스처를 흐름 방향으로 스크롤 → 물 흐를 때처럼 반사가 일그러짐
                float t = _Time.y * _Speed;
                float2 flow = _FlowDir.xy * t;
                float2 dUV = IN.texcoord * _NoiseScale + flow;
                float dx = tex2D(_NoiseTex, dUV).r;
                float dy = tex2D(_NoiseTex, dUV + float2(0.37, 0.53)).r; // 다른 위치 → x/y 다른 변위
                float2 uv = IN.texcoord + (float2(dx, dy) - 0.5) * _Strength;

                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;

                // --- 물 빛반사: 물결 솟은 부분에 흰색이 얇게 반짝 ---
                float st = _Time.y * _SparkleSpeed;
                // y를 늘려 가로로 눕는 반짝임 밴드 + 위로 흐름
                float2 spUV = float2(IN.texcoord.x * _SparkleScale, IN.texcoord.y * _SparkleScale * 5.0 + st);
                float sp = noise(spUV) * noise(spUV * 1.7 - st * 0.6); // 두 겹으로 불규칙 반짝임
                float sparkle = smoothstep(_SparkleThreshold, 1.0, sp);
                color.rgb += _SparkleColor.rgb * sparkle * _SparkleStrength * color.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
