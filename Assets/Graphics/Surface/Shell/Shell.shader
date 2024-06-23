Shader "Custom/Shell"
{
	Properties
	{
		[HideInInspector] _ShellIndex("Shell Index", Int) = 0

		[Header(Shell Settings)][Space]
		_ShellCount ("Shell Count", Int) = 16
		_MaxShellLength ("Max Shell Length", Float) = 0.2

		[Header(Noise)][Space]
		_NoiseTexture("Noise Texture", 2D) = "white" {}

		_NoiseMin("Noise Min", Range(0.0, 1.0)) = 0.0
		_NoiseMax("Noise Max", Range(0.0, 1.0)) = 1.0

		[Header(Color)][Space]
		_BaseColor("Base Color", Color) = (0, 0, 0, 1)
		_TipColor("Tip Color", Color) = (1, 1, 1, 1)

		[Space]

		_FadeLength("Fade Length", Range(0.1, 1.0)) = 1.0
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
				float3 positionWS : TEXCOORD2;
				float3 normalWS : TEXCOORD3;

				float2 uv : TEXCOORD0;

				#ifdef _MAIN_LIGHT_SHADOWS
					float4 shadowCoord : TEXCOORD4;
				#endif
			};

			int _ShellIndex, _ShellCount;
			float _MaxShellLength;
			
			TEXTURE2D(_NoiseTexture);
			float4 _NoiseTexture_ST;

			float _NoiseMin, _NoiseMax;

			float4 _BaseColor, _TipColor;
			float _FadeLength;

			SamplerState sampler_point_repeat;

			Interpolators Vertex(Attributes input)
			{
				Interpolators output;

				float3 posOS = input.positionOS;
				posOS += input.normalOS * (_MaxShellLength / (float)_ShellCount) * (float)(_ShellIndex + 1);
				
				VertexPositionInputs pInputs = GetVertexPositionInputs(posOS);
				VertexNormalInputs nInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = pInputs.positionCS;
				output.positionWS = pInputs.positionWS;
				output.normalWS = nInputs.normalWS;

				output.uv = TRANSFORM_TEX(input.uv, _NoiseTexture);

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
				float3 noiseSample = _NoiseTexture.Sample(sampler_point_repeat, input.uv).rgb;
				float noise = lerp(_NoiseMin, _NoiseMax, noiseSample.r);
				
				float h = (float)_ShellIndex / (float)_ShellCount;
				if (noise < h) discard;

				float3 ao = lerp(_BaseColor.rgb, _TipColor.rgb, clamp(h / _FadeLength, 0, 1));

				float3 bakedGI = SampleSH(input.normalWS);
				float3 diffuse = Diffuse(input);

				float3 lit = bakedGI + diffuse;

				return float4(lit * ao, 1);
			}

			ENDHLSL
		}
	}
}