// UI 이미지의 모양(알파) 바깥으로만 빛나는 테두리를 그린다. 원본 위에 겹쳐 두어도 원본을 가리지 않는다.
// - Image의 사각형을 _Inflate만큼 더 크게 잡고(스프라이트는 원래 크기로 매핑), 그 여백에 테두리와 바깥 번짐이 그려진다.
// - 증강 카드 호버/선택 테두리처럼 "카드 모양 그대로" 빛나는 연출용(로비 건물 외곽선과 같은 느낌).
Shader "Custom/UI/OutlineGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Outline Color", Color) = (1.4, 1.4, 1.4, 1)
        _Inflate ("Inflate (사각형 대비 여백 비율)", Range(0, 0.3)) = 0.08
        _Width ("Outline Width (텍스처 픽셀)", Range(1, 120)) = 26
        _GlowScale ("Glow Radius Scale", Range(1, 5)) = 2.6
        _Glow ("Glow Amount", Range(0, 2)) = 0.7
        _AlphaCut ("Alpha Cut", Range(0, 1)) = 0.4

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Inflate, _Width, _GlowScale, _Glow, _AlphaCut;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float A (float2 uv)
            {
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                return smoothstep(_AlphaCut * 0.5, _AlphaCut, tex2Dlod(_MainTex, float4(uv, 0, 2)).a) * inside;
            }

            float Dilate (float2 uv, float2 r)
            {
                float a = 0;
                [unroll] for (int k = 0; k < 20; k++)
                {
                    float ang = k * 0.314159;
                    float2 d = float2(cos(ang), sin(ang));
                    a = max(a, A(uv + d * r));
                    a = max(a, A(uv + d * r * 0.55));
                }
                return a;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 넓힌 사각형의 uv를 원래 스프라이트 공간으로 되돌린다
                float2 uv = (i.uv - 0.5) * (1 + 2 * _Inflate) + 0.5;
                float2 r = _MainTex_TexelSize.xy * _Width;
                float self = A(uv);
                float core = Dilate(uv, r);
                float glow = Dilate(uv, r * _GlowScale);
                float outline = saturate(core - self);
                float halo = saturate(glow - self) * (1 - outline);
                float a = saturate(outline + halo * 0.45 * _Glow);
                fixed3 col = _Color.rgb * i.color.rgb * (1 + outline * 0.6);   // 이미지 색으로 팩 색을 입힌다
                return fixed4(col, a * _Color.a * i.color.a);
            }
            ENDCG
        }
    }
}
