using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KWS
{
    internal static partial class KWS_CoreUtils
    {
        private static Light _lastDirLight;
        private static float _sunIntensityBeforeCameraEnterUnderwater;
        private static float _environmentLightingBeforeCameraEnterUnderwater;


        static bool CanRenderWaterForCurrentCamera_PlatformSpecific(Camera cam)
        {
            return true;
        }

        public static Vector2Int GetCameraRTHandleViewPortSize(Camera cam)
        {
#if ENABLE_VR_MODULE && ENABLE_VR && ENABLE_XR_MODULE
            if (XRSettings.enabled)
            {
                return new Vector2Int(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight);
            }
            else
#endif
            {
                var viewPortSize = RTHandles.rtHandleProperties.currentViewportSize;
                if (viewPortSize.x == 0 || viewPortSize.y == 0) return new Vector2Int(cam.pixelWidth, cam.pixelHeight);
                else return viewPortSize;
            }

        }

        public static bool CanRenderSinglePassStereo(Camera cam)
        {
#if ENABLE_VR_MODULE && ENABLE_VR && ENABLE_XR_MODULE
            return XRSettings.enabled &&
                   (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced && cam.cameraType != CameraType.SceneView);
#else
            return false;
#endif
        }

        public static bool IsSinglePassStereoActive()
        {
#if ENABLE_VR_MODULE && ENABLE_VR && ENABLE_XR_MODULE
            return XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced;
#else
            return false;
#endif
        }

        public static void UniversalCameraRendering(WaterSystem waterInstance, Camera camera)
        {
            camera.Render();
        }

        public static void SetPlatformSpecificPlanarReflectionParams(Camera reflCamera)
        {

        }

        public static void UpdatePlatformSpecificPlanarReflectionParams(Camera reflCamera, WaterSystem waterInstance)
        {
            //if (waterInstance.Settings.UseScreenSpaceReflection && waterInstance.Settings.UseAnisotropicReflections)
            //{
            //    reflCamera.clearFlags      = CameraClearFlags.Color;
            //    reflCamera.backgroundColor = Color.black;
            //}
            //else
            //{
            //    reflCamera.clearFlags      = CameraClearFlags.Skybox;
            //}
        }


        public static void SetComputeShadersDefaultPlatformSpecificValues(this CommandBuffer cmd, ComputeShader cs, int kernel)
        {

        }

        public static void RenderDepth(Camera depthCamera, RenderTexture depthRT)
        {
            var currentShadowDistance = QualitySettings.shadowDistance;
            var lodBias               = QualitySettings.lodBias;

            var terrains                                            = Terrain.activeTerrains;
            var pixelError                                          = new float[terrains.Length];
            for (var i = 0; i < terrains.Length; i++) pixelError[i] = terrains[i].heightmapPixelError;

            try
            {
                QualitySettings.shadowDistance = 0;
                QualitySettings.lodBias        = 10;
                foreach (var terrain in terrains) terrain.heightmapPixelError = 1;

                depthCamera.targetTexture = depthRT;
                depthCamera.Render();
                
            }
            finally
            {
                for (var i = 0; i < terrains.Length; i++)
                {
                    terrains[i].heightmapPixelError = pixelError[i];
                }

                QualitySettings.shadowDistance = currentShadowDistance;
                QualitySettings.lodBias        = lodBias;
            }
        }
    }

}