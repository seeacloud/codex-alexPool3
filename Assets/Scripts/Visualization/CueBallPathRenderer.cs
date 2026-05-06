using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Renders the cue ball's physically-simulated trajectory (before and after
    /// contact with the target ball). Distinct from AimLineRenderer.cueToGhost
    /// which is just the intent line from cue to ghost aim point.
    /// </summary>
    public class CueBallPathRenderer : MonoBehaviour
    {
        public LineRenderer line;

        [Tooltip("每隔 N 个采样点取 1 个（1 = 全量）")]
        public int decimation = 2;

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
