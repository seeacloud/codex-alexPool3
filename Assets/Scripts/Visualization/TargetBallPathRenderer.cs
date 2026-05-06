using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Draws the object ball's physically-simulated trajectory as a polyline.
    /// Unlike AimLineRenderer.objectToPocket (which is a straight ideal line
    /// from target to pocket), this renderer shows the ACTUAL path including
    /// rail bounces and post-impact motion.
    /// </summary>
    public class TargetBallPathRenderer : MonoBehaviour
    {
        public LineRenderer line;

        [Tooltip("每隔 N 个采样点取 1 个，减少顶点数（1 = 全量）")]
        public int decimation = 1;

        public void Show(Vector3[] trajectory)
        {
            if (line == null) return;
            if (trajectory == null || trajectory.Length < 2)
            {
                line.enabled = false;
                return;
            }

            int n = trajectory.Length;
            int step = Mathf.Max(1, decimation);
            int count = (n + step - 1) / step;
            if (count < 2) count = 2;

            line.enabled = true;
            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                int idx = Mathf.Min(i * step, n - 1);
                line.SetPosition(i, trajectory[idx]);
            }
        }

        public void Hide()
        {
            if (line != null) line.enabled = false;
        }
    }
}
