using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KWS
{
    internal class KWS_WaterPassHandler
    {
        List<WaterPass> _waterPasses;

        //TileBasedReflectionProbePass _tileBasedReflectionProbePass = new();
        OrthoDepthPass                       _orthoDepthPass               = new();
        VolumetricLightingPrePass            _volumetricLightingPrePass    = new();
        FftWavesPass                         _fftWavesPass                 = new();
        BuoyancyPass                         _buoyancyPass                 = new();
        FlowPass                             _flowPass                     = new();
        DynamicWavesPass                     _dynamicWavesPass             = new();
        
        ShorelineWavesPass        _shorelineWavesPass        = new();
        WaterPrePass              _waterPrePass              = new();
        MotionVectorsPass         _motionVectorsPass         = new();
        CausticPrePass         _causticPrePass         = new();
        VolumetricLightingPass _volumetricLightingPass = new();
        CausticDecalPass       _causticDecalPass       = new();
        CopyColorPass          _copyColorPass          = new();
      
        ScreenSpaceReflectionPass _ssrPass             = new();
        ReflectionFinalPass       _reflectionFinalPass = new();
        DrawMeshPass              _drawMeshPass        = new();
        ShorelineFoamPass         _shorelineFoamPass   = new();
        UnderwaterPass             _underwaterPass            = new();
        DrawToPosteffectsDepthPass _drawToDepthPass           = new();



        internal KWS_WaterPassHandler()
        {
            _waterPasses = new List<WaterPass>
            {
                //_tileBasedReflectionProbePass, 
                _orthoDepthPass, _volumetricLightingPrePass, _fftWavesPass, _buoyancyPass, _flowPass, _dynamicWavesPass,
                _shorelineWavesPass, _waterPrePass, _motionVectorsPass, _causticPrePass, _volumetricLightingPass, _causticDecalPass, _copyColorPass,
                _ssrPass, _reflectionFinalPass, _drawMeshPass, _shorelineFoamPass, _underwaterPass, _drawToDepthPass
            };

            _drawToDepthPass.cameraEvent = CameraEvent.AfterForwardAlpha;
        }


        public void OnBeforeFrameRendering(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates)
        {
            foreach (var waterPass in _waterPasses) waterPass.ExecutePerFrame(cameras, fixedUpdates);
        }

        public void OnBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            try
            {

                var cameraSize = KWS_CoreUtils.GetScreenSizeLimited(KWS_CoreUtils.SinglePassStereoEnabled);
                KWS_CoreUtils.RTHandles.SetReferenceSize(cameraSize.x, cameraSize.y);

                WaterPass.WaterPassContext waterContext = default;
                waterContext.cam         = cam;
                waterContext.cameraDepth = cam.actualRenderingPath == RenderingPath.Forward ? BuiltinRenderTextureType.Depth : BuiltinRenderTextureType.ResolvedDepth;
                waterContext.cameraColor = BuiltinRenderTextureType.CameraTarget;

                foreach (var waterPass in _waterPasses)
                {
                    waterPass.ExecuteBeforeCameraRendering(cam);
                    //waterPass.ExecuteInjectionPointPass(waterContext); //cause bug with command buffer and editor camera
                }

                _shorelineWavesPass.ExecuteInjectionPointPass(waterContext);
                _waterPrePass.ExecuteInjectionPointPass(waterContext);
                //_tileBasedReflectionProbePass.ExecuteInjectionPointPass(waterContext);
                _motionVectorsPass.ExecuteInjectionPointPass(waterContext);
                _causticPrePass.ExecuteInjectionPointPass(waterContext);
                _volumetricLightingPass.ExecuteInjectionPointPass(waterContext);
                _causticDecalPass.ExecuteInjectionPointPass(waterContext);
                _copyColorPass.ExecuteInjectionPointPass(waterContext);
                _ssrPass.ExecuteInjectionPointPass(waterContext);
                _reflectionFinalPass.ExecuteInjectionPointPass(waterContext);
                _shorelineFoamPass.ExecuteInjectionPointPass(waterContext);
                _drawToDepthPass.ExecuteInjectionPointPass(waterContext);

            }
            catch (Exception e)
            {
                Debug.LogError("Water rendering error: " + e.Message + "    \r\n " + e.StackTrace);
            }
        }



        public void OnAfterCameraRendering(Camera cam)
        {
            foreach (var waterPass in _waterPasses) waterPass?.ReleaseCameraBuffer(cam);
        }

        public void Release()
        {
            if (_waterPasses != null)
            {
                foreach (var waterPass in _waterPasses)
                {
                    waterPass?.Release();
                }
            }

            _orthoDepthPass?.Release();
        }

    }
}