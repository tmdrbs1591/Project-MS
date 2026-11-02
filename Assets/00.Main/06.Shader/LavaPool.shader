// 용암 바닥(웅덩이). 그림 없이 전부 셰이더로 그린다 — 흰 사각형 스프라이트(Full Rect)에 붙여서 크기만 키우면 된다.
// - 표면: 여러 파도를 겹친 수면선이 출렁이고, 그 위로 열기 빛무리와 위로 떠오르는 불티가 피어오른다.
// - 속: 녹은 용암이 소용돌이치며 흐르고, 그 위를 검붉은 껍질 조각들이 떠다니며 갈라진 틈으로 노란 빛이 새어 나온다.
// - 표면 근처에서 거품이 부풀었다 터진다.
// 모든 무늬는 월드 좌표 기준이라 사각형을 늘려도 무늬가 늘어나지 않는다.
Shader "Custom/Sprite/LavaPool"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (흰 사각형)", 2D) = "white" {}

        [Header(Surface)]
        _SurfaceHeight ("Surface Height (uv)", Range(0, 1)) = 0.65
        _WaveAmp ("Wave Amplitude (units)", Range(0, 1)) = 0.16
        _WaveLength ("Wave Length (units)", Range(0.5, 20)) = 4
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.2

        [Header(Flow)]
        _FlowDir ("Flow Direction (xy)", Vector) = (1, 0, 0, 0)
        _FlowSpeed ("Flow Speed", Range(0, 3)) = 0.35
        _NoiseScale ("Noise Scale (per unit)", Range(0.05, 3)) = 0.45
        _Swirl ("Swirl", Range(0, 3)) = 1.3

        [Header(Crust)]
        _CrustScale ("Crust Plate Size (per unit)", Range(0.1, 3)) = 0.7
        _CrustAmount ("Crust Amount", Range(0, 1)) = 0.55
        _CrustDrift ("Crust Drift Speed", Range(0, 2)) = 0.3
        _GapWidth ("Crack Gap Width", Range(0.01, 0.4)) = 0.12

        [Header(Color)]
        [HDR] _HotColor ("Hot (yellow)", Color) = (2.2, 1.5, 0.5, 1)
        [HDR] _MidColor ("Mid (orange)", Color) = (1.5, 0.45, 0.06, 1)
        _CoolColor ("Cool (red)", Color) = (0.6, 0.07, 0.03, 1)
        _CrustColor ("Crust (dark)", Color) = (0.13, 0.03, 0.04, 1)
        [HDR] _SurfaceColor ("Surface Line", Color) = (2.4, 1.3, 0.35, 1)
        _SurfaceWidth ("Surface Line Width (units)", Range(0.01, 0.5)) = 0.09

        [Header(Above Surface)]
        [HDR] _GlowColor ("Heat Glow", Color) = (1.4, 0.35, 0.05, 1)
        _GlowHeight ("Glow Height (units)", Range(0.05, 4)) = 1.1
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.45
        _Embers ("Embers", Range(0, 2)) = 1
        _EmberHeight ("Ember Rise Height (units)", Range(0.2, 6)) = 2.5
        _Bubbles ("Bubbles", Range(0, 2)) = 1

        _HeatBias ("Heat Bias (낮을수록 붉고 어둡게)", Range(-1, 0.5)) = -0.3
        _GapGlow ("Crack Gap Glow", Range(0, 1.5)) = 0.3
        _Brightness ("Brightness", Range(0, 3)) = 1
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
                float2 posWS      : TEXCOORD1;
                float  height     : TEXCOORD2; // 사각형의 월드 높이
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _SurfaceHeight, _WaveAmp, _WaveLength, _WaveSpeed;
                float4 _FlowDir;
                float _FlowSpeed, _NoiseScale, _Swirl;
                float _CrustScale, _CrustAmount, _CrustDrift, _GapWidth;
                float4 _HotColor, _MidColor, _CoolColor, _CrustColor, _SurfaceColor;
                float _SurfaceWidth;
                float4 _GlowColor;
                float _GlowHeight, _GlowStrength, _Embers, _EmberHeight, _Bubbles;
                float _HeatBias, _GapGlow;
                float _Brightness;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                // 오브젝트 기준(월드 크기) 좌표: 패럴랙스로 오브젝트가 움직여도 무늬가 미끄러지지 않게 같이 따라간다
                o.posWS = TransformObjectToWorld(v.positionOS.xyz).xy - float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13);
                o.height = length(float3(UNITY_MATRIX_M._m01, UNITY_MATRIX_M._m11, UNITY_MATRIX_M._m21));
                return o;
            }

            float2 Hash2 (float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453) * 2.0 - 1.0;
            }
            float Hash1 (float2 p) { return frac(sin(dot(p, float2(41.3, 289.1))) * 45758.5453); }

            float Noise (float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(dot(Hash2(i), f), dot(Hash2(i + float2(1, 0)), f - float2(1, 0)), u.x),
                            lerp(dot(Hash2(i + float2(0, 1)), f - float2(0, 1)), dot(Hash2(i + float2(1, 1)), f - float2(1, 1)), u.x), u.y);
            }

            float Fbm (float2 p)
            {
                float s = 0, a = 0.5;
                [unroll] for (int k = 0; k < 4; k++) { s += Noise(p) * a; p = mul(float2x2(1.6, 1.2, -1.2, 1.6), p) + 11.7; a *= 0.5; }
                return s;
            }

            // 움직이는 셀룰러: x = 가장 가까운 점까지, y = 두 번째와의 차(경계까지), z = 셀 id 해시
            float3 Cells (float2 p, float t)
            {
                float2 i = floor(p), f = frac(p);
                float d1 = 8, d2 = 8, id = 0;
                [unroll] for (int y = -1; y <= 1; y++)
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 h = Hash2(i + g);
                    float2 o = 0.5 + 0.38 * sin(t * (0.6 + 0.4 * h) + 6.2831 * h);
                    float d = length(g + o - f);
                    if (d < d1) { d2 = d1; d1 = d; id = Hash1(i + g); }
                    else if (d < d2) d2 = d;
                }
                return float3(d1, d2 - d1, id);
            }

            float Waves (float x, float t)
            {
                float k = 6.2831 / _WaveLength;
                return _WaveAmp * (sin(x * k + t * _WaveSpeed) * 0.5
                                 + sin(x * k * 2.37 - t * _WaveSpeed * 1.43 + 1.3) * 0.22
                                 + Noise(float2(x * k * 0.35 + t * 0.25, 3.7)) * 0.9);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 wp = i.posWS;

                // 수면까지의 깊이(월드 단위, 안쪽이 +)
                float depth = (_SurfaceHeight - i.uv.y) * i.height + Waves(wp.x, t);
                float aa = max(fwidth(depth), 1e-4);
                float inside = smoothstep(-aa, aa, depth);

                float3 col = 0;
                float alpha = 0;

                // ---------- 수면 위: 열기 빛무리 + 떠오르는 불티 ----------
                float above = -depth;
                float glow = exp(-max(above, 0) / _GlowHeight) * _GlowStrength;
                float2 eq = float2(wp.x * 2.2, (above - t * 0.9) * 1.6);
                float2 ei = floor(eq);
                float eh = Hash1(ei);
                float2 ep = frac(eq) - 0.5 - Hash2(ei + 3.1) * 0.3;
                ep.x += sin(t * 2 + eh * 20) * 0.15;
                float ember = step(0.86, eh) * smoothstep(0.09, 0.0, length(ep)) * saturate(1 - above / _EmberHeight) * step(0, above) * _Embers;
                float3 outCol = _GlowColor.rgb * glow + _HotColor.rgb * ember;
                float outA = saturate(glow + ember);

                // ---------- 속: 흐르는 용암 + 떠다니는 껍질 ----------
                float2 dir = normalize(_FlowDir.xy + 1e-5);
                float2 p = wp * _NoiseScale;
                float2 flow = dir * t * _FlowSpeed;
                float2 w = float2(Fbm(p * 0.7 - flow * 0.5 + 3.1), Fbm(p * 0.7 - flow * 0.5 + 8.3 + t * 0.05));
                float2 pa = p + w * _Swirl - flow;
                float molten = Fbm(pa) * 0.5 + 0.5;
                float vein = pow(saturate(1 - abs(Fbm(pa * 1.8 + 5.2 - flow))), 6);
                float heat = saturate(molten * 0.9 + vein * 0.8 + _HeatBias);

                float3 lava = lerp(_CoolColor.rgb, _MidColor.rgb, smoothstep(0.25, 0.6, heat));
                lava = lerp(lava, _HotColor.rgb, smoothstep(0.62, 0.95, heat));

                // 껍질 조각: 흐름 따라 떠다니고 셀 점이 움직여 모양이 계속 바뀐다
                float2 cp = (wp + w * 0.6 - dir * t * _CrustDrift) * _CrustScale;
                float3 c = Cells(cp, t * 0.5);
                float plate = smoothstep(_GapWidth * 0.5, _GapWidth, c.y);              // 틈 = 0, 조각 안 = 1
                float hasCrust = step(c.z, _CrustAmount) * smoothstep(0.15, 0.6, depth); // 일부 조각만 굳고, 표면 바로 아래는 녹아 있음
                float melt = saturate(Noise(cp * 0.7 + t * 0.2) * 0.8 + 0.4);           // 조각 가장자리부터 녹아 붉게
                float3 crust = lerp(_CrustColor.rgb, _CoolColor.rgb * 0.8, smoothstep(0.0, 0.25, c.y) * (1 - melt) * 0.6 + (1 - melt) * 0.15);
                float crustMask = plate * hasCrust;
                float3 inCol = lerp(lava, crust, crustMask);
                // 틈새는 더 뜨겁게 빛난다
                inCol += _MidColor.rgb * (1 - plate) * hasCrust * _GapGlow;

                // 거품: 표면 근처에서 부풀었다 터진다
                float2 bq = float2(wp.x * 1.3, depth * 1.6);
                float2 bi = floor(bq);
                float bh = Hash1(bi + 7.7);
                float ph = frac(t * (0.25 + bh * 0.35) + bh * 10);
                float2 bp = frac(bq) - 0.5 - Hash2(bi) * 0.25;
                float r = ph * 0.32;
                float ring = smoothstep(0.05, 0.0, abs(length(bp) - r)) * (1 - ph) * step(0.72, bh) * smoothstep(1.6, 0.2, depth) * _Bubbles;
                inCol += _HotColor.rgb * ring;

                // 수면선: 가장 뜨겁게 빛나는 얇은 띠
                float surfLine = exp(-max(depth, 0) / _SurfaceWidth);
                inCol = lerp(inCol, _SurfaceColor.rgb, surfLine * 0.85);
                // 깊을수록 살짝 어둡게
                inCol *= lerp(1, 0.75, saturate(depth / max(i.height, 0.01)));

                col = lerp(outCol, inCol, inside);
                alpha = lerp(outA, 1, inside);

                col *= _Brightness * i.color.rgb;
                return half4(col, alpha * i.color.a);
            }
            ENDHLSL
        }
    }
}
