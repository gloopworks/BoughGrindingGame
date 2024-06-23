Shader "Custom/Rail"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
		LOD 100

		Pass
		{
			Name "UniversalForward"
			Tags { "LightMode" = "UniversalForward" }

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
			};

			struct Interpolators
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD2;
				float3 normalWS : TEXCOORD3;

				#ifdef _MAIN_LIGHT_SHADOWS
					float4 shadowCoord : TEXCOORD4;
				#endif
			};

			float4 _BaseColor;

			Interpolators Vertex(Attributes input)
			{
				Interpolators output;
				
				VertexPositionInputs pInputs = GetVertexPositionInputs(input.positionOS);
				VertexNormalInputs nInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = pInputs.positionCS;
				output.positionWS = pInputs.positionWS;
				output.normalWS = nInputs.normalWS;

				#ifdef _MAIN_LIGHT_SHADOWS
					output.shadowCoord = GetShadowCoord(pInputs);
				#endif

				return output;
			}

			float3 Diffuse(Interpolators input)
			{
				#ifdef _MAIN_LIGHT_SHADOWS
					Light mainLight = GetMainLight(input.shadowCoord);
				#else
					Light mainLight = GetMainLight();
				#endif

				float NdotL = saturate(dot(input.normalWS, mainLight.direction));
				float3 mainDiffuse = mainLight.color * (mainLight.shadowAttenuation * mainLight.distanceAttenuation * NdotL);

				return mainDiffuse;
			}

			float4 Fragment(Interpolators input) : SV_TARGET
			{
				float3 bakedGI = SampleSH(input.normalWS);
				float3 diffuse = Diffuse(input);

				float3 lit = bakedGI + diffuse;

				return float4(lit * _BaseColor.rgb, 1);
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

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
			};

			float3 _LightDirection;
			float3 _LightPosition;
			float4 _ShadowBias;

			float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
			{
				float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
			    float scale = invNdotL * _ShadowBias.y;

				// normal bias is negative since we want to apply an inset normal offset
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
				return output;
			}

			half4 ShadowPassFragment(Varyings input) : SV_TARGET
			{
				return 0;
			}
			
			ENDHLSL
		}
    }
}
