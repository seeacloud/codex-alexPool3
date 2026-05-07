using UnityEngine;

namespace PoolAimTrainer.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraAntiAliasingSetup : MonoBehaviour
    {
        void Awake()
        {
            if (QualitySettings.antiAliasing < 4)
                QualitySettings.antiAliasing = 4;

            // URP camera AA is set via UniversalAdditionalCameraData.
            // Access via reflection to avoid hard dependency on URP assembly.
            var cam = GetComponent<Camera>();
            var dataComp = cam.GetComponent("UniversalAdditionalCameraData");
            if (dataComp != null)
            {
                var type = dataComp.GetType();
                var aaProp = type.GetProperty("antialiasing");
                if (aaProp != null) aaProp.SetValue(dataComp, 2); // 2 = SMAA
                var aaQualProp = type.GetProperty("antialiasingQuality");
                if (aaQualProp != null) aaQualProp.SetValue(dataComp, 2); // 2 = High
            }
        }
    }
}
