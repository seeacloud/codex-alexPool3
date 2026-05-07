using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Keeps attached LineRenderers visually the same width on screen regardless
    /// of camera distance. Each frame: widthMultiplier = baseWidth * distance / referenceDistance.
    ///
    /// Attach to the same GameObject as AimLineRenderer / TargetBallPathRenderer /
    /// CueBallPathRenderer, or to their child LineRenderer objects.
    /// </summary>
    public class LineWidthCompensator : MonoBehaviour
    {
        [Tooltip("参考距离（米），在该距离下 LineRenderer 用其原始宽度")]
        public float referenceDistance = 2.5f;

        [Tooltip("参考点（通常是台面中心）")]
        public Transform referencePoint;

        Camera cam;
        LineRenderer[] lines;
        float[] baseWidths;

        void Start()
        {
            cam = Camera.main;
            lines = GetComponentsInChildren<LineRenderer>(true);
            baseWidths = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                baseWidths[i] = lines[i].startWidth;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || lines == null) return;

            Vector3 refPos = referencePoint != null ? referencePoint.position : Vector3.zero;
            float dist = Vector3.Distance(cam.transform.position, refPos);
            float scale = dist / Mathf.Max(0.01f, referenceDistance);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                float w = baseWidths[i] * scale;
                lines[i].startWidth = w;
                lines[i].endWidth = w;
            }
        }
    }
}
