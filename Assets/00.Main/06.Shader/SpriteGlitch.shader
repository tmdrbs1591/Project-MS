// 스프라이트 글리치 셰이더 (URP 2D, SpriteRenderer 용 / 조명 영향 없음)
// - 가로 줄 단위로 화면이 찢어지듯 밀리는 슬라이스 변위
// - RGB 색 분리(채널이 좌우로 어긋남)
// - 블록 노이즈 / 스캔라인 / 깜빡임
// - 평소엔 멀쩡하다가 가끔 "팍" 튀는 버스트 방식(Always On 을 켜면 계속 글리치)
Shader "Custom/2D/SpriteGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Timing)]
        [Toggle] _AlwaysOn ("Always On (계속 글리치)", Float) = 0
        _Intensity ("Intensity (전체 세기)", Range(0, 1)) = 1
        _BurstFrequency ("Burst Frequency (초당 튀는 빈도)", Range(0, 10)) = 1.5
        _BurstChance ("Burst Chance (튈 확률)", Range(0, 1)) = 0.35
        _Speed ("Glitch Speed (패턴 바뀌는 속도)", Range(1, 60)) = 18

        [Header(Slice Displacement)]
        _SliceCount ("Slice Count (가로 줄 수)", Range(2, 80)) = 24
        _SliceOffset ("Slice Offset (밀리는 거리)", Range(0, 0.3)) = 0.06
        _SliceThreshold ("Slice Threshold (밀리는 줄 비율, 높을수록 적게)", Range(0, 1)) = 0.6

        [Header(RGB Split)]
        _RGBSplit ("RGB Split (색 분리 거리)", Range(0, 0.1)) = 0.02

        [Header(Block Noise)]
        _BlockSize ("Block Size", Range(2, 64)) = 12
        _BlockStrength ("Block Strength (블록 색 튐)", Range(0, 1)) = 0.35

        [Header(Scanline and Flicker)]
        _ScanlineCount ("Scanline Count", Range(0, 400)) = 120
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.15
        _Flicker ("Flicker (밝기 깜빡임)", Range(0, 1)) = 0.2
        [HDR] _GlitchColor ("Glitch Color (블록에 섞이는 색)", Color) = (0, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALWAYSON_ON

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
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Intensity;
                float _BurstFrequency;
                float _BurstChance;
                float _Speed;
                float _SliceCount;
                float _SliceOffset;
                float _SliceThreshold;
                float _RGBSplit;
                float _BlockSize;
                float _BlockStrength;
                float _ScanlineCount;
                float _ScanlineStrength;
                float _Flicker;
                float4 _GlitchColor;
            CBUFFER_END

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // 지금 글리치가 터지는 중인지(0~1). 버스트 구간마다 랜덤으로 켜지고, 켜진 동안은 세기도 랜덤.
            float GlitchAmount()
            {
                #if defined(_ALWAYSON_ON)
                    return _Intensity;
                #else
                    float slot = floor(_Time.y * _BurstFrequency);
                    float on = step(1.0 - _BurstChance, hash11(slot + 3.7));
                    // 버스트 구간 안에서도 짧게 끊겼다 켜졌다 하도록 빠른 on/off 를 섞는다.
                    float stutter = step(0.3, hash11(floor(_Time.y * _Speed) + 11.1));
                    return on * stutter * _Intensity * lerp(0.5, 1.0, hash11(slot + 91.3));
                #endif
            }

            half4 frag(Varyings i) : SV_Target
            {
                float g = GlitchAmount();
                float2 uv = i.uv;
                float step_t = floor(_Time.y * _Speed);

                // 1) 가로 슬라이스 변위: 일부 줄만 좌우로 확 밀린다.
                float slice = floor(uv.y * _SliceCount);
                float sliceRand = hash21(float2(slice, step_t));
                float sliceOn = step(_SliceThreshold, sliceRand);
                float offset = (hash21(float2(slice, step_t + 7.0)) - 0.5) * 2.0 * _SliceOffset * sliceOn * g;
                uv.x += offset;

                // 2) RGB 색 분리: 채널을 서로 반대 방향으로 어긋나게 샘플.
                float split = _RGBSplit * g * (0.5 + hash11(step_t + 5.0));
                half4 cR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(split, 0));
                half4 cG = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 cB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(split, 0));
                half4 col = half4(cR.r, cG.g, cB.b, max(cG.a, max(cR.a, cB.a) * step(0.001, g)));

                // 3) 블록 노이즈: 사각 블록 몇 개가 다른 색으로 튀거나 반전된다.
                float2 block = floor(i.uv * _BlockSize);
                float blockRand = hash21(block + step_t);
                float blockOn = step(1.0 - _BlockStrength * g, blockRand);
                col.rgb = lerp(col.rgb, lerp(1.0 - col.rgb, _GlitchColor.rgb, 0.5), blockOn);

                // 4) 스캔라인 + 깜빡임.
                float scan = 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin(i.uv.y * _ScanlineCount * 6.2831 + _Time.y * 20.0));
                float flicker = 1.0 - _Flicker * g * hash11(step_t + 23.0);
                col.rgb *= lerp(1.0, scan, g) * flicker; // 글리치가 없을 땐 원본 그대로

                col *= i.color;
                return col;
            }
            ENDHLSL
        }
    }
}
