using UnityEngine;

namespace KWS
{
    internal partial class KWS_UpdateManager
    {
        private KWS_WaterPassHandler _passHandler;

        void OnEnablePlatformSpecific()
        {
            //Debug.Log("Initialized update manager");

            Camera.onPreCull    += OnBeforeCameraRendering;
            Camera.onPostRender += OnAfterCameraRendering;

            if (_passHandler == null) _passHandler = new KWS_WaterPassHandler();
        }

        void OnDisablePlatformSpecific()
        {
            //Debug.Log("Removed update manager");

            Camera.onPreCull    -= OnBeforeCameraRendering;
            Camera.onPostRender -= OnAfterCameraRendering;

            _passHandler?.Release();
        }

        private void OnBeforeCameraRendering(Camera cam)
        {
            ExecutePerCamera(cam, default);
        }

        private void OnAfterCameraRendering(Camera cam)
        {
            _passHandler.OnAfterCameraRendering(cam);
        }

    }
}