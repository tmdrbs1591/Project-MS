// 용암 열기 빛. 흰 사각형 스프라이트(Full Rect)에 붙여 넓게 깔면, 아래에서 위로 갈수록 옅어지는 붉은 빛이 더해진다(가산).
// - 노이즈 텍스처로 빛이 천천히 일렁이고 숨 쉬듯 밝아졌다 어두워진다.
// - 지평선 뒤에 깔면 먼 용암 바다의 붉은 하늘빛, 앞산 위에 깔면 아래 용암에 비친 열기가 된다.
Shader "Custom/Sprite/HeatGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (흰 사각형)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise (반복 텍스처)", 2D) = "gray" {}
        [HDR] _Color ("Glow Color", Color) = (1.0, 0.32, 0.08, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.5
        _Height ("Glow Height (uv)", Range(0.05, 1)) = 0.6
        _Falloff ("Falloff", Range(0.3, 6)) = 2
        _Flicker ("Flicker", Range(0, 1)) = 0.35
        _NoiseScale ("Noise Scale (per unit)", Range(0.005, 1)) = 0.08
        _Speed ("Drift Speed", Range(0, 2)) = 0.25
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
                float2 local      : TEXCOORD1;
            };

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Intensity, _Height, _Falloff, _Flicker, _NoiseScale, _Speed, _EdgeFade;
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

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float h = saturate(1 - i.uv.y / _Height);
                float glow = pow(h, _Falloff);
                // 일렁이는 빛: 옆으로 흐르는 노이즈 두 겹 + 전체가 천천히 숨 쉼
                float2 p = i.local * _NoiseScale;
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p + float2(t * _Speed * _NoiseScale * 4, 0)).r * 0.6
                        + SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 1.9 + float2(-t * _Speed * _NoiseScale * 6, 0.37)).r * 0.4;
                float breathe = 0.85 + 0.15 * sin(t * 0.9);
                glow *= lerp(1, (0.55 + n) * breathe, _Flicker);
                glow *= smoothstep(0, _EdgeFade, i.uv.x) * smoothstep(1, 1 - _EdgeFade, i.uv.x);
                return half4(_Color.rgb * i.color.rgb, saturate(glow * _Intensity) * i.color.a);
            }
            ENDHLSL
        }
    }
}
