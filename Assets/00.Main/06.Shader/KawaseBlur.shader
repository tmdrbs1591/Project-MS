// 화면 배경 블러용 Kawase 블러 한 단계 (Graphics.Blit 전용).
// UIBackgroundBlur가 낮은 해상도로 렌더한 배경에 이 패스를 여러 번(offset을 키워가며) 반복해서
// 싸게 넓은 블러를 만든다. 대각선 4점을 평균내는 방식이라 반복할수록 부드럽게 퍼진다.
Shader "Hidden/UI/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Offset", Float) = 1
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Offset;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * _Offset;
                fixed4 c = tex2D(_MainTex, i.uv + float2(o.x, o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x, o.y));
                c += tex2D(_MainTex, i.uv + float2(o.x, -o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x, -o.y));
                return c * 0.25;
            }
            ENDCG
        }
    }
}
