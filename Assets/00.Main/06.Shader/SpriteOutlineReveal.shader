// 스프라이트 외곽선이 아래에서부터 양옆으로 쫙 올라가 위에서 만나는 연출용 셰이더.
// LobbyHighlight가 스프라이트보다 조금 큰 사각형 메시를 만들어 원본 뒤에 깔고 이 셰이더로 그린다.
// - 주변 알파를 여러 방향으로 훑어 실루엣을 부풀린 뒤(원본이 위를 덮으므로 테두리만 보인다)
// - 높이(_Progress) 아래만 보이게 하고, 올라가는 끝부분은 더 밝게 빛난다.
Shader "Custom/Sprite/OutlineReveal"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 0.95, 0.7, 1)
        [HDR] _GlowColor ("Outer Glow Color", Color) = (0.6, 0.7, 1, 0.5)
        _Radius ("Radius (uv, xy)", Vector) = (0.006, 0.006, 0, 0)
        _GlowScale ("Glow Radius Scale", Range(1, 4)) = 2.4
        _UVRect ("Sprite UV Rect (xMin, yMin, xMax, yMax)", Vector) = (0, 0, 1, 1)
        _Progress ("Reveal Progress", Range(0, 1.2)) = 1
        _FrontWidth ("Front Width", Range(0.01, 0.5)) = 0.12
        _FrontBoost ("Front Boost", Range(0, 4)) = 1.6
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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
                float2 uv         : TEXCOORD0;
                float2 height     : TEXCOORD1; // x = 아래 0 ~ 위 1
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  height     : TEXCOORD1;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float4 _GlowColor;
                float4 _Radius;
                float _GlowScale;
                float4 _UVRect;
                float _Progress;
                float _FrontWidth;
                float _FrontBoost;
                float _Alpha;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.height = v.height.x;
                return o;
            }

            float SampleAlpha (float2 uv)
            {
                // 스프라이트 영역 밖(아틀라스의 다른 칸 등)은 비어 있는 걸로 본다.
                float inside = step(_UVRect.x, uv.x) * step(uv.x, _UVRect.z) * step(_UVRect.y, uv.y) * step(uv.y, _UVRect.w);
                // 밉맵으로 흐려진 알파가 번지지 않게 항상 원본 해상도(LOD 0)로 읽고, 거의 투명한 픽셀은 버린다.
                float a = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a;
                return smoothstep(0.3, 0.6, a) * inside;
            }

            float Dilate (float2 uv, float2 radius)
            {
                float a = 0;
                [unroll] for (int i = 0; i < 16; i++)
                {
                    float ang = i * 0.392699; // 2π/16
                    float2 d = float2(cos(ang), sin(ang));
                    a = max(a, SampleAlpha(uv + d * radius));
                    a = max(a, SampleAlpha(uv + d * radius * 0.5));
                }
                return a;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 r = _Radius.xy;
                float core = smoothstep(0.15, 0.6, Dilate(i.uv, r));
                float glow = Dilate(i.uv, r * _GlowScale);

                // 아래에서부터 차오른다. 끝부분은 부드럽게, 그리고 더 밝게.
                float h = i.height;
                float visible = 1 - smoothstep(_Progress - 0.02, _Progress + 0.02, h);
                float front = (1 - smoothstep(0, _FrontWidth, abs(_Progress - h))) * step(_Progress, 1.0);

                float3 col = lerp(_GlowColor.rgb, _OutlineColor.rgb, core) * (1 + front * _FrontBoost);
                float a = max(core * _OutlineColor.a, glow * _GlowColor.a * 0.6);
                a = saturate(a + front * core * 0.5) * visible * _Alpha;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
