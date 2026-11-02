// 물속 바닥/배경에 일렁이는 물빛(코스틱). 흰 사각형 스프라이트(Full Rect)를 넓게 깔면 가산으로 은은하게 더해진다.
// - 노이즈 텍스처 두 장을 서로 다른 방향으로 흘리고, 두 값이 비슷한 곳(경계)만 가늘게 밝혀 그물 모양 물빛을 만든다.
// - 위/아래 가장자리와 좌우 끝은 부드럽게 사라진다(_FadeTop/_FadeBottom).
Shader "Custom/Sprite/Caustics"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (흰 사각형)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise (반복 텍스처)", 2D) = "gray" {}
        [HDR] _Color ("Light Color", Color) = (0.6, 1.0, 1.0, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.35
        _Scale ("Pattern Scale (per unit)", Range(0.02, 2)) = 0.25
        _Speed ("Speed", Range(0, 2)) = 0.25
        _Sharpness ("Line Sharpness", Range(1, 30)) = 10
        _FadeTop ("Fade Top (uv)", Range(0, 1)) = 0.35
        _FadeBottom ("Fade Bottom (uv)", Range(0, 1)) = 0.1
        _EdgeFade ("Side Edge Fade (uv)", Range(0.001, 0.5)) = 0.08
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
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 local : TEXCOORD1; };

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Intensity, _Scale, _Speed, _Sharpness, _FadeTop, _FadeBottom, _EdgeFade;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * unity_SpriteColor;
                o.uv = v.uv;
                o.local = TransformObjectToWorld(v.positionOS.xyz).xy - float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13);
                return o;
            }

            float N (float2 p) { return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p).r; }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 p = i.local * _Scale;
                float2 w = float2(N(p * 0.5 + float2(t * 0.11, 0.3)), N(p * 0.5 + float2(0.7, -t * 0.09))) - 0.5;
                float a = N(p + w * 0.6 + float2(t * 0.13, t * 0.05));
                float b = N(p * 1.13 + w * 0.6 + float2(-t * 0.1, t * 0.08) + 0.41);
                float lines = pow(saturate(1 - abs(a - b) * _Sharpness * 0.25), 3);   // 두 물결이 겹치는 경계만 밝게
                float fine = pow(saturate(1 - abs(N(p * 2.1 - w + t * 0.07) - b) * _Sharpness * 0.3), 4) * 0.5;
                float c = lines + fine;

                float v = smoothstep(0, _FadeBottom, i.uv.y) * smoothstep(1, 1 - _FadeTop, i.uv.y);
                v *= smoothstep(0, _EdgeFade, i.uv.x) * smoothstep(1, 1 - _EdgeFade, i.uv.x);
                return half4(_Color.rgb * i.color.rgb, saturate(c * v * _Intensity) * i.color.a);
            }
            ENDHLSL
        }
    }
}
