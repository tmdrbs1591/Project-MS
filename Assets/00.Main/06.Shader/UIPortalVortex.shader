// 포탈로 빨려 들어갈 때 화면 가장자리에서 소용돌이치며 조여 오는 포탈 테두리 (uGUI 전체 화면 Image/RawImage 용).
// - 가운데는 비어 있고, 가장자리일수록 진한 보라 소용돌이가 돈다.
// - _Intensity(0~1)를 올리면 소용돌이가 안쪽으로 조여 오고 더 빠르게 돈다.
Shader "Custom/UI/PortalVortex"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _DeepColor ("Deep Color", Color) = (0.03, 0.0, 0.1, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.55, 0.2, 1, 1)
        [HDR] _RimColor ("Rim Color", Color) = (0.6, 0.9, 1, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _Twist ("Twist", Range(0, 8)) = 3
        _Speed ("Speed", Range(0, 10)) = 2.5
        _Aspect ("Aspect (w/h)", Float) = 1.7778

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _DeepColor;
                float4 _GlowColor;
                float4 _RimColor;
                float _Intensity;
                float _Twist;
                float _Speed;
                float _Aspect;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float noise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * float2(_Aspect, 1) * 2;   // 세로 기준 -1~1
                float r = length(p);
                float a = atan2(p.y, p.x);
                float t = _Time.y * _Speed * (1 + _Intensity * 1.5);

                // 안쪽일수록 더 많이 비틀린 소용돌이 좌표(반시계 방향으로 돈다)
                float swirl = a - _Twist / (r + 0.35) + t;
                float bands = sin(swirl * 5 + r * 6) * 0.5 + 0.5;
                // 각도를 그대로 노이즈에 넣으면 왼쪽(각도가 한 바퀴 도는 곳)에서 끊긴다 →
                // 원 위의 점(cos, sin)으로 바꿔서 넣어 이음매 없이 이어지게 한다.
                float2 dir = float2(cos(swirl), sin(swirl));
                float n = noise(dir * 2.4 + (r * 4 - t * 1.5));
                float n2 = noise(dir * 5.5 + float2(3.1, 7.7) + (r * 9 - t * 3));
                float pattern = saturate(bands * 0.55 + n * 0.6 + n2 * 0.25);

                // 가장자리 마스크: _Intensity가 클수록 안쪽까지 조여 온다(울렁이는 경계).
                float edge = lerp(2.2, 0.55, _Intensity) + (n - 0.5) * 0.25;
                float mask = smoothstep(edge - 0.45, edge + 0.25, r);
                // 경계에 밝은 테두리 빛
                float rim = exp(-abs(r - edge) * 9) * (0.6 + 0.4 * n2);

                // 대비를 세게: 어두운 우주 바탕 위로 밝은 보라 줄기가 감겨 들어간다.
                float streak = pow(pattern, 2.2);
                float3 col = lerp(_DeepColor.rgb, _GlowColor.rgb, streak) + _GlowColor.rgb * pow(pattern, 6) * 0.8;
                col += _RimColor.rgb * rim * _Intensity * 1.2;
                float alpha = saturate(mask * (0.85 + 0.15 * pattern) + rim * 0.5) * saturate(_Intensity * 3);
                return half4(col, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}
