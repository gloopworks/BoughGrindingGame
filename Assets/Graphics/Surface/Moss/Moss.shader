Shader "Custom/Moss"
{
	Properties
	{
		[HideInInspector] _ShellIndex("Shell Index", Int) = 0
		
		_Length ("Length", Float) = 2

		[Header(Shell Settings)][Space]
		_ShellCount ("Shell Count", Int) = 8

		[Header(Noise)][Space]
		_NoiseTexture("Noise Texture", 2D) = "white" {}

		_NoiseMin("Noise Min", Range(0.0, 1.0)) = 0.0
		_NoiseMax("Noise Max", Range(0.0, 1.0)) = 1.0

		[Header(Color)][Space]
		_Albedo("Albedo", 2D) = "white" {}

		[Space]

		_BaseColor("Base Color", Color) = (0, 0, 0, 1)
		_TipColor("Tip Color", Color) = (1, 1, 1, 1)

		[Space]

		_FadeLength("Fade Length", Range(0.1, 1.0)) = 1.0

		[Header(Displacement)][Space]
		[NoScaleOffset] _DisplacementNoiseTexture ("Displacement Noise Texture", 2D) = "white" {}
		_DisplacementStrength ("Displacement Strength", Float) = 0
		
		[Space]

		_MinDisplacementBreadth ("Min Displacement Breadth", Float) = 0.1
		_MaxDisplacementBreadth ("Max Displacement Breadth", Float) = 0.2

		[Header(Blotches)][Space]
		_BlotchTexture ("Blotch Texture", 2D) = "white" {}
		_BlotchThreshold ("Blotch Threshold", Float) = 0.6
		_BlotchPower ("Blotch Power", Float) = 3
	}
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalRenderPipeline" }
		LOD 100

		Pass
		{
			Name "UniversalForward"
			Tags { "LightMode" = "UniversalForward" }
			Cull Off

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

				float2 uv : TEXCOORD0;
			};

			struct Interpolators
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD4;
				float3 normalWS : TEXCOORD5;

				float2 uv : TEXCOORD0;
				float2 baseUV : TEXCOORD1;
				float2 noiseUV : TEXCOORD2;
				float2 blotchUV : TEXCOORD3;

				#ifdef _MAIN_LIGHT_SHADOWS
					float4 shadowCoord : TEXCOORD6;
				#endif
			};

			int _ShellIndex, _ShellCount;
			
			TEXTURE2D(_NoiseTexture);
			float4 _NoiseTexture_ST;

			float _NoiseMin, _NoiseMax;

			TEXTURE2D(_Albedo);
			float4 _Albedo_ST;

			float4 _BaseColor, _TipColor;
			float _FadeLength;

			TEXTURE2D(_DisplacementNoiseTexture);
			float4 _DisplacementNoiseTexture_ST;

			float _DisplacementStrength, _MinDisplacementBreadth, _MaxDisplacementBreadth;

			TEXTURE2D(_BlotchTexture);
			float4 _BlotchTexture_ST;
			float _BlotchThreshold, _BlotchPower;

			float _Length;

			SamplerState sampler_point_repeat;
			SamplerState sampler_bilinear_repeat;

			Interpolators Vertex(Attributes input)
			{
				Interpolators output;

				float noise = SAMPLE_TEXTURE2D_LOD(_DisplacementNoiseTexture, sampler_point_repeat, input.positionOS.xy, 0).r;

				float3 positionOS = input.positionOS;
				positionOS += input.normalOS * noise * _DisplacementStrength;
				positionOS += input.normalOS * lerp(_MinDisplacementBreadth, _MaxDisplacementBreadth, (float)(_ShellIndex + 1) / (float)_ShellCount);
				
				VertexPositionInputs pInputs = GetVertexPositionInputs(positionOS);
				VertexNormalInputs nInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = pInputs.positionCS;
				output.positionWS = pInputs.positionWS;
				output.normalWS = nInputs.normalWS;

				output.uv = input.uv;
				output.baseUV = TRANSFORM_TEX(input.uv, _Albedo);
				output.noiseUV = TRANSFORM_TEX(input.uv, _NoiseTexture);
				output.blotchUV = TRANSFORM_TEX(input.uv, _BlotchTexture);

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
				float h = (float)_ShellIndex / (float)_ShellCount;

				float3 noiseSample = _NoiseTexture.Sample(sampler_point_repeat, input.noiseUV).rgb;
				float noise = lerp(_NoiseMin, _NoiseMax, noiseSample.r);

				float blotchSample = _BlotchTexture.Sample(sampler_bilinear_repeat, input.blotchUV).r;
				float blotch = pow(smoothstep(0, _BlotchThreshold, blotchSample), _BlotchPower);

				float inverseLength = step(_Length, input.uv.y);

				float threshold = (noise - h) - (1 - blotch) - inverseLength;
				
				if (threshold < 0) discard;

				float t = clamp(h / _FadeLength, 0, 1) * _Albedo.Sample(sampler_bilinear_repeat, input.baseUV).r;
				float3 unlit = lerp(_BaseColor.rgb, _TipColor.rgb, t);

				float3 bakedGI = SampleSH(input.normalWS);
				float3 diffuse = Diffuse(input);

				float3 lit = bakedGI + diffuse;

				return float4(unlit * lit, 1);
			}

			ENDHLSL
		}
	}
}