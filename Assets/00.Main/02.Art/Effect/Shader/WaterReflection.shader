// 카메라/렌더텍스처 없이 머티리얼만으로 "위쪽 화면"을 뒤집어 비추는 물 반사 셰이더 (URP 2D Renderer)
// - 화면은 URP 2D 의 Camera Sorting Layer Texture(_CameraSortingLayerTexture)에서 가져온다.
// - 스프라이트 윗변을 수면으로 보고, 그 위 화면을 아래로 뒤집어 그린다.
// - 일렁임/반짝임은 water.shader(Custom/UIWaterWobble) 와 동일.
//
// [설정]
//   1) Settings/Renderer2D.asset → Camera Sorting Layer Texture → Foremost Sorting Layer 를
//      "반사에 비칠 것들이 들어있는 가장 앞 레이어"로 지정하고, Downsampling 은 None 권장.
//   2) 이 머티리얼을 쓰는 물 스프라이트는 그 레이어보다 "뒤에 그려지는" 소팅 레이어에 둔다.
//   3) 스프라이트 Mesh Type 은 Full Rect (윗변 = 수면 계산에 UV 0~1 을 사용).
Shader "Custom/2D/WaterReflection"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (마스크)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _NoiseTex ("Deform Noise", 2D) = "gray" {}
        _NoiseScale ("Deform Tiling", Range(0.1, 20)) = 5
        _FlowDir ("Flow Direction (XY)", Vector) = (1, 0.3, 0, 0)
        _Strength ("Deform Strength", Range(0, 0.08)) = 0.012
        _Speed ("Flow Speed", Range(0, 5)) = 1

        [Header(Reflection)]
        _ReflectionOffset ("Waterline Offset (화면 UV)", Range(-0.2, 0.2)) = 0
        _DepthFade ("Depth Fade (아래로 갈수록 흐려짐)", Range(0, 1)) = 0

        [Header(Water Sparkle)]
        [HDR] _SparkleColor ("Sparkle Color", Color) = (1,1,1,1)
        _SparkleStrength ("Sparkle Strength", Range(0, 2)) = 0.6
        _SparkleScale ("Sparkle Scale", Range(1, 40)) = 10
        _SparkleSpeed ("Sparkle Speed", Range(0, 5)) = 1
        _SparkleThreshold ("Sparkle Threshold (높을수록 얇고 드물게)", Range(0.3, 0.95)) = 0.72
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
                float4 screenPos  : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);                    SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);                   SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_CameraSortingLayerTexture);  SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _NoiseScale;
                float4 _FlowDir;
                float _Strength;
                float _Speed;
                float _ReflectionOffset;
                float _DepthFade;
                float4 _SparkleColor;
                float _SparkleStrength;
                float _SparkleScale;
                float _SparkleSpeed;
                float _SparkleThreshold;
            CBUFFER_END

            // 부드러운 값 노이즈 (water.shader 와 동일)
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

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // 스프라이트 UV 1 만큼이 화면 UV 로 얼마인지(부호 포함). 선형이라 미분이 정확하다.
                // 플랫폼마다 화면 텍스처의 위/아래 방향이 달라도 부호가 알아서 맞춰진다.
                float dUV = ddy(i.uv.y);
                float k = abs(dUV) > 1e-6 ? ddy(screenUV.y) / dUV : 0.0;

                // 수면(윗변)까지의 거리만큼 위로 올라간 지점 = 뒤집힌 반사 위치
                float distToTop = 1.0 - i.uv.y;
                float2 reflUV = float2(screenUV.x, screenUV.y + 2.0 * distToTop * k + _ReflectionOffset * sign(k));

                // 디폼 노이즈 텍스처를 흐름 방향으로 스크롤 → 물 흐를 때처럼 반사가 일그러짐
                float t = _Time.y * _Speed;
                float2 flow = _FlowDir.xy * t;
                float2 nUV = i.uv * _NoiseScale + flow;
                float dx = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV).r;
                float dy = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUV + float2(0.37, 0.53)).r;
                reflUV += (float2(dx, dy) - 0.5) * _Strength;
                reflUV = saturate(reflUV);

                half4 color = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, reflUV);
                color.a = 1.0;
                color *= i.color;
                color.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                color.a *= lerp(1.0, i.uv.y, _DepthFade);

                // --- 물 빛반사: 물결 솟은 부분에 흰색이 얇게 반짝 ---
                float st = _Time.y * _SparkleSpeed;
                float2 spUV = float2(i.uv.x * _SparkleScale, i.uv.y * _SparkleScale * 5.0 + st);
                float sp = noise(spUV) * noise(spUV * 1.7 - st * 0.6);
                float sparkle = smoothstep(_SparkleThreshold, 1.0, sp);
                color.rgb += _SparkleColor.rgb * sparkle * _SparkleStrength * color.a;

                return color;
            }
            ENDHLSL
        }
    }
}
