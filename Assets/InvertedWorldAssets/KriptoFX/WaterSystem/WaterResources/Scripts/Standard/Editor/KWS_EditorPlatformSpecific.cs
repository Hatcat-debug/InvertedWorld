#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Linq;
using UnityEngine;

namespace KWS
{
    internal partial class KWS_Editor
    {
        void CheckPlatformSpecificMessages()
        {
            CheckPlatformSpecificMessages_VolumeLight();
            CheckPlatformSpecificMessages_Reflection();
        }

        void CheckPlatformSpecificMessages_VolumeLight()
        {
            if (_waterInstance.Settings.UseVolumetricLight && KWS_WaterLights.Lights.Count == 0) EditorGUILayout.HelpBox("Water->'Volumetric lighting' doesn't work because no lights has been added for water rendering! Add the script 'AddLightToWaterRendering' to your light.", MessageType.Error);
        }

        void CheckPlatformSpecificMessages_Reflection()
        {
            if (_waterInstance.Settings.ReflectSun)
            {
                if (KWS_WaterLights.Lights.Count == 0 || KWS_WaterLights.Lights.Count(l => l.Light.type == LightType.Directional) == 0)
                {
                    EditorGUILayout.HelpBox("'Water->Reflection->Reflect Sunlight' doesn't work because no directional light has been added for water rendering! Add the script 'AddLightToWaterRendering' to your directional light!", MessageType.Error);
                }
            }

            if (ReflectionProbe.defaultTexture.width == 1 && _waterInstance.Settings.OverrideSkyColor == false)
            {
                EditorGUILayout.HelpBox("Sky reflection doesn't work in this scene, you need to generate scene lighting! " + Environment.NewLine +
                                        "Open the \"Lighting\" window -> select the Generate Lighting option Reflection Probes", MessageType.Error);
            }
        }
    }

}
#endif