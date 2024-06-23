Shader "Custom/Rail"
{
    Properties
    {
		[Header(Rail Properties)][Space]
		_Length ("Length", Float) = 1

		[Header(Lighting)][Space]
		_BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1)

		[Space]

		_NormalMap("Normal Map", 2D) = "white" {}
		_NormalStrength("Normal Strength", Float) = 1

		[Header(Displacement)][Space]
		[NoScaleOffset] _NoiseTexture ("Noise Texture", 2D) = "white" {}

		[Space]

		_DisplacementStrength ("Displacement Strength", Float) = 0
		_DisplacementBreadth ("Displacement Breadth", Float) = 0
    }
    SubShader
    {
        Tags
		{ 
			"RenderPipeline" = "UniversalRenderPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}
		LOD 100

		Pass
		{
			Name "UniversalForward"
			Tags { "LightMode" = "UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vertex
			#pragma fragment Fragment
			
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            struct Attributes
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;

				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};

			struct Interpolators
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD2;
				float3 normalWS : TEXCOORD3;
				float4 tangentWS : TEXCOORD4;

				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;

				#ifdef _MAIN_LIGHT_SHADOWS
					float4 shadowCoord : TEXCOORD5;
				#endif
			};

			float4 _BaseColor;
			float _Length;

			TEXTURE2D(_NormalMap);
			SAMPLER(sampler_NormalMap);
			float4 _NormalMap_ST;
			float _NormalStrength;

			TEXTURE2D(_NoiseTexture);
			SAMPLER(sampler_NoiseTexture);

			float _DisplacementStrength;
			float _DisplacementBreadth;

			Interpolators Vertex(Attributes input)
			{
				Interpolators output;

				float noise = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture, input.positionOS.xy, 0).r;

				float3 positionOS = input.positionOS;
				positionOS += input.normalOS * noise * _DisplacementStrength;
				positionOS += input.normalOS * _DisplacementBreadth;

				VertexPositionInputs pInputs = GetVertexPositionInputs(positionOS);
				VertexNormalInputs nInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

				output.positionCS = pInputs.positionCS;
				output.positionWS = pInputs.positionWS;
				output.normalWS = nInputs.normalWS;
				output.tangentWS = float4(nInputs.tangentWS, input.tangentOS.w);
				
				output.uv0 = TRANSFORM_TEX(input.uv0, _NormalMap);
				output.uv1 = input.uv1;

				#ifdef _MAIN_LIGHT_SHADOWS
					output.shadowCoord = GetShadowCoord(pInputs);
				#endif

				return output;
			}

			float3 Diffuse(float3 normalWS, float4 shadowCoord)
			{
				Light mainLight = GetMainLight(shadowCoord);

				float NdotL = saturate(dot(normalWS, mainLight.direction));
				float3 mainDiffuse = mainLight.color * (mainLight.shadowAttenuation * mainLight.distanceAttenuation * NdotL);

				return mainDiffuse;
			}

			float4 Fragment(Interpolators input) : SV_TARGET
			{
				float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv0), _NormalStrength);
				float3x3 tangentToWorld = CreateTangentToWorld(input.normalWS, input.tangentWS.xyz, input.tangentWS.w);
				float3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));

				float3 bakedGI = SampleSH(normalWS);
				#ifdef _MAIN_LIGHT_SHADOWS
					float3 diffuse = Diffuse(normalWS, input.shadowCoord);
				#else
					float3 diffuse = Diffuse(normalWS, float4(0, 0, 0, 0));
				#endif

				float3 lit = bakedGI + diffuse;

				float a = step(input.uv1.y, _Length);

				return float4(lit * _BaseColor.rgb, a);
			}

            ENDHLSL
        }
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			// Majority copied from Shadows.hlsl but excluding all of the stuff that gave me errors

			float3 _LightDirection;
			float3 _LightPosition;
			float4 _ShadowBias;

			float _Length;

			float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
			{
				float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
			    float scale = invNdotL * _ShadowBias.y;

				positionWS = lightDirection * _ShadowBias.xxx + positionWS;
				positionWS = normalWS * scale.xxx + positionWS;
				return positionWS;
			}

			float4 GetShadowPositionHClip(Attributes input)
			{
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
			    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif
			
				return positionCS;
			}

			Varyings ShadowPassVertex(Attributes input)
			{
				Varyings output;

				output.positionCS = GetShadowPositionHClip(input);
				output.uv = input.uv;
				return output;
			}

			half4 ShadowPassFragment(Varyings input) : SV_TARGET
			{
				clip(_Length - input.uv.y);

				return 1.0f;
			}
			
			ENDHLSL
		}
    }
}
