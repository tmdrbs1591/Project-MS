// 용암 위 열기 아지랑이. 흰 사각형 스프라이트(Full Rect)를 용암 바로 위에 깔면, 일렁이며 위로 피어오르는 열기 물결이 보인다.
// (게임 씬 카메라에서는 화면 텍스처가 갱신되지 않아 화면을 직접 일그러뜨리는 대신, 열기 물결 자체를 그려서 표현한다)
// - 노이즈로 구불구불 휘는 가는 물결선들이 위로 흘러 올라가고, 아래(용암 쪽)일수록 진하고 위로 갈수록 사라진다.
Shader "Custom/Sprite/HeatHaze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (흰 사각형)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise (반복 텍스처)", 2D) = "gray" {}
        [HDR] _Color ("Heat Color", Color) = (1.0, 0.55, 0.3, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.18
        _LineScale ("Wave Line Density (per unit)", Range(0.1, 6)) = 1.6
        _Wobble ("Wobble", Range(0, 2)) = 0.8
        _Scale ("Noise Scale (per unit)", Range(0.05, 3)) = 0.35
        _Speed ("Rise Speed", Range(0, 3)) = 0.9
        _EdgeFade ("Side Edge Fade (uv)", Range(0.001, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 local      : TEXCOORD1;
            };

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Intensity, _LineScale, _Wobble, _Scale, _Speed, _EdgeFade;
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
                float2 p = i.local * _Scale;
                float rise = t * _Speed * _Scale;
                // 노이즈로 휘게 한 가로 좌표 → 세로로 구불구불 올라가는 물결선
                float bend = (N(float2(p.x * 0.7, p.y * 0.5 - rise)) - 0.5) * _Wobble * 2
                           + (N(float2(p.x * 1.9 + 0.37, p.y * 1.1 - rise * 1.6)) - 0.5) * _Wobble;
                float x = i.local.x * _LineScale + bend * 3;
                float lines = pow(0.5 + 0.5 * sin(x * 6.2831), 2);        // 넓고 부드러운 물결(가는 선은 낙서처럼 보임)
                // 물결선이 끊겼다 이어지며 피어오르게
                float puff = smoothstep(0.35, 0.75, N(float2(p.x * 0.9 + 0.71, p.y * 0.8 - rise * 1.2)));
                float shimmer = lines * puff + puff * 0.25;

                float fade = (1 - i.uv.y) * (1 - i.uv.y);
                fade *= smoothstep(0, _EdgeFade, i.uv.x) * smoothstep(1, 1 - _EdgeFade, i.uv.x);
                return half4(_Color.rgb * i.color.rgb, saturate(shimmer * fade * _Intensity) * i.color.a);
            }
            ENDHLSL
        }
    }
}
