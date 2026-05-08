using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Red dashed line from the cue ball, through the target ball, extending to the
    /// nearest rail. Visualises the "if cue goes straight through" sight line.
    /// Independent of physics — purely a geometric reference.
    /// </summary>
    public class CueThroughTargetLineRenderer : DashLine
    {
        [Tooltip("红色参考线颜色")]
        public Color lineColor = new Color(1f, 0.2f, 0.2f, 1f);

        LineRenderer lr;

        void Awake()
        {
            lineColor.a = 1f;
            lr = CreateDashLineRenderer("LR_CueThroughTarget", lineColor);
        }

        public void Show(Vector3 cuePos, Vector3 targetPos, Vector3 railHitPos)
        {
            if (lr == null) return;
            lr.enabled = true;
            lr.positionCount = 3;
            lr.SetPosition(0, cuePos);
            lr.SetPosition(1, targetPos);
            lr.SetPosition(2, railHitPos);
        }

        public void Hide()
        {
            HideLine(lr);
        }
    }
}
