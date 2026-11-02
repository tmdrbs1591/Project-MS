// 용암 폭포. 그림 없이 전부 셰이더로 그린다 — 흰 사각형 스프라이트(Full Rect)에 붙여 세로로 길게 키우면 된다.
// - 꿀렁임: 노이즈 텍스처로 화면 좌표 자체를 일그러뜨려(디폼) 줄기 모양이 제자리에서 계속 부풀고 휘며 출렁인다.
//   (그냥 아래로 스크롤되는 게 아니라, 시간 축으로도 노이즈를 굴려서 모양 자체가 변한다)
// - 흐름: 속은 노이즈 텍스처 두 장을 서로 다른 속도로 흘려보내고, 또 다른 노이즈로 흐름을 비틀어(플로우 디스토션) 녹은 결이 흘러내린다.
// - 가운데는 뜨겁고, 가장자리는 식어서 어둡고, 바닥은 부딪혀 퍼지며, 주변으로 열기가 번진다.
// 사각형 uv: x = 가로(0~1), y = 아래 0 ~ 위 1. 무늬는 월드 단위라 늘려도 늘어나지 않는다.
Shader "Custom/Sprite/LavaFall"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (흰 사각형)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Noise (반복 텍스처)", 2D) = "gray" {}
        [NoScaleOffset] _NoiseTex2 ("Noise 2 (비틀기용)", 2D) = "gray" {}

        [Header(Shape)]
        _Width ("Stream Width (units)", Range(0.1, 5)) = 1.1
        _Splash ("Bottom Splash (uv)", Range(0, 0.4)) = 0.08
        _TopFade ("Top Fade (uv)", Range(0, 0.3)) = 0.03

        [Header(Deform)]
        _DeformAmount ("Deform Amount (units)", Range(0, 2)) = 0.45
        _DeformScale ("Deform Scale (per unit)", Range(0.01, 1)) = 0.12
        _DeformSpeed ("Deform Morph Speed", Range(0, 2)) = 0.35
        _DeformTravel ("Deform Travel Speed (units/s)", Range(0, 6)) = 1.2
        _Bulge ("Bulge (굵기 꿀렁)", Range(0, 1)) = 0.45
        _EdgeRipple ("Edge Ripple", Range(0, 0.5)) = 0.15

        [Header(Flow)]
        _FlowSpeed ("Flow Speed (units/s)", Range(0, 12)) = 3.5
        _FlowScale ("Flow Texture Scale (per unit)", Range(0.02, 2)) = 0.35
        _FlowStretch ("Flow Stretch (세로로 길게)", Range(1, 8)) = 3
        _FlowDistort ("Flow Distortion", Range(0, 1)) = 0.35

        [Header(Color)]
        [HDR] _HotColor ("Hot (yellow)", Color) = (1.9, 1.3, 0.45, 1)
        [HDR] _MidColor ("Mid (orange)", Color) = (1.5, 0.45, 0.06, 1)
        _CoolColor ("Cool (red)", Color) = (0.6, 0.07, 0.03, 1)
        _CrustColor ("Crust (dark)", Color) = (0.15, 0.03, 0.04, 1)
        _CoreHeat ("Core Heat", Range(0, 2)) = 0.45
        _HeatBias ("Heat Bias", Range(-1, 0.5)) = -0.2
        _Lumps ("Cooled Lumps", Range(0, 1)) = 0.6
        [HDR] _GlowColor ("Heat Glow", Color) = (1.3, 0.3, 0.05, 1)
        _GlowWidth ("Glow Width (units)", Range(0.01, 3)) = 0.6
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.35
        _Brightness ("Brightness", Range(0, 3)) = 1
        _Seed ("Pattern Seed (폭포마다 다르게)", Float) = 0
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
                float2 size       : TEXCOORD1; // 사각형의 월드 가로/세로
                float  seed       : TEXCOORD2; // 폭포마다 무늬가 다르게(머티리얼의 _Seed)
            };

            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_NoiseTex2); SAMPLER(sampler_NoiseTex2);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Width, _Splash, _TopFade;
                float _DeformAmount, _DeformScale, _DeformSpeed, _DeformTravel, _Bulge, _EdgeRipple;
                float _FlowSpeed, _FlowScale, _FlowStretch, _FlowDistort;
                float4 _HotColor, _MidColor, _CoolColor, _CrustColor;
                float _CoreHeat, _HeatBias, _Lumps;
                float4 _GlowColor;
                float _GlowWidth, _GlowStrength, _Brightness, _Seed;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.size = float2(length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20)),
                                length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21)));
                // 위치로 시드를 만들면 패럴랙스로 오브젝트가 움직일 때 무늬가 꼬이며 빨라진다 → 머티리얼 값으로 고정
                o.seed = frac(_Seed * 0.137);
                return o;
            }

            // 노이즈 텍스처 읽기(-0.5 ~ 0.5)
            float N1 (float2 p) { return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p).r - 0.5; }
            float N2 (float2 p) { return SAMPLE_TEXTURE2D(_NoiseTex2, sampler_NoiseTex2, p).r - 0.5; }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 wp = float2((i.uv.x - 0.5) * i.size.x, i.uv.y * i.size.y); // 사각형 안 월드 좌표(가운데 0, 아래 0)
                float2 seed = float2(i.seed * 7.3, i.seed * 3.1);

                // ---------- 꿀렁임(디폼) ----------
                // 같은 노이즈를 "아래로 흘러가는 축"과 "시간에 따라 변하는 축" 두 방향으로 읽어 섞는다
                //  → 부푼 곳이 흘러내리면서 동시에 제자리에서 모양이 계속 바뀐다
                float2 dq = wp * _DeformScale + seed;
                float travel = t * _DeformTravel * _DeformScale;
                float morph = t * _DeformSpeed * 0.1;
                float dx = N1(float2(dq.x * 0.5 + morph, dq.y + travel)) + N2(float2(dq.y * 0.7 - morph * 1.3, dq.x + 0.37 + travel * 0.6)) * 0.35;
                float dy = N2(float2(dq.x + 0.53, dq.y * 0.8 + travel + morph));
                float2 p = wp + float2(dx, dy * 0.15) * _DeformAmount * 2.0;

                // 굵기도 꿀렁(부풀었다 홀쭉해졌다)
                float bulge = N1(float2(p.y * _DeformScale * 1.6 + travel * 1.4 + 0.71, morph * 2.0 + seed.x));
                float width = _Width * (1 + bulge * 2.0 * _Bulge);
                float splash = smoothstep(_Splash, 0, i.uv.y);
                width *= 1 + splash * 1.6;

                float s = p.x / (width * 0.5);               // -1 ~ 1 이 줄기 안
                float as = abs(s);
                // 가장자리 잔물결: 빠르게 흘러내리는 작은 노이즈
                as += _EdgeRipple * 2.0 * N2(float2(sign(s) * 0.31 + p.y * 0.05, p.y * 0.6 + t * _FlowSpeed * 0.6) + seed);
                float aa = max(fwidth(as), 1e-4);
                float inside = smoothstep(1 + aa, 1 - aa, as);
                inside *= smoothstep(1, 1 - _TopFade, i.uv.y);

                // ---------- 안쪽 흐름(노이즈 텍스처를 흘려보냄) ----------
                float2 fq = float2(p.x, p.y / _FlowStretch) * _FlowScale + seed;
                float fv = t * _FlowSpeed * _FlowScale;
                // 비틀기용 노이즈도 천천히 흘려서 흐름 결이 구불구불 휘게
                float2 warp = float2(N2(fq * 0.7 + float2(0, fv * 0.35)), N2(fq * 0.7 + float2(0.5, fv * 0.35 + 0.5))) * _FlowDistort;
                float a = N1(fq + float2(0, fv) + warp) + 0.5;
                float b = N1(fq * float2(2.1, 1.7) + float2(0.33, fv * 1.6) + warp * 1.5) + 0.5;
                float streak = a * 0.6 + b * 0.4;
                // 밝은 결(리지): 노이즈 중간값 근처가 얇은 선
                float vein = pow(saturate(1 - abs(b - 0.5) * 4.0), 3);

                float core = 1 - as * as;
                float heat = saturate(streak * 0.9 + vein * 0.55 + core * _CoreHeat + _HeatBias + splash * 0.35);

                float3 col = lerp(_CoolColor.rgb, _MidColor.rgb, smoothstep(0.2, 0.55, heat));
                col = lerp(col, _HotColor.rgb, smoothstep(0.62, 0.95, heat));

                // 식은 덩어리: 노이즈 진한 곳이 검붉게 굳어 같이 떠내려간다
                float lumpN = N1(fq * 1.3 + float2(0.77, fv * 0.9) + warp * 0.5) + 0.5;
                float lump = smoothstep(0.66, 0.8, lumpN) * smoothstep(0.15, 0.8, as);
                col = lerp(col, _CrustColor.rgb, lump * _Lumps);
                // 가장자리 어둡게
                col *= lerp(1, 0.5, smoothstep(0.7, 1.0, as));

                // ---------- 바깥 열기 ----------
                float dist = max(as - 1, 0) * width * 0.5;
                float glow = exp(-dist / _GlowWidth) * _GlowStrength * (1 - inside) * smoothstep(1, 1 - _TopFade * 4, i.uv.y);
                glow *= smoothstep(0, 0.15, i.uv.x) * smoothstep(1, 0.85, i.uv.x); // 사각형 경계에서 잘려 보이지 않게

                float3 outCol = col * inside + _GlowColor.rgb * glow;
                float alpha = saturate(inside + glow);
                outCol = alpha > 0 ? outCol / max(alpha, 1e-3) : 0;
                outCol *= _Brightness * i.color.rgb;
                return half4(outCol, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}
