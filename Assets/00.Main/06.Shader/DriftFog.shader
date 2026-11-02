// 천천히 흘러가는 연기/안개 층. 흰 사각형 스프라이트(Full Rect)에 붙여 화면보다 크게 깔고, 레이어 사이마다 하나씩 둔다.
// - 노이즈 텍스처 두 장을 서로 다른 속도로 옆으로 흘리고 서로 비틀어서 연기가 뭉게뭉게 흘러간다.
// - 아래쪽일수록 진하고(용암 열기에 데워진 연기) 아래 색(_BottomColor)이 비친다. 위쪽은 서서히 사라진다.
// - 사각형 가장자리는 부드럽게 사라져서 경계가 안 보인다.
Shader "Custom/Sprite/DriftFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (흰 사각형)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise (반복 텍스처)", 2D) = "gray" {}
        _Color ("Fog Color", Color) = (0.45, 0.32, 0.62, 1)
        [HDR] _BottomColor ("Bottom Color (열기)", Color) = (1.0, 0.35, 0.15, 1)
        _Density ("Density", Range(0, 1)) = 0.45
        _Scale ("Noise Scale (per unit)", Range(0.005, 1)) = 0.06
        _Speed ("Drift Speed (units/s)", Range(-3, 3)) = 0.35
        _Contrast ("Puffiness", Range(0.5, 4)) = 1.8
        _HeightStart ("Fade Start (uv, 아래부터)", Range(0, 1)) = 0.15
        _HeightEnd ("Fade End (uv)", Range(0, 1)) = 0.7
        _BottomGlow ("Bottom Glow Amount", Range(0, 1)) = 0.5
        _EdgeFade ("Edge Fade (uv)", Range(0.001, 0.5)) = 0.12
        _Seed ("Seed", Float) = 0
        [Toggle] _FlipV ("Flip Vertical (위가 진하게: 하늘 구름용)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 local      : TEXCOORD1; // 오브젝트 기준(월드 크기) 좌표
            };

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color, _BottomColor;
                float _Density, _Scale, _Speed, _Contrast, _HeightStart, _HeightEnd, _BottomGlow, _EdgeFade, _Seed, _FlipV;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.local = TransformObjectToWorld(v.positionOS.xyz).xy - float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13);
                return o;
            }

            float N (float2 p) { return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p).r; }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 p = i.local * _Scale * float2(1, 1.6) + _Seed * 0.173;
                float drift = t * _Speed * _Scale;
                float2 warp = float2(N(p * 0.6 + float2(drift * 0.4, 0.31)), N(p * 0.6 + float2(0.57, drift * 0.25))) - 0.5;
                float a = N(p + float2(drift, 0) + warp * 0.5);
                float b = N(p * 2.1 + float2(drift * 1.7, 0.43) + warp * 0.8);
                float fog = saturate(pow(saturate(a * 0.65 + b * 0.35), _Contrast) * 1.6);

                float v = lerp(i.uv.y, 1 - i.uv.y, _FlipV);                               // 뒤집으면 위가 진하고 아래로 사라짐
                float h = smoothstep(_HeightEnd, _HeightStart, v);
                float edge = smoothstep(0, _EdgeFade, i.uv.x) * smoothstep(1, 1 - _EdgeFade, i.uv.x)
                           * smoothstep(0, _EdgeFade, i.uv.y) * smoothstep(1, 1 - _EdgeFade, i.uv.y);
                float alpha = fog * h * edge * _Density;

                float3 col = lerp(_Color.rgb, _BottomColor.rgb, saturate((1 - v / max(_HeightEnd, 0.01))) * _BottomGlow);
                col *= 0.8 + fog * 0.4;
                return half4(col * i.color.rgb, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}
