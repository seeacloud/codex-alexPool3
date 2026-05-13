using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.Visualization
{
    public class CueBallPathRenderer : DashLine
    {
        [Tooltip("方向线颜色")]
        public Color lineColor = new Color(0.22f, 0.93f, 0.95f, 1f);
        [Tooltip("每隔 N 个采样点取 1 个")]
        public int decimation = 2;

        LineRenderer lr;

        void Awake()
        {
            lineColor = new Color(0.22f, 0.93f, 0.95f, 1f);
            lr = CreateDashLineRenderer("LR_CuePath", lineColor);
        }

        public void Show(Vector3[] trajectory)
        {
            if (!ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.ManualCuePath))
            {
                Hide();
                return;
            }

            if (trajectory == null || trajectory.Length < 2)
            {
                Hide();
                return;
            }

            int n = trajectory.Length;
            int step = Mathf.Max(1, decimation);
            int count = (n + step - 1) / step;
            if (count < 2) count = 2;

            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int idx = Mathf.Min(i * step, n - 1);
                points[i] = trajectory[idx];
            }
            SetPolyline(lr, points);
        }

        public void Hide()
        {
            HideLine(lr);
        }
    }
}
