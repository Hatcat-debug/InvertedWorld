Shader "Hidden/KriptoFX/KWS/VolumetricLighting"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" { }
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.6

			#pragma multi_compile_fragment _ KWS_USE_DIR_LIGHT

			#pragma multi_compile_fragment _ KWS_USE_POINT_LIGHTS
			#pragma multi_compile_fragment _ KWS_USE_SHADOW_POINT_LIGHTS
			#pragma multi_compile_fragment _ KWS_USE_SPOT_LIGHTS
			#pragma multi_compile_fragment _ KWS_USE_SHADOW_SPOT_LIGHTS

			#pragma multi_compile_fragment _ USE_CAUSTIC USE_ADDITIONAL_CAUSTIC
			//#pragma multi_compile_fragment _ USE_UNDERWATER_REFLECTION
			#pragma multi_compile_fragment _ KWS_USE_AQUARIUM_RENDERING

			#ifdef SHADER_API_VULKAN
				#define KWS_DISABLE_POINT_SPOT_SHADOWS
			#endif

			#include "../PlatformSpecific/Includes/KWS_HelpersIncludes.cginc"
			#include "KWS_Lighting.cginc"
			#include "../Common/CommandPass/KWS_VolumetricLight_Common.cginc"
			//	#define SAMPLE_POINT_SHADOW
			
			inline void IntegrateAdditionalLight(RaymarchData raymarchData, inout float3 scattering, inout float transmittance, float atten, float3 lightPos, float3 step, inout float3 currentPos)
			{
				float3 posToLight = normalize(currentPos - lightPos.xyz);
				
				#if defined(USE_ADDITIONAL_CAUSTIC)
					if (lightPos.y > raymarchData.waterHeight)
					{
						atten += atten * RaymarchCaustic(raymarchData, currentPos, posToLight);
					}
				#endif
				
				half cosAngle = dot(-raymarchData.rayDir, posToLight);
				atten += atten * MieScattering(cosAngle) * 5;

				IntegrateLightSlice(scattering, transmittance, atten, raymarchData);
				currentPos += step;
			}

			inline void RayMarchDirLight(RaymarchData raymarchData, inout RaymarchResult result)
			{
				result.DirLightScattering = 0;
				result.DirLightSurfaceShadow = 1;
				result.DirLightSceneShadow = 1;

				
				float3 finalScattering = 0;
				//* GetVolumeLightInDepthTransmitance(raymarchData.waterHeight, GetCameraAbsolutePosition().y, raymarchData.transparent);
				float transmittance = 1;
				
				#if defined(KWS_USE_DIR_LIGHT)
					float3 currentPos = raymarchData.currentPos;
					
					ShadowLightData light = KWS_DirLightsBuffer[0];
					
					float sunAngleAttenuation = GetVolumeLightSunAngleAttenuation(light.forward.xyz);
					finalScattering = GetAmbientColor(GetExposure()) * 0.5;
					finalScattering *= GetVolumeLightInDepthTransmitance(raymarchData.waterHeight, currentPos.y, raymarchData.transparent, raymarchData.waterID);
					finalScattering *= sunAngleAttenuation;

					float3 waterPos = GetWorldSpacePositionFromDepth(raymarchData.uv, GetWaterDepth(raymarchData.uv));


					float3 step = raymarchData.step;
					float3 reflectedStep = reflect(raymarchData.rayDir, float3(0, -1, 0)) * (raymarchData.rayLength / KWS_RayMarchSteps);

					UNITY_LOOP
					for (uint i = 0; i < KWS_RayMarchSteps; ++i)
					{
						if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToSceneZ) break;
						if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToWaterZ) step = reflectedStep;
						
						half atten = 1;
						UNITY_BRANCH
						if (KWS_UseDirLightShadow == 1)	atten *= DirLightRealtimeShadow(0, currentPos);
						
						#if defined(USE_CAUSTIC) || defined(USE_ADDITIONAL_CAUSTIC)
							atten += atten * RaymarchCaustic(raymarchData, currentPos, light.forward);
						#endif
						atten *= sunAngleAttenuation;
						atten *= GetVolumeLightInDepthTransmitance(raymarchData.waterHeight, currentPos.y, raymarchData.transparent, raymarchData.waterID);
						
						IntegrateLightSlice(finalScattering, transmittance, atten, raymarchData);
						
						currentPos += step;
					}
					
					finalScattering *= light.color * raymarchData.tubidityColor;
					
					result.DirLightSurfaceShadow = DirLightRealtimeShadow(0, raymarchData.rayStart);
					#if defined(USE_CAUSTIC) || defined(USE_ADDITIONAL_CAUSTIC)
						result.DirLightSceneShadow = DirLightRealtimeShadow(0, raymarchData.rayEnd);
					#endif
				#endif
				
				result.DirLightScattering = finalScattering;
			}


			inline void RayMarchAdditionalLights(RaymarchData raymarchData, inout RaymarchResult result)
			{
				result.AdditionalLightsScattering = 0;
				result.AdditionalLightsSceneAttenuation = 0;

				
				float3 reflectedStep = reflect(raymarchData.rayDir, float3(0, -1, 0)) * (raymarchData.rayLength / KWS_RayMarchSteps);

				#if KWS_USE_POINT_LIGHTS
					UNITY_LOOP
					for (uint pointIdx = 0; pointIdx < KWS_PointLightsCount; pointIdx++)
					{

						float3 scattering = 0;
						float transmittance = 1;
						LightData light = KWS_PointLightsBuffer[pointIdx];
						result.AdditionalLightsSceneAttenuation = max(result.AdditionalLightsSceneAttenuation, PointLightAttenuation(pointIdx, raymarchData.rayEnd));
						
						float3 step = raymarchData.step;
						float3 currentPos = raymarchData.currentPos;
						UNITY_LOOP
						for (uint i = 0; i < KWS_RayMarchSteps; ++i)
						{
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToSceneZ) break;
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToWaterZ) step = reflectedStep;

							half atten = PointLightAttenuation(pointIdx, currentPos);
							IntegrateAdditionalLight(raymarchData, scattering, transmittance, atten, light.position, step, currentPos);
						}
						result.AdditionalLightsScattering += scattering * light.color * raymarchData.tubidityColor;
					}

				#endif

				#if KWS_USE_SHADOW_POINT_LIGHTS
					
					UNITY_LOOP
					for (uint shadowPointIdx = 0; shadowPointIdx < KWS_ShadowPointLightsCount; shadowPointIdx++)
					{
						float3 scattering = 0;
						float transmittance = 1;
						ShadowLightData light = KWS_ShadowPointLightsBuffer[shadowPointIdx];
						result.AdditionalLightsSceneAttenuation = max(result.AdditionalLightsSceneAttenuation, PointLightAttenuationShadow(shadowPointIdx, raymarchData.rayEnd));
						
						float3 step = raymarchData.step;
						float3 currentPos = raymarchData.currentPos;
						UNITY_LOOP
						for (uint i = 0; i < KWS_RayMarchSteps; ++i)
						{
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToSceneZ) break;
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToWaterZ) step = reflectedStep;

							float atten = PointLightAttenuationShadow(shadowPointIdx, currentPos);
							IntegrateAdditionalLight(raymarchData, scattering, transmittance, atten, light.position, step, currentPos);
						}
						result.AdditionalLightsScattering += scattering * light.color * raymarchData.tubidityColor;
					}
				#endif



				#if KWS_USE_SPOT_LIGHTS
					UNITY_LOOP
					for (uint spotIdx = 0; spotIdx < KWS_SpotLightsCount; spotIdx++)
					{
						float3 scattering = 0;
						float transmittance = 1;
						LightData light = KWS_SpotLightsBuffer[spotIdx];
						result.AdditionalLightsSceneAttenuation = max(result.AdditionalLightsSceneAttenuation, SpotLightAttenuation(spotIdx, raymarchData.rayEnd));
						
						float3 step = raymarchData.step;
						float3 currentPos = raymarchData.currentPos;
						UNITY_LOOP
						for (uint i = 0; i < KWS_RayMarchSteps; ++i)
						{
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToSceneZ) break;
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToWaterZ) step = reflectedStep;

							float atten = SpotLightAttenuation(spotIdx, currentPos);
							IntegrateAdditionalLight(raymarchData, scattering, transmittance, atten, light.position, step, currentPos);
						}
						result.AdditionalLightsScattering += scattering * light.color * raymarchData.tubidityColor;
					}
				#endif

				#if KWS_USE_SHADOW_SPOT_LIGHTS

					UNITY_LOOP
					for (uint shadowSpotIdx = 0; shadowSpotIdx < KWS_ShadowSpotLightsCount; shadowSpotIdx++)
					{
						float3 scattering = 0;
						float transmittance = 1;
						ShadowLightData light = KWS_ShadowSpotLightsBuffer[shadowSpotIdx];
						result.AdditionalLightsSceneAttenuation = max(result.AdditionalLightsSceneAttenuation, SpotLightAttenuationShadow(shadowSpotIdx, raymarchData.rayEnd));
						
						float3 step = raymarchData.step;
						float3 currentPos = raymarchData.currentPos;
						UNITY_LOOP
						for (uint i = 0; i < KWS_RayMarchSteps; ++i)
						{
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToSceneZ) break;
							if (length(currentPos - raymarchData.rayStart) > raymarchData.rayLengthToWaterZ) step = reflectedStep;

							float atten = SpotLightAttenuationShadow(shadowSpotIdx, currentPos);
							IntegrateAdditionalLight(raymarchData, scattering, transmittance, atten, light.position, step, currentPos);
						}
						result.AdditionalLightsScattering += scattering * light.color * raymarchData.tubidityColor;
					}
				#endif
			}

			void frag(vertexOutput i, out half3 volumeLightColor : SV_Target0, out half3 additionalData : SV_Target1)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float waterMask = GetWaterMask(i.uv);
				if (waterMask == 0) discard;
				
				RaymarchData raymarchData = InitRaymarchData(i, waterMask);
				RaymarchResult raymarchResult = (RaymarchResult)0;

				RayMarchDirLight(raymarchData, raymarchResult);
				RayMarchAdditionalLights(raymarchData, raymarchResult);
				
				volumeLightColor = raymarchResult.DirLightScattering + raymarchResult.AdditionalLightsScattering + MIN_THRESHOLD;
				additionalData = float3(raymarchResult.DirLightSurfaceShadow, raymarchResult.DirLightSceneShadow, raymarchResult.AdditionalLightsSceneAttenuation);

				AddTemporalAccumulation(raymarchData.rayEnd, volumeLightColor);
			}
			
			ENDCG
		}
	}
}