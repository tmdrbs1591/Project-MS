// Made with Amplify Shader Editor v1.9.9.4
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "LoadComplete/VFX/URP2D_FX_Master"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_Hue( "Hue", Range( -1, 1 ) ) = 0
		_Saturation( "Saturation", Range( 0, 2 ) ) = 1
		_Value( "Value", Range( 0, 2 ) ) = 1
		[Enum(Additive,1,AlphaBlend,10)] _BlendMode( "BlendMode", Float ) = 10
		[Toggle] _FixedTiling( "FixedTiling", Float ) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode( "CullMode", Float ) = 0
		_AlphaClip( "AlphaClip", Range( 0, 1 ) ) = 0
		_Pixelate( "Pixelate", Int ) = 0
		[Enum(Normal UV,0,Border UV,1)] _UVSwitch( "UVSwitch", Float ) = 0
		_MainTex( "MainTex", 2D ) = "white" {}
		[Enum(A,0,R,1)] _Main_Alpha_Ch( "Main_Alpha_Ch", Float ) = 0
		_Main_Color( "Main_Color", Color ) = ( 1, 1, 1, 1 )
		[Enum(MainTex Color,0,R Channel Color,1,ParticleCustom 1W,2)] _MainTex_ColorMode( "MainTex_ColorMode", Float ) = 0
		[Enum(property,0,Custom 1W,1)] _Emission_Mode( "Emission_Mode", Float ) = 0
		_Emission( "Emission", Float ) = 0
		[ToggleUI] _Tiling_ParticleCustom1XY( "Tiling_ParticleCustom1XY", Float ) = 0
		_Main_Rotate( "Main_Rotate", Float ) = 0
		[ToggleUI] _Main_Radial( "Main_Radial", Float ) = 0
		_Main_Radial_Tiling( "Main_Radial_Tiling", Vector ) = ( 1, 1, 0, 0 )
		[ToggleUI] _Main_Pan_ParticleCustom1Z( "Main_Pan_ParticleCustom1Z", Float ) = 1
		_Main_Panning( "Main_Panning", Vector ) = ( 0, 0, 0, 0 )
		[Header(___________________Deform___________________)][Space(5)][Toggle( _DEFORM_USE_ON )] _Deform_Use( "Deform_Use", Float ) = 0
		[Enum(Add,0,Lerp,1)] _DeformType( "DeformType", Int ) = 0
		_DeformTex( "DeformTex", 2D ) = "bump" {}
		_Deform_Rotate( "Deform_Rotate", Float ) = 0
		[ToggleUI] _Deform_Radial( "Deform_Radial", Float ) = 0
		_Deform_Radial_Tiling( "Deform_Radial_Tiling", Vector ) = ( 1, 1, 0, 0 )
		[ToggleUI] _Deform_Str_ParticleCustom2X( "Deform_Str_ParticleCustom2X", Float ) = 1
		_Deform_Strength( "Deform_Strength", Float ) = 0
		[ToggleUI] _Deform_Pan_ParticleCustom2Y( "Deform_Pan_ParticleCustom2Y", Float ) = 1
		_Deform_Panning( "Deform_Panning", Vector ) = ( 0, 0, 0, 0 )
		[Enum(Linear,0,Beam,1,Radial,2,Ring,3)][Space (12)] _DeformMask_Type( "DeformMask_Type", Int ) = 0
		_Deform_Mask_OffsetStrength( "Deform_Mask_Offset/Strength", Vector ) = ( 0, 0, 0, 0 )
		_Deform_Mask_Smooth( "Deform_Mask_Smooth", Range( 0, 1 ) ) = 0
		_Deform_Mask_Rotate( "Deform_Mask_Rotate", Float ) = 0
		[Header(___________________Dissolve___________________)][Space(5)][Toggle( _DISSOLVE_USE_ON )] _Dissolve_Use( "Dissolve_Use", Float ) = 0
		_DissolveTex( "DissolveTex", 2D ) = "white" {}
		[ToggleUI] _DissolveTex_Reverse( "DissolveTex_Reverse", Float ) = 0
		_Dissolve_Rotate( "Dissolve_Rotate", Float ) = 0
		[ToggleUI] _Dissolve_Radial( "Dissolve_Radial", Float ) = 0
		_Dissolve_Radial_Tiling( "Dissolve_Radial_Tiling", Vector ) = ( 1, 1, 0, 0 )
		[ToggleUI] _Dissolve_Prog_ParticleCustom2W( "Dissolve_Prog_ParticleCustom2W", Float ) = 1
		_Dissolve_Progress( "Dissolve_Progress", Range( 0, 1 ) ) = 1
		_Dissolve_smooth( "Dissolve_smooth", Range( 0, 1 ) ) = 0
		[Enum(ParticleCustom2 Z,0,With Deform,1,Use Prop Auto,2)] _Dissolve_Panning_Type( "Dissolve_Panning_Type", Float ) = 0
		_Dissolve_Panning( "Dissolve_Panning", Vector ) = ( 0, 0, 0, 0 )
		[ToggleUI] _Use_Dissolve_Edge( "Use_Dissolve_Edge", Float ) = 0
		[HDR] _Dissolve_Edge_Color( "Dissolve_Edge_Color", Color ) = ( 4, 0.9547434, 0.2641504, 1 )
		_Dissolve_Edge_Thick( "Dissolve_Edge_Thick", Float ) = 0.02
		_Dissolve_Edge_smooth( "Dissolve_Edge_smooth", Range( 0, 1 ) ) = 0.9
		[Enum(Linear,0,Beam,1,Radial,2)][Space (12)] _DissolveMask_Type( "DissolveMask_Type", Int ) = 0
		[Enum(Add,0,Multiply,1)] _DissolveMask_BlendType( "DissolveMask_BlendType", Int ) = 0
		_Dissolve_Mask_OffsetStrength( "Dissolve_Mask_Offset/Strength", Vector ) = ( 0, 0, 0, 0 )
		_Dissolve_Mask_Smooth( "Dissolve_Mask_Smooth", Range( 0, 1 ) ) = 0
		_Dissolve_Mask_Rotate( "Dissolve_Mask_Rotate", Float ) = 0
		[HideInInspector][Toggle] _ERRORTOGGLE( "ERRORTOGGLE", Float ) = 0
		[HideInInspector] _SpriteBorder( "SpriteBorder", Vector ) = ( 0, 0, 0, 0 )
		[HideInInspector] _OriginalSize( "OriginalSize", Vector ) = ( 0, 0, 0, 0 )
		[Header(___________________PixelFresnel___________________)][Space (8)][Toggle] _Fresnel_AlphaClip( "Fresnel_AlphaClip", Float ) = 0
		_Fresnel_AlphaClipPixelate( "Fresnel_AlphaClipPixelate", Range( 1, 4 ) ) = 4
		_Fresnel_AlphaClipPower( "Fresnel_AlphaClipPower", Range( 0, 4 ) ) = 1.5
		_Fresnel_AlphaClipStepMin( "Fresnel_AlphaClipStepMin", Range( 0, 1 ) ) = 0.05
		_Fresnel_AlphaClipStepMax( "Fresnel_AlphaClipStepMax", Range( 0, 1 ) ) = 0.6125
		[Header(___________________Mask___________________)][Space(5)][Toggle( _MASK_USE_ON )] _Mask_Use( "Mask_Use", Float ) = 0
		_Mask_Tex( "Mask_Tex", 2D ) = "white" {}
		_Mask_Rotate( "Mask_Rotate", Float ) = 0
		[Enum(Property,0,Multiply_Dissolve_Progress,1)] _Mask_Strength_Mode( "Mask_Strength_Mode", Float ) = 0
		[Enum(A,0,R,1)] _Mask_Alpha_Ch( "Mask_Alpha_Ch", Float ) = 1
		_Mask_Contrast( "Mask_Contrast", Float ) = 1
		[Enum(Multiply,0,Add,1)] _Mask_BlendMode( "Mask_BlendMode", Float ) = 0
		_Mask_Strength( "Mask_Strength", Float ) = 1
		_Mask_Smooth( "Mask_Smooth", Range( 0, 1 ) ) = 0
		_Mask_Scale( "Mask_Scale", Float ) = 1
		_Mask_ScaleOffset( "Mask_ScaleOffset", Float ) = 0

		[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		LOD 0

		

        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "UniversalMaterialType"="Lit" "Queue"="Transparent" "ShaderGraphShader"="true" }

		Cull [_CullMode]
		BlendOp Add, Max
		Blend SrcAlpha [_BlendMode], One OneMinusSrcAlpha
		ZTest LEqual
		ZWrite Off
		Offset 0 , 0
		ColorMask RGBA
		

		HLSLINCLUDE
		#pragma target 2.0
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		ENDHLSL

		
		Pass
		{
			
			Name "Sprite Lit"
            Tags { "LightMode"="Universal2D" }

			HLSLPROGRAM

			#define ASE_VERSION 19904
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_SCREENPOSITION
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITELIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES3
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_SCREEN_POSITION_NORMALIZED
			#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED
			#pragma shader_feature_local _DEFORM_USE_ON
			#pragma shader_feature_local _DISSOLVE_USE_ON
			#pragma shader_feature_local _MASK_USE_ON


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				float4 screenPosition : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
			};

			sampler2D _MainTex;
			sampler2D _DeformTex;
			sampler2D _DissolveTex;
			sampler2D _Mask_Tex;
			CBUFFER_START( UnityPerMaterial )
			float4 _MainTex_ST;
			float4 _DeformTex_ST;
			float4 _Mask_Tex_ST;
			float4 _Main_Color;
			float4 _Dissolve_Edge_Color;
			float4 _DissolveTex_ST;
			float4 _SpriteBorder;
			float4 _OriginalSize;
			float2 _Deform_Radial_Tiling;
			float2 _Deform_Panning;
			float2 _Main_Radial_Tiling;
			float2 _Dissolve_Mask_OffsetStrength;
			float2 _Dissolve_Radial_Tiling;
			float2 _Main_Panning;
			float2 _Dissolve_Panning;
			float2 _Deform_Mask_OffsetStrength;
			float _Dissolve_Radial;
			float _ERRORTOGGLE;
			float _Dissolve_Rotate;
			float _Dissolve_Panning_Type;
			float _DissolveTex_Reverse;
			float _Dissolve_Prog_ParticleCustom2W;
			float _Dissolve_Progress;
			float _Dissolve_Edge_Thick;
			float _Mask_Alpha_Ch;
			float _Mask_BlendMode;
			float _Mask_Smooth;
			float _Mask_Strength;
			float _Dissolve_Mask_Rotate;
			float _Mask_Rotate;
			float _Mask_Contrast;
			float _Mask_Scale;
			float _Mask_ScaleOffset;
			float _Mask_Strength_Mode;
			float _Main_Alpha_Ch;
			float _Dissolve_smooth;
			float _Fresnel_AlphaClip;
			float _Fresnel_AlphaClipStepMin;
			float _Fresnel_AlphaClipStepMax;
			float _AlphaClip;
			float _CullMode;
			float _Use_Dissolve_Edge;
			float _Dissolve_Mask_Smooth;
			float _BlendMode;
			float _MainTex_ColorMode;
			float _Main_Pan_ParticleCustom1Z;
			float _Main_Radial;
			float _Tiling_ParticleCustom1XY;
			float _FixedTiling;
			float _UVSwitch;
			int _DeformType;
			float _Deform_Pan_ParticleCustom2Y;
			float _Deform_Radial;
			int _Pixelate;
			float _Deform_Rotate;
			int _DissolveMask_Type;
			float _Deform_Str_ParticleCustom2X;
			float _Deform_Mask_Smooth;
			int _DeformMask_Type;
			float _Deform_Mask_Rotate;
			float _Main_Rotate;
			float _Hue;
			float _Saturation;
			float _Value;
			float _Emission_Mode;
			float _Emission;
			float _Fresnel_AlphaClipPower;
			float _Dissolve_Edge_smooth;
			int _DissolveMask_BlendType;
			float _Deform_Strength;
			float _Fresnel_AlphaClipPixelate;
			CBUFFER_END


			float3 HSVToRGB( float3 c )
			{
				float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
				float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
				return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
			}
			
			float3 RGBToHSV(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
				float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
				float d = q.x - min( q.w, q.y );
				float e = 1.0e-10;
				return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			float2 NineSliceUV514( float2 uv, float4 border, float2 currentSize, float2 originalSize )
			{
				 // currentSize = 파티클 크기 (TexCoord1.xy에서 받음)
				  // originalSize = Material Property로 스크립트에서 전달
				  // border = Material Property로 스크립트에서 전달 (0~1 normalized)
				  float2 scale = currentSize / max(originalSize, 0.001);
				  float2 sMin = border.xy / max(scale, 0.001);
				  float2 sMax = border.zw / max(scale, 0.001);
				  float2 eStart = sMin;
				  float2 eEnd = 1.0 - sMax;
				  float2 L = uv * scale;
				  float2 R = 1.0 - (1.0 - uv) * scale;
				  float2 M = lerp(border.xy, 1.0 - border.zw, saturate((uv - eStart) / max(eEnd - eStart, 0.001)));
				  float2 maskL = step(uv, eStart);
				  float2 maskR = step(eEnd, uv);
				  float2 maskM = 1.0 - maskL - maskR;
				  return L * maskL + R * maskR + M * maskM;
			}
			
			float2 DeformMaskType835( int DeformType, float2 Add, float2 Lerp )
			{
				int mode = (int)DeformType;
				if (mode == 0) return Add;
				else if (mode == 1) return Lerp;
				else return Add;
			}
			
			float DeformMaskType822( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float DissolveMaskType799( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float BlendType778( int MaskBlendType, float Add, float Multiply )
			{
				int mode = (int)MaskBlendType;
				if (mode == 0) return Add;
				else if (mode == 1) return Multiply;
				else return Add;
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float3 ase_normalWS = TransformObjectToWorldNormal( v.normal );
				o.ase_texcoord7.xyz = ase_normalWS;
				
				o.ase_texcoord4.xy = v.ase_texcoord3.xy;
				o.ase_texcoord5 = v.ase_texcoord1;
				o.ase_texcoord6 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord4.zw = 0;
				o.ase_texcoord7.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS.xyz = vertexInput.positionWS;
				o.texCoord0.xyzw = v.uv0;
				o.color.xyzw =  v.color;
				o.screenPosition.xyzw = vertexInput.positionNDC;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float2 texCoord493 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv514 = texCoord493;
				float4 border514 = _SpriteBorder;
				float2 texCoord517 = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float2 currentSize514 = texCoord517;
				float2 originalSize514 = _OriginalSize.xy;
				float2 localNineSliceUV514 = NineSliceUV514( uv514 , border514 , currentSize514 , originalSize514 );
				float2 temp_output_454_0 = ( ( _UVSwitch != 1.0 ? texCoord493 : localNineSliceUV514 ) * _MainTex_ST.xy );
				float2 temp_output_457_0 = ( temp_output_454_0 + _MainTex_ST.zw );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 ObjectScaleXY396 = (ase_objectScale).xy;
				float2 texCoord453 = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_456_0 = ( ( temp_output_454_0 * texCoord453 ) + _MainTex_ST.zw );
				float2 temp_output_34_0_g6 = ( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) - float2( 0.5,0.5 ) );
				float2 break39_g6 = temp_output_34_0_g6;
				float2 appendResult50_g6 = (float2(( _Main_Radial_Tiling.y * ( length( temp_output_34_0_g6 ) * 2.0 ) ) , ( ( atan2( break39_g6.x , break39_g6.y ) * ( 1.0 / TWO_PI ) ) * _Main_Radial_Tiling.x )));
				int DeformType835 = _DeformType;
				float2 texCoord438 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_478_0 = ( _Pixelate * 1.0 );
				float temp_output_437_0 = max( temp_output_478_0 , 2.0 );
				half2 pixelateduv440 = floor( texCoord438 * float2( temp_output_437_0, temp_output_437_0 ) + float2( 0,0 ) ) / float2( temp_output_437_0, temp_output_437_0 );
				float2 lerpResult441 = lerp( texCoord438 , pixelateduv440 , saturate( step( 1.0 , abs( temp_output_478_0 ) ) ));
				float2 PixelUVBase442 = lerpResult441;
				float2 temp_output_446_0 = ( ( PixelUVBase442 * _DeformTex_ST.xy ) + _DeformTex_ST.zw );
				float2 temp_output_34_0_g4 = ( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g4 = temp_output_34_0_g4;
				float2 appendResult50_g4 = (float2(( _Deform_Radial_Tiling.y * ( length( temp_output_34_0_g4 ) * 2.0 ) ) , ( ( atan2( break39_g4.x , break39_g4.y ) * ( 1.0 / TWO_PI ) ) * _Deform_Radial_Tiling.x )));
				float2 panner133 = ( 1.0 * _Time.y * _Deform_Panning + (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )));
				float4 texCoord223 = IN.ase_texcoord6;
				texCoord223.xy = IN.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float cos212 = cos(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin212 = sin(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator212 = mul( (( _Deform_Pan_ParticleCustom2Y )?( ( (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )) + ( texCoord223.y * _Deform_Panning ) ) ):( panner133 )) - float2( 0.5,0.5 ) , float2x2( cos212 , -sin212 , sin212 , cos212 )) + float2( 0.5,0.5 );
				float2 clampResult848 = clamp( rotator212 , float2( 0.001,0.001 ) , float2( 0.999,0.999 ) );
				int DeformTypeSwitch841 = _DeformType;
				float2 lerpResult847 = lerp( rotator212 , clampResult848 , (float)DeformTypeSwitch841);
				float2 temp_output_122_0 = (tex2D( _DeformTex, lerpResult847 )).rg;
				float2 temp_output_138_0 = ( temp_output_122_0 - float2( 0.5,0.5 ) );
				float2 break200 = temp_output_138_0;
				float4 texCoord208 = IN.ase_texcoord6;
				texCoord208.xy = IN.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Deform165 = ( temp_output_138_0 * ( break200.x * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) * ( break200.y * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) );
				float2 Add835 = ( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) + Deform165 );
				float2 DeformTex829 = temp_output_122_0;
				float Deform_Strength830 = (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength ));
				float2 lerpResult831 = lerp( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , DeformTex829 , Deform_Strength830);
				float2 Lerp835 = lerpResult831;
				float2 localDeformMaskType835 = DeformMaskType835( DeformType835 , Add835 , Lerp835 );
				float lerpResult337 = lerp( 0.0 , 0.5 , _Deform_Mask_Smooth);
				int MaskType822 = _DeformMask_Type;
				float2 temp_cast_2 = (_Deform_Mask_OffsetStrength.x).xx;
				float2 texCoord329 = IN.texCoord0.xy * float2( 1,1 ) + temp_cast_2;
				float cos349 = cos(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin349 = sin(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator349 = mul( (( _FixedTiling )?( ( texCoord329 * ObjectScaleXY396 ) ):( texCoord329 )) - float2( 0.5,0.5 ) , float2x2( cos349 , -sin349 , sin349 , cos349 )) + float2( 0.5,0.5 );
				float Linear822 = (( rotator349 * _Deform_Mask_OffsetStrength.y )).x;
				float Beam822 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator349 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Radial822 = saturate( ( ( 1.0 - ( distance( rotator349 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Ring822 = 0.0;
				float localDeformMaskType822 = DeformMaskType822( MaskType822 , Linear822 , Beam822 , Radial822 , Ring822 );
				float smoothstepResult334 = smoothstep( lerpResult337 , ( 1.0 - lerpResult337 ) , localDeformMaskType822);
				float Deform_Mask352 = smoothstepResult334;
				float2 lerpResult356 = lerp( localDeformMaskType835 , (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , Deform_Mask352);
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch125 = lerpResult356;
				#else
				float2 staticSwitch125 = (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) ));
				#endif
				float2 panner288 = ( 1.0 * _Time.y * _Main_Panning + staticSwitch125);
				float4 texCoord285 = IN.ase_texcoord5;
				texCoord285.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float cos109 = cos(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin109 = sin(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator109 = mul( (( _Main_Pan_ParticleCustom1Z )?( ( staticSwitch125 + ( texCoord285.z * _Main_Panning ) ) ):( panner288 )) - float2( 0.5,0.5 ) , float2x2( cos109 , -sin109 , sin109 , cos109 )) + float2( 0.5,0.5 );
				float Pixelate463 = temp_output_478_0;
				float temp_output_496_0 = max( Pixelate463 , 2.0 );
				half2 pixelateduv497 = floor( rotator109 * float2( temp_output_496_0, temp_output_496_0 ) + float2( 0,0 ) ) / float2( temp_output_496_0, temp_output_496_0 );
				float2 lerpResult499 = lerp( rotator109 , pixelateduv497 , saturate( step( 1.0 , abs( Pixelate463 ) ) ));
				float4 tex2DNode13 = tex2D( _MainTex, lerpResult499 );
				float3 hsvTorgb3_g7 = RGBToHSV( tex2DNode13.rgb );
				float3 hsvTorgb10_g7 = HSVToRGB( float3(frac( ( hsvTorgb3_g7.x + _Hue ) ),saturate( ( hsvTorgb3_g7.y * _Saturation ) ),( hsvTorgb3_g7.z * _Value )) );
				float3 temp_output_893_0 = hsvTorgb10_g7;
				float3 temp_cast_3 = (tex2DNode13.r).xxx;
				float3 temp_cast_4 = (tex2DNode13.r).xxx;
				float4 texCoord369 = IN.ase_texcoord5;
				texCoord369.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float3 lerpResult368 = lerp( temp_output_893_0 , temp_cast_4 , texCoord369.w);
				float3 ifLocalVar370 = 0;
				if( 1.0 > _MainTex_ColorMode )
				ifLocalVar370 = temp_output_893_0;
				else if( 1.0 == _MainTex_ColorMode )
				ifLocalVar370 = temp_cast_3;
				else if( 1.0 < _MainTex_ColorMode )
				ifLocalVar370 = lerpResult368;
				float4 texCoord899 = IN.ase_texcoord5;
				texCoord899.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult904 = clamp( ( _Emission_Mode == 1.0 ? texCoord899.w : _Emission ) , 1.0 , 999.0 );
				float lerpResult306 = lerp( 0.0 , 0.5 , _Dissolve_Edge_smooth);
				int MaskBlendType778 = _DissolveMask_BlendType;
				float lerpResult382 = lerp( 0.0 , 0.5 , _Dissolve_Mask_Smooth);
				int MaskType799 = _DissolveMask_Type;
				float2 temp_cast_5 = (_Dissolve_Mask_OffsetStrength.x).xx;
				float2 texCoord386 = IN.texCoord0.xy * float2( 1,1 ) + temp_cast_5;
				float cos377 = cos(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin377 = sin(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator377 = mul( (( _FixedTiling )?( ( texCoord386 * ObjectScaleXY396 ) ):( texCoord386 )) - float2( 0.5,0.5 ) , float2x2( cos377 , -sin377 , sin377 , cos377 )) + float2( 0.5,0.5 );
				float Linear799 = (( rotator377 * _Dissolve_Mask_OffsetStrength.y )).x;
				float Beam799 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator377 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Radial799 = saturate( ( ( 1.0 - ( distance( rotator377 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Ring799 = 0.0;
				float localDissolveMaskType799 = DissolveMaskType799( MaskType799 , Linear799 , Beam799 , Radial799 , Ring799 );
				float smoothstepResult384 = smoothstep( lerpResult382 , ( 1.0 - lerpResult382 ) , localDissolveMaskType799);
				float Dissolve_Mask385 = smoothstepResult384;
				float2 temp_output_450_0 = ( ( PixelUVBase442 * _DissolveTex_ST.xy ) + _DissolveTex_ST.zw );
				float2 temp_output_34_0_g5 = ( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g5 = temp_output_34_0_g5;
				float2 appendResult50_g5 = (float2(( _Dissolve_Radial_Tiling.y * ( length( temp_output_34_0_g5 ) * 2.0 ) ) , ( ( atan2( break39_g5.x , break39_g5.y ) * ( 1.0 / TWO_PI ) ) * _Dissolve_Radial_Tiling.x )));
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch279 = ( (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) )) + Deform165 );
				#else
				float2 staticSwitch279 = (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) ));
				#endif
				float2 panner234 = ( 1.0 * _Time.y * _Dissolve_Panning + staticSwitch279);
				float2 Deform_Panning272 = _Deform_Panning;
				float2 panner273 = ( 1.0 * _Time.y * Deform_Panning272 + staticSwitch279);
				float4 texCoord227 = IN.ase_texcoord6;
				texCoord227.xy = IN.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float cos242 = cos(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin242 = sin(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator242 = mul(  ( _Dissolve_Panning_Type - 0.0 > 1.0 ? panner234 : _Dissolve_Panning_Type - 0.0 <= 1.0 && _Dissolve_Panning_Type + 0.0 >= 1.0 ? (( _Deform_Pan_ParticleCustom2Y )?( ( staticSwitch279 + ( texCoord227.y * Deform_Panning272 ) ) ):( panner273 )) : ( staticSwitch279 + ( texCoord227.z * _Dissolve_Panning ) ) )  - float2( 0.5,0.5 ) , float2x2( cos242 , -sin242 , sin242 , cos242 )) + float2( 0.5,0.5 );
				float4 tex2DNode209 = tex2D( _DissolveTex, rotator242 );
				float Add778 = ( Dissolve_Mask385 + (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float Multiply778 = ( Dissolve_Mask385 * (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float localBlendType778 = BlendType778( MaskBlendType778 , Add778 , Multiply778 );
				float4 texCoord252 = IN.ase_texcoord6;
				texCoord252.xy = IN.ase_texcoord6.xy * float2( 1,1 ) + float2( 0,0 );
				float lerpResult250 = lerp( -1.0 , 1.0 , (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress )));
				float temp_output_254_0 = ( saturate( localBlendType778 ) + lerpResult250 );
				float Dissolve_Before_Smooth298 = temp_output_254_0;
				float smoothstepResult304 = smoothstep( lerpResult306 , ( 1.0 - lerpResult306 ) , ( 1.0 - ( Dissolve_Before_Smooth298 - _Dissolve_Edge_Thick ) ));
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch310 = smoothstepResult304;
				#else
				float staticSwitch310 = 0.0;
				#endif
				float3 lerpResult299 = lerp( ( ( (IN.color).rgb * ifLocalVar370 * _Main_Color.rgb ) * clampResult904 ) , _Dissolve_Edge_Color.rgb , (( _Use_Dissolve_Edge )?( staticSwitch310 ):( 0.0 )));
				
				float lerpResult362 = lerp( 0.0 , 0.5 , _AlphaClip);
				float lerpResult868 = lerp( 0.0 , 0.5 , _Mask_Smooth);
				float2 uv_Mask_Tex = IN.texCoord0.xy * _Mask_Tex_ST.xy + _Mask_Tex_ST.zw;
				float cos908 = cos(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin908 = sin(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator908 = mul( uv_Mask_Tex - float2( 0.5,0.5 ) , float2x2( cos908 , -sin908 , sin908 , cos908 )) + float2( 0.5,0.5 );
				float4 tex2DNode856 = tex2D( _Mask_Tex, rotator908 );
				float Dissolve_Progress_ref880 = (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress ));
				float smoothstepResult870 = smoothstep( lerpResult868 , ( 1.0 - lerpResult868 ) , ( _Mask_Strength * (pow( ( _Mask_Alpha_Ch == 1.0 ? tex2DNode856.r : tex2DNode856.a ) , _Mask_Contrast )*_Mask_Scale + _Mask_ScaleOffset) * ( _Mask_Strength_Mode == 0.0 ? 1.0 : Dissolve_Progress_ref880 ) ));
				float Mask859 = smoothstepResult870;
				#ifdef _MASK_USE_ON
				float staticSwitch861 = Mask859;
				#else
				float staticSwitch861 = 1.0;
				#endif
				float lerpResult260 = lerp( 0.0 , 0.5 , _Dissolve_smooth);
				float smoothstepResult258 = smoothstep( lerpResult260 , ( 1.0 - lerpResult260 ) , temp_output_254_0);
				float Dissolve264 = saturate( smoothstepResult258 );
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch266 = Dissolve264;
				#else
				float staticSwitch266 = 1.0;
				#endif
				float temp_output_878_0 = ( saturate( ( IN.color.a * _Main_Color.a * ( _Main_Alpha_Ch == 1.0 ? tex2DNode13.r : tex2DNode13.a ) ) ) * staticSwitch266 );
				float temp_output_874_0 = saturate( ( _Mask_BlendMode == 0.0 ? ( staticSwitch861 * temp_output_878_0 ) : ( staticSwitch861 + temp_output_878_0 ) ) );
				float smoothstepResult364 = smoothstep( lerpResult362 , ( 1.0 - lerpResult362 ) , temp_output_874_0);
				float lerpResult365 = lerp( smoothstepResult364 , temp_output_874_0 , step( _AlphaClip , 1E-05 ));
				float3 ase_normalWS = IN.ase_texcoord7.xyz;
				float dotResult703 = dot( ase_normalWS , -UNITY_MATRIX_V[ 2 ].xyz );
				float smoothstepResult750 = smoothstep( _Fresnel_AlphaClipStepMin , _Fresnel_AlphaClipStepMax , pow( abs( dotResult703 ) , _Fresnel_AlphaClipPower ));
				float temp_output_747_0 = ( 1.0 / _Fresnel_AlphaClipPixelate );
				float temp_output_745_0 = ( temp_output_747_0 * -1.0 );
				float clampResult746 = clamp( ddx( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float2 appendResult708 = (float2(IN.screenPosition.x , IN.screenPosition.y));
				float2 appendResult720 = (float2(_ScaledScreenParams.x , _ScaledScreenParams.y));
				float2 temp_output_718_0 = ( appendResult708 * appendResult720 );
				float temp_output_771_0 = ( ( _Fresnel_AlphaClipPixelate * ( _ScaledScreenParams.x / 1920.0 ) ) / ( unity_OrthoParams.y / 10.5 ) );
				float2 break711 = ( floor( temp_output_718_0 ) - ( floor( ( temp_output_718_0 / temp_output_771_0 ) ) * temp_output_771_0 ) );
				float clampResult748 = clamp( ddy( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float FresnelAlphaClip631 = (( _Fresnel_AlphaClip )?( step( 0.5 , ( step( 0.05 , smoothstepResult750 ) * ( ( smoothstepResult750 - ( clampResult746 * break711.x ) ) - ( clampResult748 * break711.y ) ) ) ) ):( 1.0 ));
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.BaseColor = lerpResult299;
				surfaceDescription.Alpha = ( lerpResult365 * FresnelAlphaClip631 );

				half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);

				#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(color.rgb, color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, positionWS, positionCS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
				#endif

				//***���⼭���� Premultiplied�� ���� Ŀ���� ó��***
				//BlendOp Add, Max ����� �� ����ؼ� �ӽ÷� ���ܵ�
				//�� ������ �Ϸ��� Blend RGB�� Premultiplied �� �ϰ� Dst�� _BlendMode ������ �״��

				//color.rgb *= color.a; // ���� �ռ��� ���� ���ĸ� RGB�� �̸� ���� (Premultiplied)

				// ����ũ�� ���� ���� �ٽ�: 
				// Additive(���ϱ�) ����� ���� ���� ���� 0���� ����� ������ ������� ������ ���� �ʰ� �ϰ�,
				// AlphaBlend ����� ���� �ּ����� ���ü��� ���� ���ĸ� �����ϰų� 1�� ������ �� �ֽ��ϴ�.
				//if (_BlendMode == 1.0) // Additive ����� ���
				//{
				//	color.a = 0; // RGB�� ��������Ƿ� ���� ��濡 ����������, �����츦 �������ϰ� ������ ����
				//}
				
				// color *= unity_SpriteColor; //������ ������ �������⿡ ����
				return color;
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "Sprite Normal"
            Tags { "LightMode"="NormalsRendering" }

			HLSLPROGRAM

			#define ASE_VERSION 19904
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SKINNED_SPRITE

			#define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITENORMAL

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES3
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#pragma shader_feature_local _MASK_USE_ON
			#pragma shader_feature_local _DEFORM_USE_ON
			#pragma shader_feature_local _DISSOLVE_USE_ON


			sampler2D _Mask_Tex;
			sampler2D _MainTex;
			sampler2D _DeformTex;
			sampler2D _DissolveTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _MainTex_ST;
			float4 _DeformTex_ST;
			float4 _Mask_Tex_ST;
			float4 _Main_Color;
			float4 _Dissolve_Edge_Color;
			float4 _DissolveTex_ST;
			float4 _SpriteBorder;
			float4 _OriginalSize;
			float2 _Deform_Radial_Tiling;
			float2 _Deform_Panning;
			float2 _Main_Radial_Tiling;
			float2 _Dissolve_Mask_OffsetStrength;
			float2 _Dissolve_Radial_Tiling;
			float2 _Main_Panning;
			float2 _Dissolve_Panning;
			float2 _Deform_Mask_OffsetStrength;
			float _Dissolve_Radial;
			float _ERRORTOGGLE;
			float _Dissolve_Rotate;
			float _Dissolve_Panning_Type;
			float _DissolveTex_Reverse;
			float _Dissolve_Prog_ParticleCustom2W;
			float _Dissolve_Progress;
			float _Dissolve_Edge_Thick;
			float _Mask_Alpha_Ch;
			float _Mask_BlendMode;
			float _Mask_Smooth;
			float _Mask_Strength;
			float _Dissolve_Mask_Rotate;
			float _Mask_Rotate;
			float _Mask_Contrast;
			float _Mask_Scale;
			float _Mask_ScaleOffset;
			float _Mask_Strength_Mode;
			float _Main_Alpha_Ch;
			float _Dissolve_smooth;
			float _Fresnel_AlphaClip;
			float _Fresnel_AlphaClipStepMin;
			float _Fresnel_AlphaClipStepMax;
			float _AlphaClip;
			float _CullMode;
			float _Use_Dissolve_Edge;
			float _Dissolve_Mask_Smooth;
			float _BlendMode;
			float _MainTex_ColorMode;
			float _Main_Pan_ParticleCustom1Z;
			float _Main_Radial;
			float _Tiling_ParticleCustom1XY;
			float _FixedTiling;
			float _UVSwitch;
			int _DeformType;
			float _Deform_Pan_ParticleCustom2Y;
			float _Deform_Radial;
			int _Pixelate;
			float _Deform_Rotate;
			int _DissolveMask_Type;
			float _Deform_Str_ParticleCustom2X;
			float _Deform_Mask_Smooth;
			int _DeformMask_Type;
			float _Deform_Mask_Rotate;
			float _Main_Rotate;
			float _Hue;
			float _Saturation;
			float _Value;
			float _Emission_Mode;
			float _Emission;
			float _Fresnel_AlphaClipPower;
			float _Dissolve_Edge_smooth;
			int _DissolveMask_BlendType;
			float _Deform_Strength;
			float _Fresnel_AlphaClipPixelate;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 tangentWS : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 NormalTS;
				float Alpha;
			};

			float2 NineSliceUV514( float2 uv, float4 border, float2 currentSize, float2 originalSize )
			{
				 // currentSize = 파티클 크기 (TexCoord1.xy에서 받음)
				  // originalSize = Material Property로 스크립트에서 전달
				  // border = Material Property로 스크립트에서 전달 (0~1 normalized)
				  float2 scale = currentSize / max(originalSize, 0.001);
				  float2 sMin = border.xy / max(scale, 0.001);
				  float2 sMax = border.zw / max(scale, 0.001);
				  float2 eStart = sMin;
				  float2 eEnd = 1.0 - sMax;
				  float2 L = uv * scale;
				  float2 R = 1.0 - (1.0 - uv) * scale;
				  float2 M = lerp(border.xy, 1.0 - border.zw, saturate((uv - eStart) / max(eEnd - eStart, 0.001)));
				  float2 maskL = step(uv, eStart);
				  float2 maskR = step(eEnd, uv);
				  float2 maskM = 1.0 - maskL - maskR;
				  return L * maskL + R * maskR + M * maskM;
			}
			
			float2 DeformMaskType835( int DeformType, float2 Add, float2 Lerp )
			{
				int mode = (int)DeformType;
				if (mode == 0) return Add;
				else if (mode == 1) return Lerp;
				else return Add;
			}
			
			float DeformMaskType822( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float DissolveMaskType799( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float BlendType778( int MaskBlendType, float Add, float Multiply )
			{
				int mode = (int)MaskBlendType;
				if (mode == 0) return Add;
				else if (mode == 1) return Multiply;
				else return Add;
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord5 = screenPos;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord3 = v.ase_texcoord2;
				o.ase_color = v.ase_color;
				o.ase_texcoord2.zw = v.ase_texcoord3.xy;
				o.ase_texcoord4 = v.ase_texcoord1;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				float3 positionWS = TransformObjectToWorld(v.positionOS);
				float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);

				o.positionCS = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  -GetViewForwardDir();
				o.tangentWS.xyzw =  tangentWS;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float lerpResult362 = lerp( 0.0 , 0.5 , _AlphaClip);
				float lerpResult868 = lerp( 0.0 , 0.5 , _Mask_Smooth);
				float2 uv_Mask_Tex = IN.ase_texcoord2.xy * _Mask_Tex_ST.xy + _Mask_Tex_ST.zw;
				float cos908 = cos(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin908 = sin(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator908 = mul( uv_Mask_Tex - float2( 0.5,0.5 ) , float2x2( cos908 , -sin908 , sin908 , cos908 )) + float2( 0.5,0.5 );
				float4 tex2DNode856 = tex2D( _Mask_Tex, rotator908 );
				float4 texCoord252 = IN.ase_texcoord3;
				texCoord252.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float Dissolve_Progress_ref880 = (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress ));
				float smoothstepResult870 = smoothstep( lerpResult868 , ( 1.0 - lerpResult868 ) , ( _Mask_Strength * (pow( ( _Mask_Alpha_Ch == 1.0 ? tex2DNode856.r : tex2DNode856.a ) , _Mask_Contrast )*_Mask_Scale + _Mask_ScaleOffset) * ( _Mask_Strength_Mode == 0.0 ? 1.0 : Dissolve_Progress_ref880 ) ));
				float Mask859 = smoothstepResult870;
				#ifdef _MASK_USE_ON
				float staticSwitch861 = Mask859;
				#else
				float staticSwitch861 = 1.0;
				#endif
				float2 texCoord493 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv514 = texCoord493;
				float4 border514 = _SpriteBorder;
				float2 texCoord517 = IN.ase_texcoord2.zw * float2( 1,1 ) + float2( 0,0 );
				float2 currentSize514 = texCoord517;
				float2 originalSize514 = _OriginalSize.xy;
				float2 localNineSliceUV514 = NineSliceUV514( uv514 , border514 , currentSize514 , originalSize514 );
				float2 temp_output_454_0 = ( ( _UVSwitch != 1.0 ? texCoord493 : localNineSliceUV514 ) * _MainTex_ST.xy );
				float2 temp_output_457_0 = ( temp_output_454_0 + _MainTex_ST.zw );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 ObjectScaleXY396 = (ase_objectScale).xy;
				float2 texCoord453 = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_456_0 = ( ( temp_output_454_0 * texCoord453 ) + _MainTex_ST.zw );
				float2 temp_output_34_0_g6 = ( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) - float2( 0.5,0.5 ) );
				float2 break39_g6 = temp_output_34_0_g6;
				float2 appendResult50_g6 = (float2(( _Main_Radial_Tiling.y * ( length( temp_output_34_0_g6 ) * 2.0 ) ) , ( ( atan2( break39_g6.x , break39_g6.y ) * ( 1.0 / TWO_PI ) ) * _Main_Radial_Tiling.x )));
				int DeformType835 = _DeformType;
				float2 texCoord438 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_478_0 = ( _Pixelate * 1.0 );
				float temp_output_437_0 = max( temp_output_478_0 , 2.0 );
				half2 pixelateduv440 = floor( texCoord438 * float2( temp_output_437_0, temp_output_437_0 ) + float2( 0,0 ) ) / float2( temp_output_437_0, temp_output_437_0 );
				float2 lerpResult441 = lerp( texCoord438 , pixelateduv440 , saturate( step( 1.0 , abs( temp_output_478_0 ) ) ));
				float2 PixelUVBase442 = lerpResult441;
				float2 temp_output_446_0 = ( ( PixelUVBase442 * _DeformTex_ST.xy ) + _DeformTex_ST.zw );
				float2 temp_output_34_0_g4 = ( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g4 = temp_output_34_0_g4;
				float2 appendResult50_g4 = (float2(( _Deform_Radial_Tiling.y * ( length( temp_output_34_0_g4 ) * 2.0 ) ) , ( ( atan2( break39_g4.x , break39_g4.y ) * ( 1.0 / TWO_PI ) ) * _Deform_Radial_Tiling.x )));
				float2 panner133 = ( 1.0 * _Time.y * _Deform_Panning + (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )));
				float4 texCoord223 = IN.ase_texcoord3;
				texCoord223.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float cos212 = cos(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin212 = sin(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator212 = mul( (( _Deform_Pan_ParticleCustom2Y )?( ( (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )) + ( texCoord223.y * _Deform_Panning ) ) ):( panner133 )) - float2( 0.5,0.5 ) , float2x2( cos212 , -sin212 , sin212 , cos212 )) + float2( 0.5,0.5 );
				float2 clampResult848 = clamp( rotator212 , float2( 0.001,0.001 ) , float2( 0.999,0.999 ) );
				int DeformTypeSwitch841 = _DeformType;
				float2 lerpResult847 = lerp( rotator212 , clampResult848 , (float)DeformTypeSwitch841);
				float2 temp_output_122_0 = (tex2D( _DeformTex, lerpResult847 )).rg;
				float2 temp_output_138_0 = ( temp_output_122_0 - float2( 0.5,0.5 ) );
				float2 break200 = temp_output_138_0;
				float4 texCoord208 = IN.ase_texcoord3;
				texCoord208.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Deform165 = ( temp_output_138_0 * ( break200.x * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) * ( break200.y * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) );
				float2 Add835 = ( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) + Deform165 );
				float2 DeformTex829 = temp_output_122_0;
				float Deform_Strength830 = (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength ));
				float2 lerpResult831 = lerp( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , DeformTex829 , Deform_Strength830);
				float2 Lerp835 = lerpResult831;
				float2 localDeformMaskType835 = DeformMaskType835( DeformType835 , Add835 , Lerp835 );
				float lerpResult337 = lerp( 0.0 , 0.5 , _Deform_Mask_Smooth);
				int MaskType822 = _DeformMask_Type;
				float2 temp_cast_2 = (_Deform_Mask_OffsetStrength.x).xx;
				float2 texCoord329 = IN.ase_texcoord2.xy * float2( 1,1 ) + temp_cast_2;
				float cos349 = cos(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin349 = sin(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator349 = mul( (( _FixedTiling )?( ( texCoord329 * ObjectScaleXY396 ) ):( texCoord329 )) - float2( 0.5,0.5 ) , float2x2( cos349 , -sin349 , sin349 , cos349 )) + float2( 0.5,0.5 );
				float Linear822 = (( rotator349 * _Deform_Mask_OffsetStrength.y )).x;
				float Beam822 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator349 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Radial822 = saturate( ( ( 1.0 - ( distance( rotator349 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Ring822 = 0.0;
				float localDeformMaskType822 = DeformMaskType822( MaskType822 , Linear822 , Beam822 , Radial822 , Ring822 );
				float smoothstepResult334 = smoothstep( lerpResult337 , ( 1.0 - lerpResult337 ) , localDeformMaskType822);
				float Deform_Mask352 = smoothstepResult334;
				float2 lerpResult356 = lerp( localDeformMaskType835 , (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , Deform_Mask352);
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch125 = lerpResult356;
				#else
				float2 staticSwitch125 = (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) ));
				#endif
				float2 panner288 = ( 1.0 * _Time.y * _Main_Panning + staticSwitch125);
				float4 texCoord285 = IN.ase_texcoord4;
				texCoord285.xy = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float cos109 = cos(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin109 = sin(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator109 = mul( (( _Main_Pan_ParticleCustom1Z )?( ( staticSwitch125 + ( texCoord285.z * _Main_Panning ) ) ):( panner288 )) - float2( 0.5,0.5 ) , float2x2( cos109 , -sin109 , sin109 , cos109 )) + float2( 0.5,0.5 );
				float Pixelate463 = temp_output_478_0;
				float temp_output_496_0 = max( Pixelate463 , 2.0 );
				half2 pixelateduv497 = floor( rotator109 * float2( temp_output_496_0, temp_output_496_0 ) + float2( 0,0 ) ) / float2( temp_output_496_0, temp_output_496_0 );
				float2 lerpResult499 = lerp( rotator109 , pixelateduv497 , saturate( step( 1.0 , abs( Pixelate463 ) ) ));
				float4 tex2DNode13 = tex2D( _MainTex, lerpResult499 );
				float lerpResult260 = lerp( 0.0 , 0.5 , _Dissolve_smooth);
				int MaskBlendType778 = _DissolveMask_BlendType;
				float lerpResult382 = lerp( 0.0 , 0.5 , _Dissolve_Mask_Smooth);
				int MaskType799 = _DissolveMask_Type;
				float2 temp_cast_3 = (_Dissolve_Mask_OffsetStrength.x).xx;
				float2 texCoord386 = IN.ase_texcoord2.xy * float2( 1,1 ) + temp_cast_3;
				float cos377 = cos(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin377 = sin(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator377 = mul( (( _FixedTiling )?( ( texCoord386 * ObjectScaleXY396 ) ):( texCoord386 )) - float2( 0.5,0.5 ) , float2x2( cos377 , -sin377 , sin377 , cos377 )) + float2( 0.5,0.5 );
				float Linear799 = (( rotator377 * _Dissolve_Mask_OffsetStrength.y )).x;
				float Beam799 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator377 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Radial799 = saturate( ( ( 1.0 - ( distance( rotator377 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Ring799 = 0.0;
				float localDissolveMaskType799 = DissolveMaskType799( MaskType799 , Linear799 , Beam799 , Radial799 , Ring799 );
				float smoothstepResult384 = smoothstep( lerpResult382 , ( 1.0 - lerpResult382 ) , localDissolveMaskType799);
				float Dissolve_Mask385 = smoothstepResult384;
				float2 temp_output_450_0 = ( ( PixelUVBase442 * _DissolveTex_ST.xy ) + _DissolveTex_ST.zw );
				float2 temp_output_34_0_g5 = ( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g5 = temp_output_34_0_g5;
				float2 appendResult50_g5 = (float2(( _Dissolve_Radial_Tiling.y * ( length( temp_output_34_0_g5 ) * 2.0 ) ) , ( ( atan2( break39_g5.x , break39_g5.y ) * ( 1.0 / TWO_PI ) ) * _Dissolve_Radial_Tiling.x )));
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch279 = ( (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) )) + Deform165 );
				#else
				float2 staticSwitch279 = (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) ));
				#endif
				float2 panner234 = ( 1.0 * _Time.y * _Dissolve_Panning + staticSwitch279);
				float2 Deform_Panning272 = _Deform_Panning;
				float2 panner273 = ( 1.0 * _Time.y * Deform_Panning272 + staticSwitch279);
				float4 texCoord227 = IN.ase_texcoord3;
				texCoord227.xy = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float cos242 = cos(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin242 = sin(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator242 = mul(  ( _Dissolve_Panning_Type - 0.0 > 1.0 ? panner234 : _Dissolve_Panning_Type - 0.0 <= 1.0 && _Dissolve_Panning_Type + 0.0 >= 1.0 ? (( _Deform_Pan_ParticleCustom2Y )?( ( staticSwitch279 + ( texCoord227.y * Deform_Panning272 ) ) ):( panner273 )) : ( staticSwitch279 + ( texCoord227.z * _Dissolve_Panning ) ) )  - float2( 0.5,0.5 ) , float2x2( cos242 , -sin242 , sin242 , cos242 )) + float2( 0.5,0.5 );
				float4 tex2DNode209 = tex2D( _DissolveTex, rotator242 );
				float Add778 = ( Dissolve_Mask385 + (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float Multiply778 = ( Dissolve_Mask385 * (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float localBlendType778 = BlendType778( MaskBlendType778 , Add778 , Multiply778 );
				float lerpResult250 = lerp( -1.0 , 1.0 , (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress )));
				float temp_output_254_0 = ( saturate( localBlendType778 ) + lerpResult250 );
				float smoothstepResult258 = smoothstep( lerpResult260 , ( 1.0 - lerpResult260 ) , temp_output_254_0);
				float Dissolve264 = saturate( smoothstepResult258 );
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch266 = Dissolve264;
				#else
				float staticSwitch266 = 1.0;
				#endif
				float temp_output_878_0 = ( saturate( ( IN.ase_color.a * _Main_Color.a * ( _Main_Alpha_Ch == 1.0 ? tex2DNode13.r : tex2DNode13.a ) ) ) * staticSwitch266 );
				float temp_output_874_0 = saturate( ( _Mask_BlendMode == 0.0 ? ( staticSwitch861 * temp_output_878_0 ) : ( staticSwitch861 + temp_output_878_0 ) ) );
				float smoothstepResult364 = smoothstep( lerpResult362 , ( 1.0 - lerpResult362 ) , temp_output_874_0);
				float lerpResult365 = lerp( smoothstepResult364 , temp_output_874_0 , step( _AlphaClip , 1E-05 ));
				float dotResult703 = dot( IN.normalWS , -UNITY_MATRIX_V[ 2 ].xyz );
				float smoothstepResult750 = smoothstep( _Fresnel_AlphaClipStepMin , _Fresnel_AlphaClipStepMax , pow( abs( dotResult703 ) , _Fresnel_AlphaClipPower ));
				float temp_output_747_0 = ( 1.0 / _Fresnel_AlphaClipPixelate );
				float temp_output_745_0 = ( temp_output_747_0 * -1.0 );
				float clampResult746 = clamp( ddx( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float4 screenPos = IN.ase_texcoord5;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float2 appendResult708 = (float2(ase_positionSSNorm.x , ase_positionSSNorm.y));
				float2 appendResult720 = (float2(_ScaledScreenParams.x , _ScaledScreenParams.y));
				float2 temp_output_718_0 = ( appendResult708 * appendResult720 );
				float temp_output_771_0 = ( ( _Fresnel_AlphaClipPixelate * ( _ScaledScreenParams.x / 1920.0 ) ) / ( unity_OrthoParams.y / 10.5 ) );
				float2 break711 = ( floor( temp_output_718_0 ) - ( floor( ( temp_output_718_0 / temp_output_771_0 ) ) * temp_output_771_0 ) );
				float clampResult748 = clamp( ddy( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float FresnelAlphaClip631 = (( _Fresnel_AlphaClip )?( step( 0.5 , ( step( 0.05 , smoothstepResult750 ) * ( ( smoothstepResult750 - ( clampResult746 * break711.x ) ) - ( clampResult748 * break711.y ) ) ) ) ):( 1.0 ));
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.NormalTS = float3(0.0f, 0.0f, 1.0f);
				surfaceDescription.Alpha = ( lerpResult365 * FresnelAlphaClip631 );

				half crossSign = (IN.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
				half3 bitangent = crossSign * cross(IN.normalWS.xyz, IN.tangentWS.xyz);
				half4 color = half4(1.0,1.0,1.0, surfaceDescription.Alpha);

				return NormalsRenderingShared(color, surfaceDescription.NormalTS, IN.tangentWS.xyz, bitangent, IN.normalWS);
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }

            Cull Off
			Blend Off
			ZTest LEqual
			ZWrite On

            HLSLPROGRAM

			#define ASE_VERSION 19904
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES3
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _MASK_USE_ON
			#pragma shader_feature_local _DEFORM_USE_ON
			#pragma shader_feature_local _DISSOLVE_USE_ON


			sampler2D _Mask_Tex;
			sampler2D _MainTex;
			sampler2D _DeformTex;
			sampler2D _DissolveTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _MainTex_ST;
			float4 _DeformTex_ST;
			float4 _Mask_Tex_ST;
			float4 _Main_Color;
			float4 _Dissolve_Edge_Color;
			float4 _DissolveTex_ST;
			float4 _SpriteBorder;
			float4 _OriginalSize;
			float2 _Deform_Radial_Tiling;
			float2 _Deform_Panning;
			float2 _Main_Radial_Tiling;
			float2 _Dissolve_Mask_OffsetStrength;
			float2 _Dissolve_Radial_Tiling;
			float2 _Main_Panning;
			float2 _Dissolve_Panning;
			float2 _Deform_Mask_OffsetStrength;
			float _Dissolve_Radial;
			float _ERRORTOGGLE;
			float _Dissolve_Rotate;
			float _Dissolve_Panning_Type;
			float _DissolveTex_Reverse;
			float _Dissolve_Prog_ParticleCustom2W;
			float _Dissolve_Progress;
			float _Dissolve_Edge_Thick;
			float _Mask_Alpha_Ch;
			float _Mask_BlendMode;
			float _Mask_Smooth;
			float _Mask_Strength;
			float _Dissolve_Mask_Rotate;
			float _Mask_Rotate;
			float _Mask_Contrast;
			float _Mask_Scale;
			float _Mask_ScaleOffset;
			float _Mask_Strength_Mode;
			float _Main_Alpha_Ch;
			float _Dissolve_smooth;
			float _Fresnel_AlphaClip;
			float _Fresnel_AlphaClipStepMin;
			float _Fresnel_AlphaClipStepMax;
			float _AlphaClip;
			float _CullMode;
			float _Use_Dissolve_Edge;
			float _Dissolve_Mask_Smooth;
			float _BlendMode;
			float _MainTex_ColorMode;
			float _Main_Pan_ParticleCustom1Z;
			float _Main_Radial;
			float _Tiling_ParticleCustom1XY;
			float _FixedTiling;
			float _UVSwitch;
			int _DeformType;
			float _Deform_Pan_ParticleCustom2Y;
			float _Deform_Radial;
			int _Pixelate;
			float _Deform_Rotate;
			int _DissolveMask_Type;
			float _Deform_Str_ParticleCustom2X;
			float _Deform_Mask_Smooth;
			int _DeformMask_Type;
			float _Deform_Mask_Rotate;
			float _Main_Rotate;
			float _Hue;
			float _Saturation;
			float _Value;
			float _Emission_Mode;
			float _Emission;
			float _Fresnel_AlphaClipPower;
			float _Dissolve_Edge_smooth;
			int _DissolveMask_BlendType;
			float _Deform_Strength;
			float _Fresnel_AlphaClipPixelate;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            int _ObjectId;
            int _PassValue;

            struct SurfaceDescription
			{
				float Alpha;
			};

			float2 NineSliceUV514( float2 uv, float4 border, float2 currentSize, float2 originalSize )
			{
				 // currentSize = 파티클 크기 (TexCoord1.xy에서 받음)
				  // originalSize = Material Property로 스크립트에서 전달
				  // border = Material Property로 스크립트에서 전달 (0~1 normalized)
				  float2 scale = currentSize / max(originalSize, 0.001);
				  float2 sMin = border.xy / max(scale, 0.001);
				  float2 sMax = border.zw / max(scale, 0.001);
				  float2 eStart = sMin;
				  float2 eEnd = 1.0 - sMax;
				  float2 L = uv * scale;
				  float2 R = 1.0 - (1.0 - uv) * scale;
				  float2 M = lerp(border.xy, 1.0 - border.zw, saturate((uv - eStart) / max(eEnd - eStart, 0.001)));
				  float2 maskL = step(uv, eStart);
				  float2 maskR = step(eEnd, uv);
				  float2 maskM = 1.0 - maskL - maskR;
				  return L * maskL + R * maskR + M * maskM;
			}
			
			float2 DeformMaskType835( int DeformType, float2 Add, float2 Lerp )
			{
				int mode = (int)DeformType;
				if (mode == 0) return Add;
				else if (mode == 1) return Lerp;
				else return Add;
			}
			
			float DeformMaskType822( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float DissolveMaskType799( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float BlendType778( int MaskBlendType, float Add, float Multiply )
			{
				int mode = (int)MaskBlendType;
				if (mode == 0) return Add;
				else if (mode == 1) return Multiply;
				else return Add;
			}
			

			VertexOutput vert(VertexInput v )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float3 ase_normalWS = TransformObjectToWorldNormal( v.normal );
				o.ase_texcoord3.xyz = ase_normalWS;
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord4 = screenPos;
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				o.ase_texcoord1 = v.ase_texcoord2;
				o.ase_color = v.ase_color;
				o.ase_texcoord.zw = v.ase_texcoord3.xy;
				o.ase_texcoord2 = v.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				float3 positionWS = TransformObjectToWorld(v.positionOS);
				o.positionCS = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float lerpResult362 = lerp( 0.0 , 0.5 , _AlphaClip);
				float lerpResult868 = lerp( 0.0 , 0.5 , _Mask_Smooth);
				float2 uv_Mask_Tex = IN.ase_texcoord.xy * _Mask_Tex_ST.xy + _Mask_Tex_ST.zw;
				float cos908 = cos(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin908 = sin(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator908 = mul( uv_Mask_Tex - float2( 0.5,0.5 ) , float2x2( cos908 , -sin908 , sin908 , cos908 )) + float2( 0.5,0.5 );
				float4 tex2DNode856 = tex2D( _Mask_Tex, rotator908 );
				float4 texCoord252 = IN.ase_texcoord1;
				texCoord252.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float Dissolve_Progress_ref880 = (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress ));
				float smoothstepResult870 = smoothstep( lerpResult868 , ( 1.0 - lerpResult868 ) , ( _Mask_Strength * (pow( ( _Mask_Alpha_Ch == 1.0 ? tex2DNode856.r : tex2DNode856.a ) , _Mask_Contrast )*_Mask_Scale + _Mask_ScaleOffset) * ( _Mask_Strength_Mode == 0.0 ? 1.0 : Dissolve_Progress_ref880 ) ));
				float Mask859 = smoothstepResult870;
				#ifdef _MASK_USE_ON
				float staticSwitch861 = Mask859;
				#else
				float staticSwitch861 = 1.0;
				#endif
				float2 texCoord493 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv514 = texCoord493;
				float4 border514 = _SpriteBorder;
				float2 texCoord517 = IN.ase_texcoord.zw * float2( 1,1 ) + float2( 0,0 );
				float2 currentSize514 = texCoord517;
				float2 originalSize514 = _OriginalSize.xy;
				float2 localNineSliceUV514 = NineSliceUV514( uv514 , border514 , currentSize514 , originalSize514 );
				float2 temp_output_454_0 = ( ( _UVSwitch != 1.0 ? texCoord493 : localNineSliceUV514 ) * _MainTex_ST.xy );
				float2 temp_output_457_0 = ( temp_output_454_0 + _MainTex_ST.zw );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 ObjectScaleXY396 = (ase_objectScale).xy;
				float2 texCoord453 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_456_0 = ( ( temp_output_454_0 * texCoord453 ) + _MainTex_ST.zw );
				float2 temp_output_34_0_g6 = ( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) - float2( 0.5,0.5 ) );
				float2 break39_g6 = temp_output_34_0_g6;
				float2 appendResult50_g6 = (float2(( _Main_Radial_Tiling.y * ( length( temp_output_34_0_g6 ) * 2.0 ) ) , ( ( atan2( break39_g6.x , break39_g6.y ) * ( 1.0 / TWO_PI ) ) * _Main_Radial_Tiling.x )));
				int DeformType835 = _DeformType;
				float2 texCoord438 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_478_0 = ( _Pixelate * 1.0 );
				float temp_output_437_0 = max( temp_output_478_0 , 2.0 );
				half2 pixelateduv440 = floor( texCoord438 * float2( temp_output_437_0, temp_output_437_0 ) + float2( 0,0 ) ) / float2( temp_output_437_0, temp_output_437_0 );
				float2 lerpResult441 = lerp( texCoord438 , pixelateduv440 , saturate( step( 1.0 , abs( temp_output_478_0 ) ) ));
				float2 PixelUVBase442 = lerpResult441;
				float2 temp_output_446_0 = ( ( PixelUVBase442 * _DeformTex_ST.xy ) + _DeformTex_ST.zw );
				float2 temp_output_34_0_g4 = ( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g4 = temp_output_34_0_g4;
				float2 appendResult50_g4 = (float2(( _Deform_Radial_Tiling.y * ( length( temp_output_34_0_g4 ) * 2.0 ) ) , ( ( atan2( break39_g4.x , break39_g4.y ) * ( 1.0 / TWO_PI ) ) * _Deform_Radial_Tiling.x )));
				float2 panner133 = ( 1.0 * _Time.y * _Deform_Panning + (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )));
				float4 texCoord223 = IN.ase_texcoord1;
				texCoord223.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float cos212 = cos(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin212 = sin(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator212 = mul( (( _Deform_Pan_ParticleCustom2Y )?( ( (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )) + ( texCoord223.y * _Deform_Panning ) ) ):( panner133 )) - float2( 0.5,0.5 ) , float2x2( cos212 , -sin212 , sin212 , cos212 )) + float2( 0.5,0.5 );
				float2 clampResult848 = clamp( rotator212 , float2( 0.001,0.001 ) , float2( 0.999,0.999 ) );
				int DeformTypeSwitch841 = _DeformType;
				float2 lerpResult847 = lerp( rotator212 , clampResult848 , (float)DeformTypeSwitch841);
				float2 temp_output_122_0 = (tex2D( _DeformTex, lerpResult847 )).rg;
				float2 temp_output_138_0 = ( temp_output_122_0 - float2( 0.5,0.5 ) );
				float2 break200 = temp_output_138_0;
				float4 texCoord208 = IN.ase_texcoord1;
				texCoord208.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Deform165 = ( temp_output_138_0 * ( break200.x * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) * ( break200.y * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) );
				float2 Add835 = ( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) + Deform165 );
				float2 DeformTex829 = temp_output_122_0;
				float Deform_Strength830 = (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength ));
				float2 lerpResult831 = lerp( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , DeformTex829 , Deform_Strength830);
				float2 Lerp835 = lerpResult831;
				float2 localDeformMaskType835 = DeformMaskType835( DeformType835 , Add835 , Lerp835 );
				float lerpResult337 = lerp( 0.0 , 0.5 , _Deform_Mask_Smooth);
				int MaskType822 = _DeformMask_Type;
				float2 temp_cast_2 = (_Deform_Mask_OffsetStrength.x).xx;
				float2 texCoord329 = IN.ase_texcoord.xy * float2( 1,1 ) + temp_cast_2;
				float cos349 = cos(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin349 = sin(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator349 = mul( (( _FixedTiling )?( ( texCoord329 * ObjectScaleXY396 ) ):( texCoord329 )) - float2( 0.5,0.5 ) , float2x2( cos349 , -sin349 , sin349 , cos349 )) + float2( 0.5,0.5 );
				float Linear822 = (( rotator349 * _Deform_Mask_OffsetStrength.y )).x;
				float Beam822 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator349 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Radial822 = saturate( ( ( 1.0 - ( distance( rotator349 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Ring822 = 0.0;
				float localDeformMaskType822 = DeformMaskType822( MaskType822 , Linear822 , Beam822 , Radial822 , Ring822 );
				float smoothstepResult334 = smoothstep( lerpResult337 , ( 1.0 - lerpResult337 ) , localDeformMaskType822);
				float Deform_Mask352 = smoothstepResult334;
				float2 lerpResult356 = lerp( localDeformMaskType835 , (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , Deform_Mask352);
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch125 = lerpResult356;
				#else
				float2 staticSwitch125 = (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) ));
				#endif
				float2 panner288 = ( 1.0 * _Time.y * _Main_Panning + staticSwitch125);
				float4 texCoord285 = IN.ase_texcoord2;
				texCoord285.xy = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float cos109 = cos(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin109 = sin(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator109 = mul( (( _Main_Pan_ParticleCustom1Z )?( ( staticSwitch125 + ( texCoord285.z * _Main_Panning ) ) ):( panner288 )) - float2( 0.5,0.5 ) , float2x2( cos109 , -sin109 , sin109 , cos109 )) + float2( 0.5,0.5 );
				float Pixelate463 = temp_output_478_0;
				float temp_output_496_0 = max( Pixelate463 , 2.0 );
				half2 pixelateduv497 = floor( rotator109 * float2( temp_output_496_0, temp_output_496_0 ) + float2( 0,0 ) ) / float2( temp_output_496_0, temp_output_496_0 );
				float2 lerpResult499 = lerp( rotator109 , pixelateduv497 , saturate( step( 1.0 , abs( Pixelate463 ) ) ));
				float4 tex2DNode13 = tex2D( _MainTex, lerpResult499 );
				float lerpResult260 = lerp( 0.0 , 0.5 , _Dissolve_smooth);
				int MaskBlendType778 = _DissolveMask_BlendType;
				float lerpResult382 = lerp( 0.0 , 0.5 , _Dissolve_Mask_Smooth);
				int MaskType799 = _DissolveMask_Type;
				float2 temp_cast_3 = (_Dissolve_Mask_OffsetStrength.x).xx;
				float2 texCoord386 = IN.ase_texcoord.xy * float2( 1,1 ) + temp_cast_3;
				float cos377 = cos(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin377 = sin(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator377 = mul( (( _FixedTiling )?( ( texCoord386 * ObjectScaleXY396 ) ):( texCoord386 )) - float2( 0.5,0.5 ) , float2x2( cos377 , -sin377 , sin377 , cos377 )) + float2( 0.5,0.5 );
				float Linear799 = (( rotator377 * _Dissolve_Mask_OffsetStrength.y )).x;
				float Beam799 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator377 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Radial799 = saturate( ( ( 1.0 - ( distance( rotator377 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Ring799 = 0.0;
				float localDissolveMaskType799 = DissolveMaskType799( MaskType799 , Linear799 , Beam799 , Radial799 , Ring799 );
				float smoothstepResult384 = smoothstep( lerpResult382 , ( 1.0 - lerpResult382 ) , localDissolveMaskType799);
				float Dissolve_Mask385 = smoothstepResult384;
				float2 temp_output_450_0 = ( ( PixelUVBase442 * _DissolveTex_ST.xy ) + _DissolveTex_ST.zw );
				float2 temp_output_34_0_g5 = ( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g5 = temp_output_34_0_g5;
				float2 appendResult50_g5 = (float2(( _Dissolve_Radial_Tiling.y * ( length( temp_output_34_0_g5 ) * 2.0 ) ) , ( ( atan2( break39_g5.x , break39_g5.y ) * ( 1.0 / TWO_PI ) ) * _Dissolve_Radial_Tiling.x )));
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch279 = ( (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) )) + Deform165 );
				#else
				float2 staticSwitch279 = (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) ));
				#endif
				float2 panner234 = ( 1.0 * _Time.y * _Dissolve_Panning + staticSwitch279);
				float2 Deform_Panning272 = _Deform_Panning;
				float2 panner273 = ( 1.0 * _Time.y * Deform_Panning272 + staticSwitch279);
				float4 texCoord227 = IN.ase_texcoord1;
				texCoord227.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float cos242 = cos(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin242 = sin(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator242 = mul(  ( _Dissolve_Panning_Type - 0.0 > 1.0 ? panner234 : _Dissolve_Panning_Type - 0.0 <= 1.0 && _Dissolve_Panning_Type + 0.0 >= 1.0 ? (( _Deform_Pan_ParticleCustom2Y )?( ( staticSwitch279 + ( texCoord227.y * Deform_Panning272 ) ) ):( panner273 )) : ( staticSwitch279 + ( texCoord227.z * _Dissolve_Panning ) ) )  - float2( 0.5,0.5 ) , float2x2( cos242 , -sin242 , sin242 , cos242 )) + float2( 0.5,0.5 );
				float4 tex2DNode209 = tex2D( _DissolveTex, rotator242 );
				float Add778 = ( Dissolve_Mask385 + (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float Multiply778 = ( Dissolve_Mask385 * (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float localBlendType778 = BlendType778( MaskBlendType778 , Add778 , Multiply778 );
				float lerpResult250 = lerp( -1.0 , 1.0 , (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress )));
				float temp_output_254_0 = ( saturate( localBlendType778 ) + lerpResult250 );
				float smoothstepResult258 = smoothstep( lerpResult260 , ( 1.0 - lerpResult260 ) , temp_output_254_0);
				float Dissolve264 = saturate( smoothstepResult258 );
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch266 = Dissolve264;
				#else
				float staticSwitch266 = 1.0;
				#endif
				float temp_output_878_0 = ( saturate( ( IN.ase_color.a * _Main_Color.a * ( _Main_Alpha_Ch == 1.0 ? tex2DNode13.r : tex2DNode13.a ) ) ) * staticSwitch266 );
				float temp_output_874_0 = saturate( ( _Mask_BlendMode == 0.0 ? ( staticSwitch861 * temp_output_878_0 ) : ( staticSwitch861 + temp_output_878_0 ) ) );
				float smoothstepResult364 = smoothstep( lerpResult362 , ( 1.0 - lerpResult362 ) , temp_output_874_0);
				float lerpResult365 = lerp( smoothstepResult364 , temp_output_874_0 , step( _AlphaClip , 1E-05 ));
				float3 ase_normalWS = IN.ase_texcoord3.xyz;
				float dotResult703 = dot( ase_normalWS , -UNITY_MATRIX_V[ 2 ].xyz );
				float smoothstepResult750 = smoothstep( _Fresnel_AlphaClipStepMin , _Fresnel_AlphaClipStepMax , pow( abs( dotResult703 ) , _Fresnel_AlphaClipPower ));
				float temp_output_747_0 = ( 1.0 / _Fresnel_AlphaClipPixelate );
				float temp_output_745_0 = ( temp_output_747_0 * -1.0 );
				float clampResult746 = clamp( ddx( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float4 screenPos = IN.ase_texcoord4;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float2 appendResult708 = (float2(ase_positionSSNorm.x , ase_positionSSNorm.y));
				float2 appendResult720 = (float2(_ScaledScreenParams.x , _ScaledScreenParams.y));
				float2 temp_output_718_0 = ( appendResult708 * appendResult720 );
				float temp_output_771_0 = ( ( _Fresnel_AlphaClipPixelate * ( _ScaledScreenParams.x / 1920.0 ) ) / ( unity_OrthoParams.y / 10.5 ) );
				float2 break711 = ( floor( temp_output_718_0 ) - ( floor( ( temp_output_718_0 / temp_output_771_0 ) ) * temp_output_771_0 ) );
				float clampResult748 = clamp( ddy( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float FresnelAlphaClip631 = (( _Fresnel_AlphaClip )?( step( 0.5 , ( step( 0.05 , smoothstepResult750 ) * ( ( smoothstepResult750 - ( clampResult746 * break711.x ) ) - ( clampResult748 * break711.y ) ) ) ) ):( 1.0 ));
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.Alpha = ( lerpResult365 * FresnelAlphaClip631 );

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }

			Cull Off
			Blend Off
			ZTest LEqual
			ZWrite On

            HLSLPROGRAM

			#define ASE_VERSION 19904
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

        	#define ASE_NEEDS_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_TEXTURE_COORDINATES2
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_TEXTURE_COORDINATES3
        	#define ASE_NEEDS_TEXTURE_COORDINATES1
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
        	#define ASE_NEEDS_VERT_NORMAL
        	#pragma shader_feature_local _MASK_USE_ON
        	#pragma shader_feature_local _DEFORM_USE_ON
        	#pragma shader_feature_local _DISSOLVE_USE_ON


			sampler2D _Mask_Tex;
			sampler2D _MainTex;
			sampler2D _DeformTex;
			sampler2D _DissolveTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _MainTex_ST;
			float4 _DeformTex_ST;
			float4 _Mask_Tex_ST;
			float4 _Main_Color;
			float4 _Dissolve_Edge_Color;
			float4 _DissolveTex_ST;
			float4 _SpriteBorder;
			float4 _OriginalSize;
			float2 _Deform_Radial_Tiling;
			float2 _Deform_Panning;
			float2 _Main_Radial_Tiling;
			float2 _Dissolve_Mask_OffsetStrength;
			float2 _Dissolve_Radial_Tiling;
			float2 _Main_Panning;
			float2 _Dissolve_Panning;
			float2 _Deform_Mask_OffsetStrength;
			float _Dissolve_Radial;
			float _ERRORTOGGLE;
			float _Dissolve_Rotate;
			float _Dissolve_Panning_Type;
			float _DissolveTex_Reverse;
			float _Dissolve_Prog_ParticleCustom2W;
			float _Dissolve_Progress;
			float _Dissolve_Edge_Thick;
			float _Mask_Alpha_Ch;
			float _Mask_BlendMode;
			float _Mask_Smooth;
			float _Mask_Strength;
			float _Dissolve_Mask_Rotate;
			float _Mask_Rotate;
			float _Mask_Contrast;
			float _Mask_Scale;
			float _Mask_ScaleOffset;
			float _Mask_Strength_Mode;
			float _Main_Alpha_Ch;
			float _Dissolve_smooth;
			float _Fresnel_AlphaClip;
			float _Fresnel_AlphaClipStepMin;
			float _Fresnel_AlphaClipStepMax;
			float _AlphaClip;
			float _CullMode;
			float _Use_Dissolve_Edge;
			float _Dissolve_Mask_Smooth;
			float _BlendMode;
			float _MainTex_ColorMode;
			float _Main_Pan_ParticleCustom1Z;
			float _Main_Radial;
			float _Tiling_ParticleCustom1XY;
			float _FixedTiling;
			float _UVSwitch;
			int _DeformType;
			float _Deform_Pan_ParticleCustom2Y;
			float _Deform_Radial;
			int _Pixelate;
			float _Deform_Rotate;
			int _DissolveMask_Type;
			float _Deform_Str_ParticleCustom2X;
			float _Deform_Mask_Smooth;
			int _DeformMask_Type;
			float _Deform_Mask_Rotate;
			float _Main_Rotate;
			float _Hue;
			float _Saturation;
			float _Value;
			float _Emission_Mode;
			float _Emission;
			float _Fresnel_AlphaClipPower;
			float _Dissolve_Edge_smooth;
			int _DissolveMask_BlendType;
			float _Deform_Strength;
			float _Fresnel_AlphaClipPixelate;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _SelectionID;

            struct SurfaceDescription
			{
				float Alpha;
			};

			float2 NineSliceUV514( float2 uv, float4 border, float2 currentSize, float2 originalSize )
			{
				 // currentSize = 파티클 크기 (TexCoord1.xy에서 받음)
				  // originalSize = Material Property로 스크립트에서 전달
				  // border = Material Property로 스크립트에서 전달 (0~1 normalized)
				  float2 scale = currentSize / max(originalSize, 0.001);
				  float2 sMin = border.xy / max(scale, 0.001);
				  float2 sMax = border.zw / max(scale, 0.001);
				  float2 eStart = sMin;
				  float2 eEnd = 1.0 - sMax;
				  float2 L = uv * scale;
				  float2 R = 1.0 - (1.0 - uv) * scale;
				  float2 M = lerp(border.xy, 1.0 - border.zw, saturate((uv - eStart) / max(eEnd - eStart, 0.001)));
				  float2 maskL = step(uv, eStart);
				  float2 maskR = step(eEnd, uv);
				  float2 maskM = 1.0 - maskL - maskR;
				  return L * maskL + R * maskR + M * maskM;
			}
			
			float2 DeformMaskType835( int DeformType, float2 Add, float2 Lerp )
			{
				int mode = (int)DeformType;
				if (mode == 0) return Add;
				else if (mode == 1) return Lerp;
				else return Add;
			}
			
			float DeformMaskType822( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float DissolveMaskType799( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float BlendType778( int MaskBlendType, float Add, float Multiply )
			{
				int mode = (int)MaskBlendType;
				if (mode == 0) return Add;
				else if (mode == 1) return Multiply;
				else return Add;
			}
			

			VertexOutput vert(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float3 ase_normalWS = TransformObjectToWorldNormal( v.normal );
				o.ase_texcoord3.xyz = ase_normalWS;
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord4 = screenPos;
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				o.ase_texcoord1 = v.ase_texcoord2;
				o.ase_color = v.ase_color;
				o.ase_texcoord.zw = v.ase_texcoord3.xy;
				o.ase_texcoord2 = v.ase_texcoord1;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				float3 positionWS = TransformObjectToWorld(v.positionOS);
				o.positionCS = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float lerpResult362 = lerp( 0.0 , 0.5 , _AlphaClip);
				float lerpResult868 = lerp( 0.0 , 0.5 , _Mask_Smooth);
				float2 uv_Mask_Tex = IN.ase_texcoord.xy * _Mask_Tex_ST.xy + _Mask_Tex_ST.zw;
				float cos908 = cos(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin908 = sin(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator908 = mul( uv_Mask_Tex - float2( 0.5,0.5 ) , float2x2( cos908 , -sin908 , sin908 , cos908 )) + float2( 0.5,0.5 );
				float4 tex2DNode856 = tex2D( _Mask_Tex, rotator908 );
				float4 texCoord252 = IN.ase_texcoord1;
				texCoord252.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float Dissolve_Progress_ref880 = (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress ));
				float smoothstepResult870 = smoothstep( lerpResult868 , ( 1.0 - lerpResult868 ) , ( _Mask_Strength * (pow( ( _Mask_Alpha_Ch == 1.0 ? tex2DNode856.r : tex2DNode856.a ) , _Mask_Contrast )*_Mask_Scale + _Mask_ScaleOffset) * ( _Mask_Strength_Mode == 0.0 ? 1.0 : Dissolve_Progress_ref880 ) ));
				float Mask859 = smoothstepResult870;
				#ifdef _MASK_USE_ON
				float staticSwitch861 = Mask859;
				#else
				float staticSwitch861 = 1.0;
				#endif
				float2 texCoord493 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv514 = texCoord493;
				float4 border514 = _SpriteBorder;
				float2 texCoord517 = IN.ase_texcoord.zw * float2( 1,1 ) + float2( 0,0 );
				float2 currentSize514 = texCoord517;
				float2 originalSize514 = _OriginalSize.xy;
				float2 localNineSliceUV514 = NineSliceUV514( uv514 , border514 , currentSize514 , originalSize514 );
				float2 temp_output_454_0 = ( ( _UVSwitch != 1.0 ? texCoord493 : localNineSliceUV514 ) * _MainTex_ST.xy );
				float2 temp_output_457_0 = ( temp_output_454_0 + _MainTex_ST.zw );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 ObjectScaleXY396 = (ase_objectScale).xy;
				float2 texCoord453 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_456_0 = ( ( temp_output_454_0 * texCoord453 ) + _MainTex_ST.zw );
				float2 temp_output_34_0_g6 = ( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) - float2( 0.5,0.5 ) );
				float2 break39_g6 = temp_output_34_0_g6;
				float2 appendResult50_g6 = (float2(( _Main_Radial_Tiling.y * ( length( temp_output_34_0_g6 ) * 2.0 ) ) , ( ( atan2( break39_g6.x , break39_g6.y ) * ( 1.0 / TWO_PI ) ) * _Main_Radial_Tiling.x )));
				int DeformType835 = _DeformType;
				float2 texCoord438 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_478_0 = ( _Pixelate * 1.0 );
				float temp_output_437_0 = max( temp_output_478_0 , 2.0 );
				half2 pixelateduv440 = floor( texCoord438 * float2( temp_output_437_0, temp_output_437_0 ) + float2( 0,0 ) ) / float2( temp_output_437_0, temp_output_437_0 );
				float2 lerpResult441 = lerp( texCoord438 , pixelateduv440 , saturate( step( 1.0 , abs( temp_output_478_0 ) ) ));
				float2 PixelUVBase442 = lerpResult441;
				float2 temp_output_446_0 = ( ( PixelUVBase442 * _DeformTex_ST.xy ) + _DeformTex_ST.zw );
				float2 temp_output_34_0_g4 = ( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g4 = temp_output_34_0_g4;
				float2 appendResult50_g4 = (float2(( _Deform_Radial_Tiling.y * ( length( temp_output_34_0_g4 ) * 2.0 ) ) , ( ( atan2( break39_g4.x , break39_g4.y ) * ( 1.0 / TWO_PI ) ) * _Deform_Radial_Tiling.x )));
				float2 panner133 = ( 1.0 * _Time.y * _Deform_Panning + (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )));
				float4 texCoord223 = IN.ase_texcoord1;
				texCoord223.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float cos212 = cos(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin212 = sin(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator212 = mul( (( _Deform_Pan_ParticleCustom2Y )?( ( (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )) + ( texCoord223.y * _Deform_Panning ) ) ):( panner133 )) - float2( 0.5,0.5 ) , float2x2( cos212 , -sin212 , sin212 , cos212 )) + float2( 0.5,0.5 );
				float2 clampResult848 = clamp( rotator212 , float2( 0.001,0.001 ) , float2( 0.999,0.999 ) );
				int DeformTypeSwitch841 = _DeformType;
				float2 lerpResult847 = lerp( rotator212 , clampResult848 , (float)DeformTypeSwitch841);
				float2 temp_output_122_0 = (tex2D( _DeformTex, lerpResult847 )).rg;
				float2 temp_output_138_0 = ( temp_output_122_0 - float2( 0.5,0.5 ) );
				float2 break200 = temp_output_138_0;
				float4 texCoord208 = IN.ase_texcoord1;
				texCoord208.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Deform165 = ( temp_output_138_0 * ( break200.x * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) * ( break200.y * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) );
				float2 Add835 = ( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) + Deform165 );
				float2 DeformTex829 = temp_output_122_0;
				float Deform_Strength830 = (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength ));
				float2 lerpResult831 = lerp( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , DeformTex829 , Deform_Strength830);
				float2 Lerp835 = lerpResult831;
				float2 localDeformMaskType835 = DeformMaskType835( DeformType835 , Add835 , Lerp835 );
				float lerpResult337 = lerp( 0.0 , 0.5 , _Deform_Mask_Smooth);
				int MaskType822 = _DeformMask_Type;
				float2 temp_cast_2 = (_Deform_Mask_OffsetStrength.x).xx;
				float2 texCoord329 = IN.ase_texcoord.xy * float2( 1,1 ) + temp_cast_2;
				float cos349 = cos(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin349 = sin(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator349 = mul( (( _FixedTiling )?( ( texCoord329 * ObjectScaleXY396 ) ):( texCoord329 )) - float2( 0.5,0.5 ) , float2x2( cos349 , -sin349 , sin349 , cos349 )) + float2( 0.5,0.5 );
				float Linear822 = (( rotator349 * _Deform_Mask_OffsetStrength.y )).x;
				float Beam822 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator349 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Radial822 = saturate( ( ( 1.0 - ( distance( rotator349 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Ring822 = 0.0;
				float localDeformMaskType822 = DeformMaskType822( MaskType822 , Linear822 , Beam822 , Radial822 , Ring822 );
				float smoothstepResult334 = smoothstep( lerpResult337 , ( 1.0 - lerpResult337 ) , localDeformMaskType822);
				float Deform_Mask352 = smoothstepResult334;
				float2 lerpResult356 = lerp( localDeformMaskType835 , (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , Deform_Mask352);
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch125 = lerpResult356;
				#else
				float2 staticSwitch125 = (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) ));
				#endif
				float2 panner288 = ( 1.0 * _Time.y * _Main_Panning + staticSwitch125);
				float4 texCoord285 = IN.ase_texcoord2;
				texCoord285.xy = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float cos109 = cos(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin109 = sin(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator109 = mul( (( _Main_Pan_ParticleCustom1Z )?( ( staticSwitch125 + ( texCoord285.z * _Main_Panning ) ) ):( panner288 )) - float2( 0.5,0.5 ) , float2x2( cos109 , -sin109 , sin109 , cos109 )) + float2( 0.5,0.5 );
				float Pixelate463 = temp_output_478_0;
				float temp_output_496_0 = max( Pixelate463 , 2.0 );
				half2 pixelateduv497 = floor( rotator109 * float2( temp_output_496_0, temp_output_496_0 ) + float2( 0,0 ) ) / float2( temp_output_496_0, temp_output_496_0 );
				float2 lerpResult499 = lerp( rotator109 , pixelateduv497 , saturate( step( 1.0 , abs( Pixelate463 ) ) ));
				float4 tex2DNode13 = tex2D( _MainTex, lerpResult499 );
				float lerpResult260 = lerp( 0.0 , 0.5 , _Dissolve_smooth);
				int MaskBlendType778 = _DissolveMask_BlendType;
				float lerpResult382 = lerp( 0.0 , 0.5 , _Dissolve_Mask_Smooth);
				int MaskType799 = _DissolveMask_Type;
				float2 temp_cast_3 = (_Dissolve_Mask_OffsetStrength.x).xx;
				float2 texCoord386 = IN.ase_texcoord.xy * float2( 1,1 ) + temp_cast_3;
				float cos377 = cos(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin377 = sin(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator377 = mul( (( _FixedTiling )?( ( texCoord386 * ObjectScaleXY396 ) ):( texCoord386 )) - float2( 0.5,0.5 ) , float2x2( cos377 , -sin377 , sin377 , cos377 )) + float2( 0.5,0.5 );
				float Linear799 = (( rotator377 * _Dissolve_Mask_OffsetStrength.y )).x;
				float Beam799 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator377 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Radial799 = saturate( ( ( 1.0 - ( distance( rotator377 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Ring799 = 0.0;
				float localDissolveMaskType799 = DissolveMaskType799( MaskType799 , Linear799 , Beam799 , Radial799 , Ring799 );
				float smoothstepResult384 = smoothstep( lerpResult382 , ( 1.0 - lerpResult382 ) , localDissolveMaskType799);
				float Dissolve_Mask385 = smoothstepResult384;
				float2 temp_output_450_0 = ( ( PixelUVBase442 * _DissolveTex_ST.xy ) + _DissolveTex_ST.zw );
				float2 temp_output_34_0_g5 = ( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g5 = temp_output_34_0_g5;
				float2 appendResult50_g5 = (float2(( _Dissolve_Radial_Tiling.y * ( length( temp_output_34_0_g5 ) * 2.0 ) ) , ( ( atan2( break39_g5.x , break39_g5.y ) * ( 1.0 / TWO_PI ) ) * _Dissolve_Radial_Tiling.x )));
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch279 = ( (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) )) + Deform165 );
				#else
				float2 staticSwitch279 = (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) ));
				#endif
				float2 panner234 = ( 1.0 * _Time.y * _Dissolve_Panning + staticSwitch279);
				float2 Deform_Panning272 = _Deform_Panning;
				float2 panner273 = ( 1.0 * _Time.y * Deform_Panning272 + staticSwitch279);
				float4 texCoord227 = IN.ase_texcoord1;
				texCoord227.xy = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float cos242 = cos(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin242 = sin(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator242 = mul(  ( _Dissolve_Panning_Type - 0.0 > 1.0 ? panner234 : _Dissolve_Panning_Type - 0.0 <= 1.0 && _Dissolve_Panning_Type + 0.0 >= 1.0 ? (( _Deform_Pan_ParticleCustom2Y )?( ( staticSwitch279 + ( texCoord227.y * Deform_Panning272 ) ) ):( panner273 )) : ( staticSwitch279 + ( texCoord227.z * _Dissolve_Panning ) ) )  - float2( 0.5,0.5 ) , float2x2( cos242 , -sin242 , sin242 , cos242 )) + float2( 0.5,0.5 );
				float4 tex2DNode209 = tex2D( _DissolveTex, rotator242 );
				float Add778 = ( Dissolve_Mask385 + (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float Multiply778 = ( Dissolve_Mask385 * (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float localBlendType778 = BlendType778( MaskBlendType778 , Add778 , Multiply778 );
				float lerpResult250 = lerp( -1.0 , 1.0 , (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress )));
				float temp_output_254_0 = ( saturate( localBlendType778 ) + lerpResult250 );
				float smoothstepResult258 = smoothstep( lerpResult260 , ( 1.0 - lerpResult260 ) , temp_output_254_0);
				float Dissolve264 = saturate( smoothstepResult258 );
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch266 = Dissolve264;
				#else
				float staticSwitch266 = 1.0;
				#endif
				float temp_output_878_0 = ( saturate( ( IN.ase_color.a * _Main_Color.a * ( _Main_Alpha_Ch == 1.0 ? tex2DNode13.r : tex2DNode13.a ) ) ) * staticSwitch266 );
				float temp_output_874_0 = saturate( ( _Mask_BlendMode == 0.0 ? ( staticSwitch861 * temp_output_878_0 ) : ( staticSwitch861 + temp_output_878_0 ) ) );
				float smoothstepResult364 = smoothstep( lerpResult362 , ( 1.0 - lerpResult362 ) , temp_output_874_0);
				float lerpResult365 = lerp( smoothstepResult364 , temp_output_874_0 , step( _AlphaClip , 1E-05 ));
				float3 ase_normalWS = IN.ase_texcoord3.xyz;
				float dotResult703 = dot( ase_normalWS , -UNITY_MATRIX_V[ 2 ].xyz );
				float smoothstepResult750 = smoothstep( _Fresnel_AlphaClipStepMin , _Fresnel_AlphaClipStepMax , pow( abs( dotResult703 ) , _Fresnel_AlphaClipPower ));
				float temp_output_747_0 = ( 1.0 / _Fresnel_AlphaClipPixelate );
				float temp_output_745_0 = ( temp_output_747_0 * -1.0 );
				float clampResult746 = clamp( ddx( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float4 screenPos = IN.ase_texcoord4;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float2 appendResult708 = (float2(ase_positionSSNorm.x , ase_positionSSNorm.y));
				float2 appendResult720 = (float2(_ScaledScreenParams.x , _ScaledScreenParams.y));
				float2 temp_output_718_0 = ( appendResult708 * appendResult720 );
				float temp_output_771_0 = ( ( _Fresnel_AlphaClipPixelate * ( _ScaledScreenParams.x / 1920.0 ) ) / ( unity_OrthoParams.y / 10.5 ) );
				float2 break711 = ( floor( temp_output_718_0 ) - ( floor( ( temp_output_718_0 / temp_output_771_0 ) ) * temp_output_771_0 ) );
				float clampResult748 = clamp( ddy( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float FresnelAlphaClip631 = (( _Fresnel_AlphaClip )?( step( 0.5 , ( step( 0.05 , smoothstepResult750 ) * ( ( smoothstepResult750 - ( clampResult746 * break711.x ) ) - ( clampResult748 * break711.y ) ) ) ) ):( 1.0 ));
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.Alpha = ( lerpResult365 * FresnelAlphaClip631 );

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = unity_SelectionID;
				return outColor;
			}

            ENDHLSL
        }

		
		Pass
		{
			
            Name "Sprite Forward"
            Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM

			#define ASE_VERSION 19904
			#define ASE_SRP_VERSION 170004


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITEFORWARD

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_FRAG_COLOR
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES3
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _DEFORM_USE_ON
			#pragma shader_feature_local _DISSOLVE_USE_ON
			#pragma shader_feature_local _MASK_USE_ON


			sampler2D _MainTex;
			sampler2D _DeformTex;
			sampler2D _DissolveTex;
			sampler2D _Mask_Tex;
			CBUFFER_START( UnityPerMaterial )
			float4 _MainTex_ST;
			float4 _DeformTex_ST;
			float4 _Mask_Tex_ST;
			float4 _Main_Color;
			float4 _Dissolve_Edge_Color;
			float4 _DissolveTex_ST;
			float4 _SpriteBorder;
			float4 _OriginalSize;
			float2 _Deform_Radial_Tiling;
			float2 _Deform_Panning;
			float2 _Main_Radial_Tiling;
			float2 _Dissolve_Mask_OffsetStrength;
			float2 _Dissolve_Radial_Tiling;
			float2 _Main_Panning;
			float2 _Dissolve_Panning;
			float2 _Deform_Mask_OffsetStrength;
			float _Dissolve_Radial;
			float _ERRORTOGGLE;
			float _Dissolve_Rotate;
			float _Dissolve_Panning_Type;
			float _DissolveTex_Reverse;
			float _Dissolve_Prog_ParticleCustom2W;
			float _Dissolve_Progress;
			float _Dissolve_Edge_Thick;
			float _Mask_Alpha_Ch;
			float _Mask_BlendMode;
			float _Mask_Smooth;
			float _Mask_Strength;
			float _Dissolve_Mask_Rotate;
			float _Mask_Rotate;
			float _Mask_Contrast;
			float _Mask_Scale;
			float _Mask_ScaleOffset;
			float _Mask_Strength_Mode;
			float _Main_Alpha_Ch;
			float _Dissolve_smooth;
			float _Fresnel_AlphaClip;
			float _Fresnel_AlphaClipStepMin;
			float _Fresnel_AlphaClipStepMax;
			float _AlphaClip;
			float _CullMode;
			float _Use_Dissolve_Edge;
			float _Dissolve_Mask_Smooth;
			float _BlendMode;
			float _MainTex_ColorMode;
			float _Main_Pan_ParticleCustom1Z;
			float _Main_Radial;
			float _Tiling_ParticleCustom1XY;
			float _FixedTiling;
			float _UVSwitch;
			int _DeformType;
			float _Deform_Pan_ParticleCustom2Y;
			float _Deform_Radial;
			int _Pixelate;
			float _Deform_Rotate;
			int _DissolveMask_Type;
			float _Deform_Str_ParticleCustom2X;
			float _Deform_Mask_Smooth;
			int _DeformMask_Type;
			float _Deform_Mask_Rotate;
			float _Main_Rotate;
			float _Hue;
			float _Saturation;
			float _Value;
			float _Emission_Mode;
			float _Emission;
			float _Fresnel_AlphaClipPower;
			float _Dissolve_Edge_smooth;
			int _DissolveMask_BlendType;
			float _Deform_Strength;
			float _Fresnel_AlphaClipPixelate;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
			};

			float3 HSVToRGB( float3 c )
			{
				float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
				float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
				return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
			}
			
			float3 RGBToHSV(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
				float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
				float d = q.x - min( q.w, q.y );
				float e = 1.0e-10;
				return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}
			float2 NineSliceUV514( float2 uv, float4 border, float2 currentSize, float2 originalSize )
			{
				 // currentSize = 파티클 크기 (TexCoord1.xy에서 받음)
				  // originalSize = Material Property로 스크립트에서 전달
				  // border = Material Property로 스크립트에서 전달 (0~1 normalized)
				  float2 scale = currentSize / max(originalSize, 0.001);
				  float2 sMin = border.xy / max(scale, 0.001);
				  float2 sMax = border.zw / max(scale, 0.001);
				  float2 eStart = sMin;
				  float2 eEnd = 1.0 - sMax;
				  float2 L = uv * scale;
				  float2 R = 1.0 - (1.0 - uv) * scale;
				  float2 M = lerp(border.xy, 1.0 - border.zw, saturate((uv - eStart) / max(eEnd - eStart, 0.001)));
				  float2 maskL = step(uv, eStart);
				  float2 maskR = step(eEnd, uv);
				  float2 maskM = 1.0 - maskL - maskR;
				  return L * maskL + R * maskR + M * maskM;
			}
			
			float2 DeformMaskType835( int DeformType, float2 Add, float2 Lerp )
			{
				int mode = (int)DeformType;
				if (mode == 0) return Add;
				else if (mode == 1) return Lerp;
				else return Add;
			}
			
			float DeformMaskType822( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float DissolveMaskType799( int MaskType, float Linear, float Beam, float Radial, float Ring )
			{
				int mode = (int)MaskType;
				if (mode == 0) return Linear;
				else if (mode == 1) return Beam;
				else if (mode == 2) return Radial;
				else if (mode == 3) return Ring;
				else return Linear;
			}
			
			float BlendType778( int MaskBlendType, float Add, float Multiply )
			{
				int mode = (int)MaskBlendType;
				if (mode == 0) return Add;
				else if (mode == 1) return Multiply;
				else return Add;
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float3 ase_normalWS = TransformObjectToWorldNormal( v.normal );
				o.ase_texcoord6.xyz = ase_normalWS;
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord7 = screenPos;
				
				o.ase_texcoord3.xy = v.ase_texcoord3.xy;
				o.ase_texcoord4 = v.ase_texcoord1;
				o.ase_texcoord5 = v.ase_texcoord2;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;
				o.ase_texcoord6.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				float3 positionWS = TransformObjectToWorld(v.positionOS);

				o.positionCS = TransformWorldToHClip(positionWS);
				o.positionWS.xyz = positionWS;
				o.texCoord0.xyzw = v.uv0;
				o.color.xyzw = v.color;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float2 texCoord493 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float2 uv514 = texCoord493;
				float4 border514 = _SpriteBorder;
				float2 texCoord517 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 currentSize514 = texCoord517;
				float2 originalSize514 = _OriginalSize.xy;
				float2 localNineSliceUV514 = NineSliceUV514( uv514 , border514 , currentSize514 , originalSize514 );
				float2 temp_output_454_0 = ( ( _UVSwitch != 1.0 ? texCoord493 : localNineSliceUV514 ) * _MainTex_ST.xy );
				float2 temp_output_457_0 = ( temp_output_454_0 + _MainTex_ST.zw );
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 ObjectScaleXY396 = (ase_objectScale).xy;
				float2 texCoord453 = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_456_0 = ( ( temp_output_454_0 * texCoord453 ) + _MainTex_ST.zw );
				float2 temp_output_34_0_g6 = ( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) - float2( 0.5,0.5 ) );
				float2 break39_g6 = temp_output_34_0_g6;
				float2 appendResult50_g6 = (float2(( _Main_Radial_Tiling.y * ( length( temp_output_34_0_g6 ) * 2.0 ) ) , ( ( atan2( break39_g6.x , break39_g6.y ) * ( 1.0 / TWO_PI ) ) * _Main_Radial_Tiling.x )));
				int DeformType835 = _DeformType;
				float2 texCoord438 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float temp_output_478_0 = ( _Pixelate * 1.0 );
				float temp_output_437_0 = max( temp_output_478_0 , 2.0 );
				half2 pixelateduv440 = floor( texCoord438 * float2( temp_output_437_0, temp_output_437_0 ) + float2( 0,0 ) ) / float2( temp_output_437_0, temp_output_437_0 );
				float2 lerpResult441 = lerp( texCoord438 , pixelateduv440 , saturate( step( 1.0 , abs( temp_output_478_0 ) ) ));
				float2 PixelUVBase442 = lerpResult441;
				float2 temp_output_446_0 = ( ( PixelUVBase442 * _DeformTex_ST.xy ) + _DeformTex_ST.zw );
				float2 temp_output_34_0_g4 = ( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g4 = temp_output_34_0_g4;
				float2 appendResult50_g4 = (float2(( _Deform_Radial_Tiling.y * ( length( temp_output_34_0_g4 ) * 2.0 ) ) , ( ( atan2( break39_g4.x , break39_g4.y ) * ( 1.0 / TWO_PI ) ) * _Deform_Radial_Tiling.x )));
				float2 panner133 = ( 1.0 * _Time.y * _Deform_Panning + (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )));
				float4 texCoord223 = IN.ase_texcoord5;
				texCoord223.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float cos212 = cos(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin212 = sin(  (0.0 + ( _Deform_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator212 = mul( (( _Deform_Pan_ParticleCustom2Y )?( ( (( _Deform_Radial )?( appendResult50_g4 ):( (( _FixedTiling )?( ( temp_output_446_0 * ObjectScaleXY396 ) ):( temp_output_446_0 )) )) + ( texCoord223.y * _Deform_Panning ) ) ):( panner133 )) - float2( 0.5,0.5 ) , float2x2( cos212 , -sin212 , sin212 , cos212 )) + float2( 0.5,0.5 );
				float2 clampResult848 = clamp( rotator212 , float2( 0.001,0.001 ) , float2( 0.999,0.999 ) );
				int DeformTypeSwitch841 = _DeformType;
				float2 lerpResult847 = lerp( rotator212 , clampResult848 , (float)DeformTypeSwitch841);
				float2 temp_output_122_0 = (tex2D( _DeformTex, lerpResult847 )).rg;
				float2 temp_output_138_0 = ( temp_output_122_0 - float2( 0.5,0.5 ) );
				float2 break200 = temp_output_138_0;
				float4 texCoord208 = IN.ase_texcoord5;
				texCoord208.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float2 Deform165 = ( temp_output_138_0 * ( break200.x * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) * ( break200.y * (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength )) ) );
				float2 Add835 = ( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) + Deform165 );
				float2 DeformTex829 = temp_output_122_0;
				float Deform_Strength830 = (( _Deform_Str_ParticleCustom2X )?( texCoord208.x ):( _Deform_Strength ));
				float2 lerpResult831 = lerp( (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , DeformTex829 , Deform_Strength830);
				float2 Lerp835 = lerpResult831;
				float2 localDeformMaskType835 = DeformMaskType835( DeformType835 , Add835 , Lerp835 );
				float lerpResult337 = lerp( 0.0 , 0.5 , _Deform_Mask_Smooth);
				int MaskType822 = _DeformMask_Type;
				float2 temp_cast_2 = (_Deform_Mask_OffsetStrength.x).xx;
				float2 texCoord329 = IN.texCoord0.xy * float2( 1,1 ) + temp_cast_2;
				float cos349 = cos(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin349 = sin(  (0.0 + ( _Deform_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator349 = mul( (( _FixedTiling )?( ( texCoord329 * ObjectScaleXY396 ) ):( texCoord329 )) - float2( 0.5,0.5 ) , float2x2( cos349 , -sin349 , sin349 , cos349 )) + float2( 0.5,0.5 );
				float Linear822 = (( rotator349 * _Deform_Mask_OffsetStrength.y )).x;
				float Beam822 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator349 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Radial822 = saturate( ( ( 1.0 - ( distance( rotator349 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Deform_Mask_OffsetStrength.y ) );
				float Ring822 = 0.0;
				float localDeformMaskType822 = DeformMaskType822( MaskType822 , Linear822 , Beam822 , Radial822 , Ring822 );
				float smoothstepResult334 = smoothstep( lerpResult337 , ( 1.0 - lerpResult337 ) , localDeformMaskType822);
				float Deform_Mask352 = smoothstepResult334;
				float2 lerpResult356 = lerp( localDeformMaskType835 , (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) )) , Deform_Mask352);
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch125 = lerpResult356;
				#else
				float2 staticSwitch125 = (( _Main_Radial )?( appendResult50_g6 ):( (( _Tiling_ParticleCustom1XY )?( (( _FixedTiling )?( ( temp_output_456_0 * ObjectScaleXY396 ) ):( temp_output_456_0 )) ):( (( _FixedTiling )?( ( temp_output_457_0 * ObjectScaleXY396 ) ):( temp_output_457_0 )) )) ));
				#endif
				float2 panner288 = ( 1.0 * _Time.y * _Main_Panning + staticSwitch125);
				float4 texCoord285 = IN.ase_texcoord4;
				texCoord285.xy = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float cos109 = cos(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin109 = sin(  (0.0 + ( _Main_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator109 = mul( (( _Main_Pan_ParticleCustom1Z )?( ( staticSwitch125 + ( texCoord285.z * _Main_Panning ) ) ):( panner288 )) - float2( 0.5,0.5 ) , float2x2( cos109 , -sin109 , sin109 , cos109 )) + float2( 0.5,0.5 );
				float Pixelate463 = temp_output_478_0;
				float temp_output_496_0 = max( Pixelate463 , 2.0 );
				half2 pixelateduv497 = floor( rotator109 * float2( temp_output_496_0, temp_output_496_0 ) + float2( 0,0 ) ) / float2( temp_output_496_0, temp_output_496_0 );
				float2 lerpResult499 = lerp( rotator109 , pixelateduv497 , saturate( step( 1.0 , abs( Pixelate463 ) ) ));
				float4 tex2DNode13 = tex2D( _MainTex, lerpResult499 );
				float3 hsvTorgb3_g7 = RGBToHSV( tex2DNode13.rgb );
				float3 hsvTorgb10_g7 = HSVToRGB( float3(frac( ( hsvTorgb3_g7.x + _Hue ) ),saturate( ( hsvTorgb3_g7.y * _Saturation ) ),( hsvTorgb3_g7.z * _Value )) );
				float3 temp_output_893_0 = hsvTorgb10_g7;
				float3 temp_cast_3 = (tex2DNode13.r).xxx;
				float3 temp_cast_4 = (tex2DNode13.r).xxx;
				float4 texCoord369 = IN.ase_texcoord4;
				texCoord369.xy = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float3 lerpResult368 = lerp( temp_output_893_0 , temp_cast_4 , texCoord369.w);
				float3 ifLocalVar370 = 0;
				if( 1.0 > _MainTex_ColorMode )
				ifLocalVar370 = temp_output_893_0;
				else if( 1.0 == _MainTex_ColorMode )
				ifLocalVar370 = temp_cast_3;
				else if( 1.0 < _MainTex_ColorMode )
				ifLocalVar370 = lerpResult368;
				float4 texCoord899 = IN.ase_texcoord4;
				texCoord899.xy = IN.ase_texcoord4.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult904 = clamp( ( _Emission_Mode == 1.0 ? texCoord899.w : _Emission ) , 1.0 , 999.0 );
				float lerpResult306 = lerp( 0.0 , 0.5 , _Dissolve_Edge_smooth);
				int MaskBlendType778 = _DissolveMask_BlendType;
				float lerpResult382 = lerp( 0.0 , 0.5 , _Dissolve_Mask_Smooth);
				int MaskType799 = _DissolveMask_Type;
				float2 temp_cast_5 = (_Dissolve_Mask_OffsetStrength.x).xx;
				float2 texCoord386 = IN.texCoord0.xy * float2( 1,1 ) + temp_cast_5;
				float cos377 = cos(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin377 = sin(  (0.0 + ( _Dissolve_Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator377 = mul( (( _FixedTiling )?( ( texCoord386 * ObjectScaleXY396 ) ):( texCoord386 )) - float2( 0.5,0.5 ) , float2x2( cos377 , -sin377 , sin377 , cos377 )) + float2( 0.5,0.5 );
				float Linear799 = (( rotator377 * _Dissolve_Mask_OffsetStrength.y )).x;
				float Beam799 = saturate( ( ( 1.0 - ( abs( ( (( ( rotator377 - float2( 0.5,0 ) ) + float2( 0.5,0 ) )).x - 0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Radial799 = saturate( ( ( 1.0 - ( distance( rotator377 , float2( 0.5,0.5 ) ) * 2.0 ) ) * _Dissolve_Mask_OffsetStrength.y ) );
				float Ring799 = 0.0;
				float localDissolveMaskType799 = DissolveMaskType799( MaskType799 , Linear799 , Beam799 , Radial799 , Ring799 );
				float smoothstepResult384 = smoothstep( lerpResult382 , ( 1.0 - lerpResult382 ) , localDissolveMaskType799);
				float Dissolve_Mask385 = smoothstepResult384;
				float2 temp_output_450_0 = ( ( PixelUVBase442 * _DissolveTex_ST.xy ) + _DissolveTex_ST.zw );
				float2 temp_output_34_0_g5 = ( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) - float2( 0.5,0.5 ) );
				float2 break39_g5 = temp_output_34_0_g5;
				float2 appendResult50_g5 = (float2(( _Dissolve_Radial_Tiling.y * ( length( temp_output_34_0_g5 ) * 2.0 ) ) , ( ( atan2( break39_g5.x , break39_g5.y ) * ( 1.0 / TWO_PI ) ) * _Dissolve_Radial_Tiling.x )));
				#ifdef _DEFORM_USE_ON
				float2 staticSwitch279 = ( (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) )) + Deform165 );
				#else
				float2 staticSwitch279 = (( _Dissolve_Radial )?( appendResult50_g5 ):( (( _FixedTiling )?( ( temp_output_450_0 * ObjectScaleXY396 ) ):( temp_output_450_0 )) ));
				#endif
				float2 panner234 = ( 1.0 * _Time.y * _Dissolve_Panning + staticSwitch279);
				float2 Deform_Panning272 = _Deform_Panning;
				float2 panner273 = ( 1.0 * _Time.y * Deform_Panning272 + staticSwitch279);
				float4 texCoord227 = IN.ase_texcoord5;
				texCoord227.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float cos242 = cos(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin242 = sin(  (0.0 + ( _Dissolve_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator242 = mul(  ( _Dissolve_Panning_Type - 0.0 > 1.0 ? panner234 : _Dissolve_Panning_Type - 0.0 <= 1.0 && _Dissolve_Panning_Type + 0.0 >= 1.0 ? (( _Deform_Pan_ParticleCustom2Y )?( ( staticSwitch279 + ( texCoord227.y * Deform_Panning272 ) ) ):( panner273 )) : ( staticSwitch279 + ( texCoord227.z * _Dissolve_Panning ) ) )  - float2( 0.5,0.5 ) , float2x2( cos242 , -sin242 , sin242 , cos242 )) + float2( 0.5,0.5 );
				float4 tex2DNode209 = tex2D( _DissolveTex, rotator242 );
				float Add778 = ( Dissolve_Mask385 + (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float Multiply778 = ( Dissolve_Mask385 * (( _DissolveTex_Reverse )?( ( 1.0 - tex2DNode209.r ) ):( tex2DNode209.r )) );
				float localBlendType778 = BlendType778( MaskBlendType778 , Add778 , Multiply778 );
				float4 texCoord252 = IN.ase_texcoord5;
				texCoord252.xy = IN.ase_texcoord5.xy * float2( 1,1 ) + float2( 0,0 );
				float lerpResult250 = lerp( -1.0 , 1.0 , (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress )));
				float temp_output_254_0 = ( saturate( localBlendType778 ) + lerpResult250 );
				float Dissolve_Before_Smooth298 = temp_output_254_0;
				float smoothstepResult304 = smoothstep( lerpResult306 , ( 1.0 - lerpResult306 ) , ( 1.0 - ( Dissolve_Before_Smooth298 - _Dissolve_Edge_Thick ) ));
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch310 = smoothstepResult304;
				#else
				float staticSwitch310 = 0.0;
				#endif
				float3 lerpResult299 = lerp( ( ( (IN.color).rgb * ifLocalVar370 * _Main_Color.rgb ) * clampResult904 ) , _Dissolve_Edge_Color.rgb , (( _Use_Dissolve_Edge )?( staticSwitch310 ):( 0.0 )));
				
				float lerpResult362 = lerp( 0.0 , 0.5 , _AlphaClip);
				float lerpResult868 = lerp( 0.0 , 0.5 , _Mask_Smooth);
				float2 uv_Mask_Tex = IN.texCoord0.xy * _Mask_Tex_ST.xy + _Mask_Tex_ST.zw;
				float cos908 = cos(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float sin908 = sin(  (0.0 + ( _Mask_Rotate - 0.0 ) * ( TWO_PI - 0.0 ) / ( 360.0 - 0.0 ) ) );
				float2 rotator908 = mul( uv_Mask_Tex - float2( 0.5,0.5 ) , float2x2( cos908 , -sin908 , sin908 , cos908 )) + float2( 0.5,0.5 );
				float4 tex2DNode856 = tex2D( _Mask_Tex, rotator908 );
				float Dissolve_Progress_ref880 = (( _Dissolve_Prog_ParticleCustom2W )?( texCoord252.w ):( _Dissolve_Progress ));
				float smoothstepResult870 = smoothstep( lerpResult868 , ( 1.0 - lerpResult868 ) , ( _Mask_Strength * (pow( ( _Mask_Alpha_Ch == 1.0 ? tex2DNode856.r : tex2DNode856.a ) , _Mask_Contrast )*_Mask_Scale + _Mask_ScaleOffset) * ( _Mask_Strength_Mode == 0.0 ? 1.0 : Dissolve_Progress_ref880 ) ));
				float Mask859 = smoothstepResult870;
				#ifdef _MASK_USE_ON
				float staticSwitch861 = Mask859;
				#else
				float staticSwitch861 = 1.0;
				#endif
				float lerpResult260 = lerp( 0.0 , 0.5 , _Dissolve_smooth);
				float smoothstepResult258 = smoothstep( lerpResult260 , ( 1.0 - lerpResult260 ) , temp_output_254_0);
				float Dissolve264 = saturate( smoothstepResult258 );
				#ifdef _DISSOLVE_USE_ON
				float staticSwitch266 = Dissolve264;
				#else
				float staticSwitch266 = 1.0;
				#endif
				float temp_output_878_0 = ( saturate( ( IN.color.a * _Main_Color.a * ( _Main_Alpha_Ch == 1.0 ? tex2DNode13.r : tex2DNode13.a ) ) ) * staticSwitch266 );
				float temp_output_874_0 = saturate( ( _Mask_BlendMode == 0.0 ? ( staticSwitch861 * temp_output_878_0 ) : ( staticSwitch861 + temp_output_878_0 ) ) );
				float smoothstepResult364 = smoothstep( lerpResult362 , ( 1.0 - lerpResult362 ) , temp_output_874_0);
				float lerpResult365 = lerp( smoothstepResult364 , temp_output_874_0 , step( _AlphaClip , 1E-05 ));
				float3 ase_normalWS = IN.ase_texcoord6.xyz;
				float dotResult703 = dot( ase_normalWS , -UNITY_MATRIX_V[ 2 ].xyz );
				float smoothstepResult750 = smoothstep( _Fresnel_AlphaClipStepMin , _Fresnel_AlphaClipStepMax , pow( abs( dotResult703 ) , _Fresnel_AlphaClipPower ));
				float temp_output_747_0 = ( 1.0 / _Fresnel_AlphaClipPixelate );
				float temp_output_745_0 = ( temp_output_747_0 * -1.0 );
				float clampResult746 = clamp( ddx( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float4 screenPos = IN.ase_texcoord7;
				float4 ase_positionSSNorm = screenPos / screenPos.w;
				ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
				float2 appendResult708 = (float2(ase_positionSSNorm.x , ase_positionSSNorm.y));
				float2 appendResult720 = (float2(_ScaledScreenParams.x , _ScaledScreenParams.y));
				float2 temp_output_718_0 = ( appendResult708 * appendResult720 );
				float temp_output_771_0 = ( ( _Fresnel_AlphaClipPixelate * ( _ScaledScreenParams.x / 1920.0 ) ) / ( unity_OrthoParams.y / 10.5 ) );
				float2 break711 = ( floor( temp_output_718_0 ) - ( floor( ( temp_output_718_0 / temp_output_771_0 ) ) * temp_output_771_0 ) );
				float clampResult748 = clamp( ddy( smoothstepResult750 ) , temp_output_745_0 , temp_output_747_0 );
				float FresnelAlphaClip631 = (( _Fresnel_AlphaClip )?( step( 0.5 , ( step( 0.05 , smoothstepResult750 ) * ( ( smoothstepResult750 - ( clampResult746 * break711.x ) ) - ( clampResult748 * break711.y ) ) ) ) ):( 1.0 ));
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.BaseColor = lerpResult299;
				surfaceDescription.NormalTS = float3(0.0f, 0.0f, 1.0f);
				surfaceDescription.Alpha = ( lerpResult365 * FresnelAlphaClip631 );


				half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);

				#if defined(DEBUG_DISPLAY)
					SurfaceData2D surfaceData;
					InitializeSurfaceData(color.rgb, color.a, surfaceData);
					InputData2D inputData;
					InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
					half4 debugColor = 0;

					SETUP_DEBUG_DATA_2D(inputData, positionWS, positionCS);

					if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
					{
						return debugColor;
					}
				#endif

				color *= IN.color;
				
				return color;
			}

            ENDHLSL
        }
		
	}
	CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19904
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;433;-5253.6,880;Inherit;False;1257.6;591.7001;;12;478;435;437;442;441;439;440;436;434;463;438;479;PixelUVBase;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;479;-5216,1296;Inherit;False;Constant;_Float1;Float 1;50;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;435;-5216,1216;Inherit;False;Property;_Pixelate;Pixelate;7;0;Create;True;0;0;0;False;0;False;0;32;False;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;478;-5088,1216;Inherit;False;2;2;0;INT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;434;-4768,1232;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;438;-5008,944;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;437;-4864,1120;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;436;-4672,1216;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCPixelate, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;440;-4736,1008;Inherit;True;4;0;FLOAT2;0,0;False;1;FLOAT;8;False;2;FLOAT;8;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;439;-4576,1216;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;441;-4368,944;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;-3662.608,224;Inherit;False;3763.084;577.4884;;38;222;272;221;133;224;325;128;405;323;223;446;416;324;445;407;443;444;165;131;138;162;143;225;200;122;116;132;208;212;213;211;210;829;830;842;847;848;849;Deform;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;442;-4240,944;Inherit;False;PixelUVBase;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureTransformNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;444;-3632,384;Inherit;False;116;False;1;0;SAMPLER2D;;False;2;FLOAT2;0;FLOAT2;1
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;443;-3632,304;Inherit;False;442;PixelUVBase;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;407;-3296,432;Inherit;False;396;ObjectScaleXY;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;445;-3424,304;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;324;-2960,448;Inherit;False;Property;_Deform_Radial_Tiling;Deform_Radial_Tiling;27;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;416;-3104,384;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;446;-3264,304;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;372;-3680,2720;Inherit;False;3588.631;833.4368;;37;385;384;382;381;379;383;380;378;377;408;376;419;373;374;375;410;386;781;782;783;784;785;786;787;788;789;790;791;792;793;795;796;798;799;800;801;802;DissolveMask;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;353;-3648,896;Inherit;False;3773.088;776.9591;;36;352;334;358;337;336;333;335;360;349;411;348;417;350;347;359;329;413;803;804;805;806;807;808;809;810;811;812;814;815;816;819;820;821;822;824;825;DeformMask;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;-2512,416;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;323;-2736,416;Inherit;False;Polar Coordinates;-1;;4;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;3;FLOAT2;0;FLOAT;55;FLOAT;56
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;405;-2960,320;Inherit;False;Property;_FixedTiling;FixedTiling;9;0;Create;True;0;0;0;False;0;False;0;True;Reference;398;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;789;-2320,2928;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;128;-2512,576;Inherit;False;Property;_Deform_Panning;Deform_Panning;31;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;325;-2512,320;Inherit;False;Property;_Deform_Radial;Deform_Radial;26;0;Create;True;0;0;0;False;0;False;0;False;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;224;-2208,400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;803;-2288,1088;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;268;-3680,1872;Inherit;False;4919.491;690.213;;52;298;264;263;258;254;390;389;779;778;259;260;261;432;388;255;209;256;250;253;252;251;242;238;247;420;233;232;244;234;248;279;273;275;226;249;274;277;328;278;402;326;227;280;450;418;327;449;404;448;447;780;880;Dissolve;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;791;-2176,2928;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;133;-2224,512;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;221;-2064,320;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;210;-2000,544;Inherit;False;Property;_Deform_Rotate;Deform_Rotate;25;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;211;-1936,640;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;804;-2144,1088;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;447;-3552,1920;Inherit;False;442;PixelUVBase;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureTransformNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;448;-3584,2000;Inherit;False;209;False;1;0;SAMPLER2D;;False;2;FLOAT2;0;FLOAT2;1
Node;AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;792;-2080,2928;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;213;-1840,544;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;360;False;3;FLOAT;0;False;4;FLOAT;6.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;222;-1952,320;Inherit;False;Property;_Deform_Pan_ParticleCustom2Y;Deform_Pan_ParticleCustom2Y;30;0;Create;True;0;0;0;False;0;False;1;False;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;805;-2048,1088;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerStateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;429;-4624,-864;Inherit;False;0;0;0;1;-1;None;1;0;SAMPLER2D;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;841;-3040,-192;Inherit;False;DeformTypeSwitch;-1;True;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;795;-2320,3104;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;404;-3232,2032;Inherit;False;396;ObjectScaleXY;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;449;-3376,1920;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;386;-3344,2784;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;410;-3312,2896;Inherit;False;396;ObjectScaleXY;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;781;-1952,2928;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;375;-3664,2976;Inherit;False;Property;_Dissolve_Mask_OffsetStrength;Dissolve_Mask_Offset/Strength;53;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;430;-4416,-864;Inherit;False;SamplerState;-1;True;1;0;SAMPLERSTATE;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;212;-1696,320;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;413;-3280,1072;Inherit;False;396;ObjectScaleXY;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;359;-3616,1152;Inherit;False;Property;_Deform_Mask_OffsetStrength;Deform_Mask_Offset/Strength;33;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DistanceOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;807;-2288,1280;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;806;-1920,1088;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;329;-3296,960;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;842;-1584,512;Inherit;False;841;DeformTypeSwitch;1;0;OBJECT;;False;1;INT;0
Node;AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;848;-1520,368;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.001,0.001;False;2;FLOAT2;0.999,0.999;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;802;-2752,3088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;782;-2160,3104;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;327;-2912,2080;Inherit;False;Property;_Dissolve_Radial_Tiling;Dissolve_Radial_Tiling;41;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;418;-3040,1968;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;450;-3216,1920;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;374;-2832,2976;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;373;-2960,2880;Inherit;False;Property;_Dissolve_Mask_Rotate;Dissolve_Mask_Rotate;55;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;419;-3104,2832;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;783;-1824,2928;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;847;-1360,304;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;849;-1584,592;Inherit;False;430;SamplerState;1;0;OBJECT;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;347;-2896,1056;Inherit;False;Property;_Deform_Mask_Rotate;Deform_Mask_Rotate;35;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;350;-2800,1152;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;417;-3072,1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;809;-2128,1280;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;808;-1792,1088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;784;-2000,3104;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;801;-2336,3200;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;280;-2416,2032;Inherit;False;165;Deform;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;227;-2176,2080;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;326;-2672,2016;Inherit;False;Polar Coordinates;-1;;5;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;3;FLOAT2;0;FLOAT;55;FLOAT;56
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;402;-2896,1920;Inherit;False;Property;_FixedTiling;FixedTiling;46;0;Create;True;0;0;0;False;0;False;0;True;Reference;398;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;272;-2240,640;Inherit;False;Deform_Panning;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;376;-2736,2880;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;360;False;3;FLOAT;0;False;4;FLOAT;6.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;408;-2928,2784;Inherit;False;Property;_FixedTiling;FixedTiling;47;0;Create;True;0;0;0;False;0;False;0;True;Reference;398;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;785;-1728,2928;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;208;-1152,576;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;132;-1152,512;Inherit;False;Property;_Deform_Strength;Deform_Strength;29;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;824;-2272,1376;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;348;-2704,1056;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;360;False;3;FLOAT;0;False;4;FLOAT;6.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;411;-2944,960;Inherit;False;Property;_FixedTiling;FixedTiling;45;0;Create;True;0;0;0;False;0;False;0;True;Reference;398;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;811;-1968,1280;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;810;-1696,1088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-1216,288;Inherit;True;Property;_DeformTex;DeformTex;24;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;796;-1840,3104;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;278;-2224,2000;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;328;-2448,1920;Inherit;False;Property;_Dissolve_Radial;Dissolve_Radial;40;0;Create;True;0;0;0;False;0;False;0;False;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;277;-2144,2384;Inherit;False;272;Deform_Panning;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;274;-1808,2432;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;377;-2576,2784;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;378;-2496,3008;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;793;-1600,2928;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;800;-1808,3040;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;637;2861.786,464;Inherit;False;2864.409;1738.285;;45;701;631;766;721;715;709;714;750;712;748;774;773;729;746;713;711;775;768;734;745;747;710;703;743;736;728;772;700;735;718;733;720;708;771;719;704;770;762;764;758;763;757;761;776;777;FresnelAlphaClip;1,1,1,1;0;0
Node;AmplifyShaderEditor.ObjectScaleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;394;-4624,-1056;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;200;-624,352;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;122;-928,288;Inherit;False;FLOAT2;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;517;-6032,-80;Inherit;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;519;-6000,32;Inherit;False;Property;_OriginalSize;OriginalSize;58;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;513;-6000,-240;Inherit;False;Property;_SpriteBorder;SpriteBorder;57;1;[HideInInspector];Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;349;-2544,960;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;360;-2560,1200;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;814;-1808,1280;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;825;-1600,1216;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;812;-1568,1088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;225;-912,560;Inherit;False;Property;_Deform_Str_ParticleCustom2X;Deform_Str_ParticleCustom2X;28;0;Create;True;0;0;0;False;0;False;1;False;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;493;-6048,-528;Inherit;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;249;-1520,1984;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;226;-1728,2112;Inherit;False;Property;_Dissolve_Panning;Dissolve_Panning;46;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;275;-1680,2368;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;273;-1696,2224;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;790;-1472,2928;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;380;-2352,2800;Inherit;False;2;2;0;FLOAT2;1,1;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;279;-2128,1920;Inherit;False;Property;_Dissolve_Deform_Use;Dissolve_Deform_Use;22;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;125;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;786;-1424,3104;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;757;3488,1824;Inherit;False;Constant;_Width;Width;57;0;Create;True;0;0;0;False;0;False;1920;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;761;3312,1728;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;392;-4432,-1008;Inherit;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-480,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;-480,448;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;516;-5344,-480;Inherit;False;Property;_UVSwitch;UVSwitch;8;1;[Enum];Create;True;0;2;Normal UV;0;Border UV;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;138;-784,288;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;815;-1664,1280;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;333;-2288,960;Inherit;False;2;2;0;FLOAT2;1,1;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;816;-1440,1088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;514;-5664,-288;Float;False; // currentSize = 파티클 크기 (TexCoord1.xy에서 받음)$  // originalSize = Material Property로 스크립트에서 전달$  // border = Material Property로 스크립트에서 전달 (0~1 normalized)$$  float2 scale = currentSize / max(originalSize, 0.001)@$$  float2 sMin = border.xy / max(scale, 0.001)@$  float2 sMax = border.zw / max(scale, 0.001)@$$  float2 eStart = sMin@$  float2 eEnd = 1.0 - sMax@$$  float2 L = uv * scale@$  float2 R = 1.0 - (1.0 - uv) * scale@$  float2 M = lerp(border.xy, 1.0 - border.zw, saturate((uv - eStart) / max(eEnd - eStart, 0.001)))@$$  float2 maskL = step(uv, eStart)@$  float2 maskR = step(eEnd, uv)@$  float2 maskM = 1.0 - maskL - maskR@$$  return L * maskL + R * maskR + M * maskM@;2;Create;4;True;uv;FLOAT2;0,0;In;;Float;False;True;border;FLOAT4;0,0,0,0;In;;Float;False;True;currentSize;FLOAT2;0,0;In;;Float;False;True;originalSize;FLOAT2;0,0;In;;Float;False;NineSliceUV;True;False;0;;False;4;0;FLOAT2;0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;906;-3824,3968;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;905;-3888,3872;Inherit;False;Property;_Mask_Rotate;Mask_Rotate;69;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;248;-1376,1920;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;234;-1360,2096;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;244;-1152,1984;Inherit;False;Property;_Dissolve_Panning_Type;Dissolve_Panning_Type;45;1;[Enum];Create;True;0;3;ParticleCustom2 Z;0;With Deform;1;Use Prop Auto;2;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;420;-1472,2224;Inherit;False;Property;_ERRORTOGGLE;ERRORTOGGLE;56;0;Create;True;0;0;0;False;1;HideInInspector;False;0;True;Reference;222;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;383;-2160,2800;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;787;-1264,3088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;788;-1328,2928;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;798;-1264,2768;Inherit;False;Property;_DissolveMask_Type;DissolveMask_Type;51;1;[Enum];Create;True;0;3;Linear;0;Beam;1;Radial;2;0;False;1;Space (12);False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;379;-1056,2976;Inherit;False;Property;_Dissolve_Mask_Smooth;Dissolve_Mask_Smooth;54;0;Create;True;0;0;0;False;0;False;0;0.06250276;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OrthoParams, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;763;3424,1936;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;758;3472,2064;Inherit;False;Constant;_Size;Size;57;0;Create;True;0;0;0;False;0;False;10.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;764;3664,1744;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;396;-4224,-1008;Inherit;False;ObjectScaleXY;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;-288,288;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureTransformNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;451;-5312,-208;Inherit;False;13;False;1;0;SAMPLER2D;;False;2;FLOAT2;0;FLOAT2;1
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;453;-5056,-96;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Compare, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;515;-5344,-416;Inherit;False;1;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;358;-2080,960;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;335;-928,1152;Inherit;False;Property;_Deform_Mask_Smooth;Deform_Mask_Smooth;34;0;Create;True;0;0;0;False;0;False;0;0.06250276;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;821;-1264,944;Inherit;False;Property;_DeformMask_Type;DeformMask_Type;32;1;[Enum];Create;True;0;4;Linear;0;Beam;1;Radial;2;Ring;3;0;False;1;Space (12);False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;819;-1168,1152;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;820;-1296,1088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;232;-1264,2320;Inherit;False;Property;_Dissolve_Rotate;Dissolve_Rotate;39;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;233;-1200,2416;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;909;-3792,3680;Inherit;False;0;856;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;907;-3728,3872;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;360;False;3;FLOAT;0;False;4;FLOAT;6.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;247;-1120,2048;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;799;-1056,2800;Inherit;False;int mode = (int)MaskType@$if (mode == 0) return Linear@$else if (mode == 1) return Beam@$else if (mode == 2) return Radial@$else if (mode == 3) return Ring@$else return Linear@;1;Create;5;True;MaskType;INT;0;In;;Inherit;False;True;Linear;FLOAT;0;In;;Float;False;True;Beam;FLOAT;1;In;;Float;False;True;Radial;FLOAT;0;In;;Float;False;True;Ring;FLOAT;0;In;;Float;False;DissolveMaskType;True;False;0;;False;5;0;INT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;382;-800,2928;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;381;-672,2928;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;762;3840,1632;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;770;3664,2016;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;829;-928,384;Inherit;False;DeformTex;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;830;-560,624;Inherit;False;Deform_Strength;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;397;-4640,-272;Inherit;False;396;ObjectScaleXY;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;401;-4656,-96;Inherit;False;396;ObjectScaleXY;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;455;-4784,-240;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;454;-5040,-320;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;336;-544,1104;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;337;-672,1104;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;822;-1056,976;Inherit;False;int mode = (int)MaskType@$if (mode == 0) return Linear@$else if (mode == 1) return Beam@$else if (mode == 2) return Radial@$else if (mode == 3) return Ring@$else return Linear@;1;Create;5;True;MaskType;INT;0;In;;Inherit;False;True;Linear;FLOAT;0;In;;Float;False;True;Beam;FLOAT;1;In;;Float;False;True;Radial;FLOAT;0;In;;Float;False;True;Ring;FLOAT;0;In;;Float;False;DeformMaskType;True;False;0;;False;5;0;INT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;165;-84,288;Inherit;False;Deform;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;238;-1104,2320;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;360;False;3;FLOAT;0;False;4;FLOAT;6.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;908;-3520,3760;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;432;-880,2240;Inherit;False;430;SamplerState;1;0;OBJECT;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;384;-544,2800;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;704;3184,1264;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;719;3184,1440;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;771;4016,1632;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;858;-2992,3760;Inherit;False;Property;_Mask_Alpha_Ch;Mask_Alpha_Ch;71;1;[Enum];Create;True;0;2;A;0;R;1;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;414;-4448,-320;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;415;-4464,-144;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;457;-4656,-432;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;456;-4592,-208;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;-3456,-64;Inherit;False;165;Deform;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;334;-400,976;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;834;-3504,80;Inherit;False;830;Deform_Strength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;832;-3472,16;Inherit;False;829;DeformTex;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;242;-864,2096;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;856;-3280,3760;Inherit;True;Property;_Mask_Tex;Mask_Tex;68;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;256;-352,2176;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;209;-640,2064;Inherit;True;Property;_DissolveTex;DissolveTex;37;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;385;-352,2816;Inherit;False;Dissolve_Mask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;708;3440,1280;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;733;3824,1424;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;720;3424,1456;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;880;-288,2256;Inherit;False;Dissolve_Progress_ref;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;857;-2992,3824;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;885;-2784,3904;Inherit;False;Property;_Mask_Contrast;Mask_Contrast;72;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;261;0,2416;Inherit;False;Property;_Dissolve_smooth;Dissolve_smooth;44;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;398;-4304,-432;Inherit;False;Property;_FixedTiling;FixedTiling;4;0;Create;True;0;0;0;False;0;False;0;True;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;322;-3968,-176;Inherit;False;Property;_Main_Radial_Tiling;Main_Radial_Tiling;19;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;399;-4320,-208;Inherit;False;Property;_FixedTiling;FixedTiling;44;0;Create;True;0;0;0;False;0;False;0;True;Reference;398;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-3248,-128;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;352;-192,976;Inherit;False;Deform_Mask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;837;-3248,-208;Inherit;False;Property;_DeformType;DeformType;23;1;[Enum];Create;True;0;2;Add;0;Lerp;1;0;False;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;831;-3248,-16;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;255;-192,2112;Inherit;False;Property;_DissolveTex_Reverse;DissolveTex_Reverse;38;0;Create;True;0;0;0;False;0;False;0;False;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;388;-144,2032;Inherit;False;385;Dissolve_Mask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;718;3632,1280;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;735;3984,1424;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;743;3488,1632;Inherit;False;Property;_Fresnel_AlphaClipPixelate;Fresnel_AlphaClipPixelate;63;0;Create;True;0;0;0;False;0;False;4;2;1;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;260;256,2368;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;259;384,2368;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;881;-2560,3808;Inherit;False;880;Dissolve_Progress_ref;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;882;-2432,3664;Inherit;False;Property;_Mask_Strength_Mode;Mask_Strength_Mode;70;1;[Enum];Create;True;0;2;Property;0;Multiply_Dissolve_Progress;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;884;-2752,3824;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;865;-2784,3984;Inherit;False;Property;_Mask_Scale;Mask_Scale;76;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;886;-2816,4048;Inherit;False;Property;_Mask_ScaleOffset;Mask_ScaleOffset;77;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;296;-4048,-304;Inherit;False;Property;_Tiling_ParticleCustom1XY;Tiling_ParticleCustom1XY;16;0;Create;True;0;0;0;False;0;False;0;False;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;320;-3744,-208;Inherit;False;Polar Coordinates;-1;;6;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;3;FLOAT2;0;FLOAT;55;FLOAT;56
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;354;-2848,-48;Inherit;False;352;Deform_Mask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;835;-3040,-128;Inherit;False;int mode = (int)DeformType@$if (mode == 0) return Add@$else if (mode == 1) return Lerp@$else return Add@;2;Create;3;True;DeformType;INT;0;In;;Inherit;False;True;Add;FLOAT2;0,0;In;;Float;False;True;Lerp;FLOAT2;1,0;In;;Float;False;DeformMaskType;True;False;0;;False;3;0;INT;0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;251;-624,2272;Inherit;False;Property;_Dissolve_Progress;Dissolve_Progress;43;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;389;112,2032;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;779;-16,1952;Inherit;False;Property;_DissolveMask_BlendType;DissolveMask_BlendType;52;1;[Enum];Create;True;0;2;Add;0;Multiply;1;0;False;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;780;80,2128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;728;3792,1280;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;736;4096,1424;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;747;3776,1040;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;700;2976,672;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CameraDirection, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;772;2944,960;Inherit;False;0;1;FLOAT3;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;258;752,2176;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.01;False;2;FLOAT;11;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;867;-2576,4080;Inherit;False;Property;_Mask_Smooth;Mask_Smooth;75;0;Create;True;0;0;0;False;0;False;0;0.06250276;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;877;-2496,3872;Inherit;False;Property;_Mask_Strength;Mask_Strength;74;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Compare, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;883;-2336,3728;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;864;-2528,3936;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;252;-560,2336;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;285;-2432,-176;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;356;-2592,-240;Inherit;False;3;0;FLOAT2;0.5,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;321;-3504,-304;Inherit;False;Property;_Main_Radial;Main_Radial;18;0;Create;True;0;0;0;False;0;False;0;False;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;253;-336,2320;Inherit;False;Property;_Dissolve_Prog_ParticleCustom2W;Dissolve_Prog_ParticleCustom2W;42;0;Create;True;0;0;0;False;0;False;1;False;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;778;256,1984;Inherit;False;int mode = (int)MaskBlendType@$if (mode == 0) return Add@$else if (mode == 1) return Multiply@$else return Add@;1;Create;3;True;MaskBlendType;INT;0;In;;Inherit;False;True;Add;FLOAT;0;In;;Float;False;True;Multiply;FLOAT;1;In;;Float;False;BlendType;True;False;0;;False;3;0;INT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DdxOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;710;3648,960;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;745;3952,1040;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;734;4160,1280;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;703;3184,672;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;263;912,2176;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;869;-2192,4032;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;868;-2320,4032;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;876;-2224,3904;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;463;-4880,1344;Inherit;False;Pixelate;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;284;-2400,-16;Inherit;False;Property;_Main_Panning;Main_Panning;21;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;286;-2112,-192;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;-2432,-304;Inherit;False;Property;_Deform_Use;Deform_Use;22;0;Create;True;0;0;0;False;2;Header(___________________Deform___________________);Space(5);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;851;-128,-192;Inherit;False;Property;_Main_Alpha_Ch;Main_Alpha_Ch;11;1;[Enum];Create;True;0;2;A;0;R;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;390;496,2032;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;250;-16,2256;Inherit;False;3;0;FLOAT;-1;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;775;3296,1072;Inherit;False;Property;_Fresnel_AlphaClipPower;Fresnel_AlphaClipPower;64;0;Create;True;0;0;0;False;0;False;1.5;1;0;4;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;711;4352,1296;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.DdyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;713;3648,1152;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;746;4144,960;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;768;3312,672;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;870;-2064,3904;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;264;1040,2176;Inherit;False;Dissolve;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;110;-1872,0;Inherit;False;Property;_Main_Rotate;Main_Rotate;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;-1808,96;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;288;-2128,-80;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;289;-1984,-256;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;494;-1088,128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;486;-1440,16;Inherit;False;463;Pixelate;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Compare, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;850;-128,-128;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;254;640,2112;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;729;3424,896;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;773;2944,1072;Inherit;False;Property;_Fresnel_AlphaClipStepMin;Fresnel_AlphaClipStepMin;65;0;Create;True;0;0;0;False;0;False;0.05;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;774;2944,1136;Inherit;False;Property;_Fresnel_AlphaClipStepMax;Fresnel_AlphaClipStepMax;66;0;Create;True;0;0;0;False;0;False;0.6125;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;748;4144,1152;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;712;4320,960;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;859;-1904,3904;Inherit;False;Mask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;46;240,-96;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;267;-48,32;Inherit;False;Constant;_DissolveSwitchOff;DissolveSwitchOff;21;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;265;-16,96;Inherit;False;264;Dissolve;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;113;-1712,0;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;360;False;3;FLOAT;0;False;4;FLOAT;6.28;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;495;-992,112;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;496;-1184,16;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;290;-1840,-160;Inherit;False;Property;_Main_Pan_ParticleCustom1Z;Main_Pan_ParticleCustom1Z;20;0;Create;True;0;0;0;False;0;False;1;False;Create;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;750;3472,736;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;714;4320,1152;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;709;4464,912;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;860;384,192;Inherit;False;859;Mask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;862;416,128;Inherit;False;Constant;_MaskOff;MaskOff;70;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;879;400,-16;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;266;144,48;Inherit;False;Property;_Dissolve_Use;Dissolve_Use;36;0;Create;True;0;0;0;False;2;Header(___________________Dissolve___________________);Space(5);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;315;-464,-1264;Inherit;False;1277.244;391.9626;;12;313;310;311;300;314;312;304;307;306;305;301;303;Dissolve_Edge;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;298;832,2048;Inherit;False;Dissolve_Before_Smooth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-1520,-160;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCPixelate, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;497;-1056,-96;Inherit;True;4;0;FLOAT2;0,0;False;1;FLOAT;8;False;2;FLOAT;8;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;498;-896,112;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;715;4672,912;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;776;4624,672;Inherit;False;2;0;FLOAT;0.05;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;861;544,144;Inherit;False;Property;_Mask_Use;Mask_Use;67;0;Create;True;0;0;0;False;2;Header(___________________Mask___________________);Space(5);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;878;544,0;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;300;-432,-1216;Inherit;False;298;Dissolve_Before_Smooth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;313;-400,-1136;Inherit;False;Property;_Dissolve_Edge_Thick;Dissolve_Edge_Thick;49;0;Create;True;0;0;0;False;0;False;0.02;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;499;-704,-160;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;777;4816,672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;863;800,-16;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;872;928,-96;Inherit;False;Property;_Mask_BlendMode;Mask_BlendMode;73;1;[Enum];Create;True;0;2;Multiply;0;Add;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;871;832,64;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;307;-416,-992;Inherit;False;Property;_Dissolve_Edge_smooth;Dissolve_Edge_smooth;50;0;Create;True;0;0;0;False;0;False;0.9;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;314;-192,-1184;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;-496,-192;Inherit;True;Property;_MainTex;MainTex;10;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;640;544,-224;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;369;608,-192;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;721;4976,896;Inherit;False;2;0;FLOAT;0.5;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;361;880,176;Inherit;False;Property;_AlphaClip;AlphaClip;6;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Compare, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;873;928,-32;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;301;-48,-1168;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;305;-32,-1040;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;306;-160,-1040;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;45;-16,-592;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;639;560,-304;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;371;832,-416;Inherit;False;Property;_MainTex_ColorMode;MainTex_ColorMode;13;1;[Enum];Create;True;0;3;MainTex Color;0;R Channel Color;1;ParticleCustom 1W;2;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;893;512,-384;Inherit;False;HSVColorRound;0;;7;29d900e5f3beb4c47bbf7900d7e44865;0;1;11;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;368;880,-288;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;901;1424,-352;Inherit;False;Property;_Emission_Mode;Emission_Mode;14;1;[Enum];Create;True;0;2;property;0;Custom 1W;1;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;899;1232,-368;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;900;1296,-208;Inherit;False;Property;_Emission;Emission;15;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;766;5168,784;Inherit;False;Property;_Fresnel_AlphaClip;Fresnel_AlphaClip;62;0;Create;True;0;0;0;False;2;Header(___________________PixelFresnel___________________);Space (8);False;0;True;Create;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;363;1264,128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;362;1136,128;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;874;1104,-32;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;304;128,-1168;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.01;False;2;FLOAT;11;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;312;336,-1056;Inherit;False;Constant;_DissolveSwitchOff2;DissolveSwitchOff2;28;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;421;208,-592;Inherit;False;FLOAT3;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ConditionalIfNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;370;1052,-432;Inherit;False;False;5;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Compare, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;902;1424,-288;Inherit;False;0;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;317;-64,-400;Inherit;False;Property;_Main_Color;Main_Color;12;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;631;5472,784;Inherit;False;FresnelAlphaClip;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;367;1552,176;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1E-05;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;364;1392,48;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0.01;False;2;FLOAT;11;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;310;336,-992;Inherit;False;Property;_Dissolve_Use;Dissolve_Use;36;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;266;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;44;1248,-576;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;904;1616,-352;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;999;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;554;419.1758,464;Inherit;False;2306.852;488.5685;;22;573;572;555;568;567;571;563;566;564;562;565;558;559;549;545;546;547;544;542;548;532;592;Fresnel;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;365;1680,-80;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;641;1920,-128;Inherit;False;631;FresnelAlphaClip;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;303;592,-1184;Inherit;False;Property;_Dissolve_Edge_Color;Dissolve_Edge_Color;48;1;[HDR];Create;True;0;0;0;False;0;False;4,0.9547434,0.2641504,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;311;560,-992;Inherit;False;Property;_Use_Dissolve_Edge;Use_Dissolve_Edge;47;0;Create;True;0;0;0;False;0;False;0;False;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;903;1760,-608;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;532;432,736;Inherit;False;463;Pixelate;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;548;768,736;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;542;752,608;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;547;864,720;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;544;880,608;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;592;496,528;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;546;960,720;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;545;976,608;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;559;1360,608;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;549;1184,528;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;558;1536,528;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;572;1376,768;Inherit;False;Property;_FresnelMask_Soft;FresnelMask_Soft;60;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;562;1632,528;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;573;1472,832;Inherit;False;Property;_FresnelMask_Offset;FresnelMask_Offset;61;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;565;1664,672;Inherit;False;2;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;564;1872,592;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;566;1792,672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;563;1728,528;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;567;2016,672;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;568;2336,784;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;555;2512,784;Inherit;False;Fresnel;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;701;2976,816;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;571;2016,528;Inherit;False;Property;_Fresnel_Reverse;Fresnel_Reverse;59;0;Create;True;0;0;0;False;0;False;0;True;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;2320,-304;Inherit;False;Property;_CullMode;CullMode;5;1;[Enum];Create;True;0;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;2160,-304;Inherit;False;Property;_BlendMode;BlendMode;4;1;[Enum];Create;True;0;2;Additive;1;AlphaBlend;10;0;True;0;False;10;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;722;1920,-208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;299;1888,-704;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;894;2160,-512;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;14;New Amplify Shader;27c2e37ef0ae0ed4ba9ce8c439224f0b;True;Sprite Lit;0;0;Sprite Lit;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;True;1;False;;5;False;;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;False;True;1;False;;5;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;895;2160,-512;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;14;New Amplify Shader;27c2e37ef0ae0ed4ba9ce8c439224f0b;True;Sprite Normal;0;1;Sprite Normal;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;True;1;False;;5;False;;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=NormalsRendering;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;896;2160,-512;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;14;New Amplify Shader;27c2e37ef0ae0ed4ba9ce8c439224f0b;True;SceneSelectionPass;0;2;SceneSelectionPass;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;True;1;False;;5;False;;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;True;0;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;897;2160,-512;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;14;New Amplify Shader;27c2e37ef0ae0ed4ba9ce8c439224f0b;True;ScenePickingPass;0;3;ScenePickingPass;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;True;1;False;;5;False;;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;True;0;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;898;2160,-512;Float;False;True;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;14;LoadComplete/VFX/URP2D_FX_Master;27c2e37ef0ae0ed4ba9ce8c439224f0b;True;Sprite Forward;0;4;Sprite Forward;6;True;True;2;5;False;;0;True;_BlendMode;3;1;False;;10;False;;True;1;False;;5;False;;False;False;False;False;False;False;False;False;False;False;True;True;2;True;_CullMode;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=UniversalForward;False;False;0;;0;0;Standard;2;Vertex Position;1;0;Debug Display;0;0;0;5;True;True;True;True;True;False;;False;0
WireConnection;478;0;435;0
WireConnection;478;1;479;0
WireConnection;434;0;478;0
WireConnection;437;0;478;0
WireConnection;436;1;434;0
WireConnection;440;0;438;0
WireConnection;440;1;437;0
WireConnection;440;2;437;0
WireConnection;439;0;436;0
WireConnection;441;0;438;0
WireConnection;441;1;440;0
WireConnection;441;2;439;0
WireConnection;442;0;441;0
WireConnection;445;0;443;0
WireConnection;445;1;444;0
WireConnection;416;0;446;0
WireConnection;416;1;407;0
WireConnection;446;0;445;0
WireConnection;446;1;444;1
WireConnection;323;1;405;0
WireConnection;323;3;324;2
WireConnection;323;4;324;1
WireConnection;405;0;446;0
WireConnection;405;1;416;0
WireConnection;789;0;377;0
WireConnection;325;0;405;0
WireConnection;325;1;323;0
WireConnection;224;0;223;2
WireConnection;224;1;128;0
WireConnection;803;0;349;0
WireConnection;791;0;789;0
WireConnection;133;0;325;0
WireConnection;133;2;128;0
WireConnection;221;0;325;0
WireConnection;221;1;224;0
WireConnection;804;0;803;0
WireConnection;792;0;791;0
WireConnection;213;0;210;0
WireConnection;213;4;211;0
WireConnection;222;0;133;0
WireConnection;222;1;221;0
WireConnection;805;0;804;0
WireConnection;841;0;837;0
WireConnection;795;0;377;0
WireConnection;449;0;447;0
WireConnection;449;1;448;0
WireConnection;386;1;375;1
WireConnection;781;0;792;0
WireConnection;430;0;429;0
WireConnection;212;0;222;0
WireConnection;212;2;213;0
WireConnection;807;0;349;0
WireConnection;806;0;805;0
WireConnection;329;1;359;1
WireConnection;848;0;212;0
WireConnection;802;0;375;2
WireConnection;782;0;795;0
WireConnection;418;0;450;0
WireConnection;418;1;404;0
WireConnection;450;0;449;0
WireConnection;450;1;448;1
WireConnection;419;0;386;0
WireConnection;419;1;410;0
WireConnection;783;0;781;0
WireConnection;847;0;212;0
WireConnection;847;1;848;0
WireConnection;847;2;842;0
WireConnection;417;0;329;0
WireConnection;417;1;413;0
WireConnection;809;0;807;0
WireConnection;808;0;806;0
WireConnection;784;0;782;0
WireConnection;801;0;802;0
WireConnection;326;1;402;0
WireConnection;326;3;327;2
WireConnection;326;4;327;1
WireConnection;402;0;450;0
WireConnection;402;1;418;0
WireConnection;272;0;128;0
WireConnection;376;0;373;0
WireConnection;376;4;374;0
WireConnection;408;0;386;0
WireConnection;408;1;419;0
WireConnection;785;0;783;0
WireConnection;824;0;359;2
WireConnection;348;0;347;0
WireConnection;348;4;350;0
WireConnection;411;0;329;0
WireConnection;411;1;417;0
WireConnection;811;0;809;0
WireConnection;810;0;808;0
WireConnection;116;1;847;0
WireConnection;116;7;849;0
WireConnection;796;0;784;0
WireConnection;796;1;801;0
WireConnection;278;0;328;0
WireConnection;278;1;280;0
WireConnection;328;0;402;0
WireConnection;328;1;326;0
WireConnection;274;0;227;2
WireConnection;274;1;277;0
WireConnection;377;0;408;0
WireConnection;377;2;376;0
WireConnection;378;0;375;2
WireConnection;793;0;785;0
WireConnection;800;0;375;2
WireConnection;200;0;138;0
WireConnection;122;0;116;0
WireConnection;349;0;411;0
WireConnection;349;2;348;0
WireConnection;360;0;359;2
WireConnection;814;0;811;0
WireConnection;814;1;824;0
WireConnection;825;0;359;2
WireConnection;812;0;810;0
WireConnection;225;0;132;0
WireConnection;225;1;208;1
WireConnection;249;0;227;3
WireConnection;249;1;226;0
WireConnection;275;0;279;0
WireConnection;275;1;274;0
WireConnection;273;0;279;0
WireConnection;273;2;277;0
WireConnection;790;0;793;0
WireConnection;790;1;800;0
WireConnection;380;0;377;0
WireConnection;380;1;378;0
WireConnection;279;1;328;0
WireConnection;279;0;278;0
WireConnection;786;0;796;0
WireConnection;392;0;394;0
WireConnection;143;0;200;0
WireConnection;143;1;225;0
WireConnection;162;0;200;1
WireConnection;162;1;225;0
WireConnection;138;0;122;0
WireConnection;815;0;814;0
WireConnection;333;0;349;0
WireConnection;333;1;360;0
WireConnection;816;0;812;0
WireConnection;816;1;825;0
WireConnection;514;0;493;0
WireConnection;514;1;513;0
WireConnection;514;2;517;0
WireConnection;514;3;519;0
WireConnection;248;0;279;0
WireConnection;248;1;249;0
WireConnection;234;0;279;0
WireConnection;234;2;226;0
WireConnection;420;0;273;0
WireConnection;420;1;275;0
WireConnection;383;0;380;0
WireConnection;787;0;786;0
WireConnection;788;0;790;0
WireConnection;764;0;761;1
WireConnection;764;1;757;0
WireConnection;396;0;392;0
WireConnection;131;0;138;0
WireConnection;131;1;143;0
WireConnection;131;2;162;0
WireConnection;515;0;516;0
WireConnection;515;2;493;0
WireConnection;515;3;514;0
WireConnection;358;0;333;0
WireConnection;819;0;815;0
WireConnection;820;0;816;0
WireConnection;907;0;905;0
WireConnection;907;4;906;0
WireConnection;247;0;244;0
WireConnection;247;2;234;0
WireConnection;247;3;420;0
WireConnection;247;4;248;0
WireConnection;799;0;798;0
WireConnection;799;1;383;0
WireConnection;799;2;788;0
WireConnection;799;3;787;0
WireConnection;382;2;379;0
WireConnection;381;0;382;0
WireConnection;762;0;743;0
WireConnection;762;1;764;0
WireConnection;770;0;763;2
WireConnection;770;1;758;0
WireConnection;829;0;122;0
WireConnection;830;0;225;0
WireConnection;455;0;454;0
WireConnection;455;1;453;0
WireConnection;454;0;515;0
WireConnection;454;1;451;0
WireConnection;336;0;337;0
WireConnection;337;2;335;0
WireConnection;822;0;821;0
WireConnection;822;1;358;0
WireConnection;822;2;820;0
WireConnection;822;3;819;0
WireConnection;165;0;131;0
WireConnection;238;0;232;0
WireConnection;238;4;233;0
WireConnection;908;0;909;0
WireConnection;908;2;907;0
WireConnection;384;0;799;0
WireConnection;384;1;382;0
WireConnection;384;2;381;0
WireConnection;771;0;762;0
WireConnection;771;1;770;0
WireConnection;414;0;457;0
WireConnection;414;1;397;0
WireConnection;415;0;456;0
WireConnection;415;1;401;0
WireConnection;457;0;454;0
WireConnection;457;1;451;1
WireConnection;456;0;455;0
WireConnection;456;1;451;1
WireConnection;334;0;822;0
WireConnection;334;1;337;0
WireConnection;334;2;336;0
WireConnection;242;0;247;0
WireConnection;242;2;238;0
WireConnection;856;1;908;0
WireConnection;256;0;209;1
WireConnection;209;1;242;0
WireConnection;209;7;432;0
WireConnection;385;0;384;0
WireConnection;708;0;704;1
WireConnection;708;1;704;2
WireConnection;733;0;718;0
WireConnection;733;1;771;0
WireConnection;720;0;719;1
WireConnection;720;1;719;2
WireConnection;880;0;253;0
WireConnection;857;0;858;0
WireConnection;857;2;856;1
WireConnection;857;3;856;4
WireConnection;398;0;457;0
WireConnection;398;1;414;0
WireConnection;399;0;456;0
WireConnection;399;1;415;0
WireConnection;123;0;321;0
WireConnection;123;1;166;0
WireConnection;352;0;334;0
WireConnection;831;0;321;0
WireConnection;831;1;832;0
WireConnection;831;2;834;0
WireConnection;255;0;209;1
WireConnection;255;1;256;0
WireConnection;718;0;708;0
WireConnection;718;1;720;0
WireConnection;735;0;733;0
WireConnection;260;2;261;0
WireConnection;259;0;260;0
WireConnection;884;0;857;0
WireConnection;884;1;885;0
WireConnection;296;0;398;0
WireConnection;296;1;399;0
WireConnection;320;1;296;0
WireConnection;320;3;322;2
WireConnection;320;4;322;1
WireConnection;835;0;837;0
WireConnection;835;1;123;0
WireConnection;835;2;831;0
WireConnection;389;0;388;0
WireConnection;389;1;255;0
WireConnection;780;0;388;0
WireConnection;780;1;255;0
WireConnection;728;0;718;0
WireConnection;736;0;735;0
WireConnection;736;1;771;0
WireConnection;747;1;743;0
WireConnection;258;0;254;0
WireConnection;258;1;260;0
WireConnection;258;2;259;0
WireConnection;883;0;882;0
WireConnection;883;3;881;0
WireConnection;864;0;884;0
WireConnection;864;1;865;0
WireConnection;864;2;886;0
WireConnection;356;0;835;0
WireConnection;356;1;321;0
WireConnection;356;2;354;0
WireConnection;321;0;296;0
WireConnection;321;1;320;0
WireConnection;253;0;251;0
WireConnection;253;1;252;4
WireConnection;778;0;779;0
WireConnection;778;1;389;0
WireConnection;778;2;780;0
WireConnection;710;0;750;0
WireConnection;745;0;747;0
WireConnection;734;0;728;0
WireConnection;734;1;736;0
WireConnection;703;0;700;0
WireConnection;703;1;772;0
WireConnection;263;0;258;0
WireConnection;869;0;868;0
WireConnection;868;2;867;0
WireConnection;876;0;877;0
WireConnection;876;1;864;0
WireConnection;876;2;883;0
WireConnection;463;0;478;0
WireConnection;286;0;285;3
WireConnection;286;1;284;0
WireConnection;125;1;321;0
WireConnection;125;0;356;0
WireConnection;390;0;778;0
WireConnection;250;2;253;0
WireConnection;711;0;734;0
WireConnection;713;0;750;0
WireConnection;746;0;710;0
WireConnection;746;1;745;0
WireConnection;746;2;747;0
WireConnection;768;0;703;0
WireConnection;870;0;876;0
WireConnection;870;1;868;0
WireConnection;870;2;869;0
WireConnection;264;0;263;0
WireConnection;288;0;125;0
WireConnection;288;2;284;0
WireConnection;289;0;125;0
WireConnection;289;1;286;0
WireConnection;494;0;486;0
WireConnection;850;0;851;0
WireConnection;850;2;13;1
WireConnection;850;3;13;4
WireConnection;254;0;390;0
WireConnection;254;1;250;0
WireConnection;729;0;768;0
WireConnection;729;1;775;0
WireConnection;748;0;713;0
WireConnection;748;1;745;0
WireConnection;748;2;747;0
WireConnection;712;0;746;0
WireConnection;712;1;711;0
WireConnection;859;0;870;0
WireConnection;46;0;45;4
WireConnection;46;1;317;4
WireConnection;46;2;850;0
WireConnection;113;0;110;0
WireConnection;113;4;114;0
WireConnection;495;1;494;0
WireConnection;496;0;486;0
WireConnection;290;0;288;0
WireConnection;290;1;289;0
WireConnection;750;0;729;0
WireConnection;750;1;773;0
WireConnection;750;2;774;0
WireConnection;714;0;748;0
WireConnection;714;1;711;1
WireConnection;709;0;750;0
WireConnection;709;1;712;0
WireConnection;879;0;46;0
WireConnection;266;1;267;0
WireConnection;266;0;265;0
WireConnection;298;0;254;0
WireConnection;109;0;290;0
WireConnection;109;2;113;0
WireConnection;497;0;109;0
WireConnection;497;1;496;0
WireConnection;497;2;496;0
WireConnection;498;0;495;0
WireConnection;715;0;709;0
WireConnection;715;1;714;0
WireConnection;776;1;750;0
WireConnection;861;1;862;0
WireConnection;861;0;860;0
WireConnection;878;0;879;0
WireConnection;878;1;266;0
WireConnection;499;0;109;0
WireConnection;499;1;497;0
WireConnection;499;2;498;0
WireConnection;777;0;776;0
WireConnection;777;1;715;0
WireConnection;863;0;861;0
WireConnection;863;1;878;0
WireConnection;871;0;861;0
WireConnection;871;1;878;0
WireConnection;314;0;300;0
WireConnection;314;1;313;0
WireConnection;13;1;499;0
WireConnection;640;0;13;1
WireConnection;721;1;777;0
WireConnection;873;0;872;0
WireConnection;873;2;863;0
WireConnection;873;3;871;0
WireConnection;301;0;314;0
WireConnection;305;0;306;0
WireConnection;306;2;307;0
WireConnection;639;0;13;1
WireConnection;893;11;13;5
WireConnection;368;0;893;0
WireConnection;368;1;640;0
WireConnection;368;2;369;4
WireConnection;766;1;721;0
WireConnection;363;0;362;0
WireConnection;362;2;361;0
WireConnection;874;0;873;0
WireConnection;304;0;301;0
WireConnection;304;1;306;0
WireConnection;304;2;305;0
WireConnection;421;0;45;0
WireConnection;370;1;371;0
WireConnection;370;2;893;0
WireConnection;370;3;639;0
WireConnection;370;4;368;0
WireConnection;902;0;901;0
WireConnection;902;2;899;4
WireConnection;902;3;900;0
WireConnection;631;0;766;0
WireConnection;367;0;361;0
WireConnection;364;0;874;0
WireConnection;364;1;362;0
WireConnection;364;2;363;0
WireConnection;310;1;312;0
WireConnection;310;0;304;0
WireConnection;44;0;421;0
WireConnection;44;1;370;0
WireConnection;44;2;317;5
WireConnection;904;0;902;0
WireConnection;365;0;364;0
WireConnection;365;1;874;0
WireConnection;365;2;367;0
WireConnection;311;1;310;0
WireConnection;903;0;44;0
WireConnection;903;1;904;0
WireConnection;548;0;532;0
WireConnection;542;0;592;0
WireConnection;542;1;532;0
WireConnection;547;1;548;0
WireConnection;544;0;542;0
WireConnection;546;0;547;0
WireConnection;545;0;544;0
WireConnection;545;1;532;0
WireConnection;549;0;592;0
WireConnection;549;1;545;0
WireConnection;549;2;546;0
WireConnection;558;0;549;0
WireConnection;558;1;559;0
WireConnection;562;0;558;0
WireConnection;565;1;572;0
WireConnection;564;0;563;0
WireConnection;566;0;565;0
WireConnection;566;1;573;0
WireConnection;563;0;562;0
WireConnection;567;0;566;0
WireConnection;567;1;572;0
WireConnection;568;0;571;0
WireConnection;568;1;567;0
WireConnection;568;2;573;0
WireConnection;555;0;568;0
WireConnection;571;0;563;0
WireConnection;571;1;564;0
WireConnection;722;0;365;0
WireConnection;722;1;641;0
WireConnection;299;0;903;0
WireConnection;299;1;303;5
WireConnection;299;2;311;0
WireConnection;898;0;299;0
WireConnection;898;2;722;0
ASEEND*/
//CHKSM=12F104A9A30A78523AEE6567883C934CE0DE11DC