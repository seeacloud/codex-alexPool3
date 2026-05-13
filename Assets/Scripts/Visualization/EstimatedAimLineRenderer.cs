using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Magenta dashed reference from the near manual-aim hit point on the object ball,
    /// through the object ball center, and onward to the table boundary.
    /// </summary>
    public class EstimatedAimLineRenderer : DashLine
    {
        [Tooltip("估瞄线颜色")]
        public Color lineColor = new Color(1f, 0f, 1f, 1f);

        LineRenderer lr;

        void Awake()
        {
            lineColor = new Color(1f, 0f, 1f, 1f);
            lr = CreateDashLineRenderer("LR_EstimatedAimLine", lineColor);
        }

        public void Show(
            Vector3 cueBallCenter, Vector3 targetBallCenter, Vector3 aimDir,
            float ballRadius, TableController table)
        {
            if (!ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.EstimatedAimLine))
            {
                Hide();
                return;
            }

            if (!TryComputeLine(cueBallCenter, targetBallCenter, aimDir, ballRadius, table,
                    out var hitPoint, out var railPoint))
            {
                Hide();
                return;
            }

            if (lr == null) return;
            lr.enabled = true;
            lr.positionCount = 3;
            lr.SetPosition(0, hitPoint);
            lr.SetPosition(1, targetBallCenter);
            lr.SetPosition(2, railPoint);
        }

        public void Hide()
        {
            HideLine(lr);
        }

        public static bool TryComputeLine(
            Vector3 cueBallCenter, Vector3 targetBallCenter, Vector3 aimDir,
            float ballRadius, TableController table,
            out Vector3 hitPoint, out Vector3 railPoint)
        {
            hitPoint = Vector3.zero;
            railPoint = Vector3.zero;
            if (table == null || ballRadius <= 0f)
                return false;

            Vector3 dir = aimDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f)
                return false;
            dir.Normalize();

            Vector3 toTarget = targetBallCenter - cueBallCenter;
            toTarget.y = 0f;
            float along = Vector3.Dot(dir, toTarget);
            if (along < 0f)
                return false;

            float closestDistSqr = toTarget.sqrMagnitude - along * along;
            float radiusSqr = ballRadius * ballRadius;
            if (closestDistSqr > radiusSqr)
                return false;

            float offset = Mathf.Sqrt(Mathf.Max(0f, radiusSqr - closestDistSqr));
            float nearT = along - offset;
            if (nearT < 0f)
                return false;

            hitPoint = cueBallCenter + dir * nearT;
            hitPoint.y = targetBallCenter.y;

            Vector3 throughDir = targetBallCenter - hitPoint;
            throughDir.y = 0f;
            if (throughDir.sqrMagnitude < 1e-8f)
                return false;
            throughDir.Normalize();

            railPoint = ClipRayToPlayfield(targetBallCenter, throughDir, table);
            railPoint.y = targetBallCenter.y;
            return true;
        }

        static Vector3 ClipRayToPlayfield(Vector3 start, Vector3 dir, TableController table)
        {
            float halfLength = table.playfieldHalfLength;
            float halfWidth = table.playfieldHalfWidth;
            float best = float.MaxValue;

            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = (dir.x > 0f ? halfLength - start.x : -halfLength - start.x) / dir.x;
                if (tx > 0f && tx < best) best = tx;
            }

            if (Mathf.Abs(dir.z) > 1e-6f)
            {
                float tz = (dir.z > 0f ? halfWidth - start.z : -halfWidth - start.z) / dir.z;
                if (tz > 0f && tz < best) best = tz;
            }

            if (best == float.MaxValue)
                best = 0f;

            Vector3 hit = start + dir * best;
            hit.x = Mathf.Clamp(hit.x, -halfLength, halfLength);
            hit.z = Mathf.Clamp(hit.z, -halfWidth, halfWidth);
            return hit;
        }
    }
}
