// 2D 빛줄기(God Rays) - 텍스처 없이 절차적으로 생성
// UI Image / SpriteRenderer 둘 다 사용 가능 (Additive)
Shader "Custom/2D/GodRays"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 0.97, 0.85, 0.6)

        [Header(Origin)]
        _Origin ("Origin (UV)", Vector) = (0.2, 1.3, 0, 0)
        _Direction ("Direction (deg, 0 = down)", Range(-90, 90)) = -20
        _Spread ("Spread (deg)", Range(1, 180)) = 60

        [Header(Rays)]
        _RayDensity ("Ray Density", Range(1, 60)) = 18
        _RaySharpness ("Ray Sharpness", Range(0.5, 8)) = 2.5
        _RayCoverage ("Ray Coverage", Range(0, 1)) = 0.45
        _Speed ("Move Speed", Range(0, 2)) = 0.15
        _Flicker ("Flicker", Range(0, 1)) = 0.3

        [Header(Fade)]
        _Length ("Length", Range(0.1, 3)) = 1.4
        _StartFade ("Start Fade", Range(0, 1)) = 0.1
        _EdgeFade ("Edge Fade", Range(0.01, 1)) = 0.35
        _Intensity ("Intensity", Range(0, 5)) = 1

        // UI 마스크 호환
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
        Blend SrcAlpha One   // Additive
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _Origin;
            float _Direction, _Spread;
            float _RayDensity, _RaySharpness, _RayCoverage, _Speed, _Flicker;
            float _Length, _StartFade, _EdgeFade, _Intensity;
            float4 _ClipRect;

            float hash(float n) { return frac(sin(n) * 43758.5453); }

            // 1D value noise
            float noise1(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(hash(i), hash(i + 1.0), f);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv - _Origin.xy;
                float dist = length(d);

                // 아래 방향 기준 각도(도)
                float ang = degrees(atan2(d.x, -d.y)) - _Direction;
                float halfSpread = _Spread * 0.5;
                float angN = ang / halfSpread;              // -1 ~ 1 범위가 빛 영역

                // 빛줄기: 각도 방향 노이즈 두 겹을 서로 다른 속도로 흘림
                float t = _Time.y * _Speed;
                float x = angN * _RayDensity;
                float n = noise1(x + t * 3.0) * 0.6
                        + noise1(x * 2.3 - t * 2.0 + 17.0) * 0.4;
                float rays = saturate((n - (1.0 - _RayCoverage)) / max(_RayCoverage, 1e-3) + 0.5);
                rays = pow(rays, _RaySharpness);

                // 줄기마다 밝기가 천천히 깜빡임
                float flick = noise1(floor(x) * 7.1 + _Time.y * 0.8);
                rays *= lerp(1.0, flick * 1.5, _Flicker);

                // 부채꼴 가장자리 페이드
                float edge = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, abs(angN));

                // 거리 페이드 (시작 부분 + 끝부분)
                float lenN = dist / _Length;
                float lenFade = smoothstep(0.0, _StartFade + 1e-3, lenN) * (1.0 - smoothstep(0.3, 1.0, lenN));

                float a = rays * edge * lenFade * _Intensity;

                float4 col = i.color;
                col.a *= a * tex2D(_MainTex, i.uv).a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}
