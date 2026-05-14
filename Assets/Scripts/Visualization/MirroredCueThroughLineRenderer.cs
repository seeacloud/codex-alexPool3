using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Red dashed line made by mirroring cue-through-target around object-to-pocket.
    /// Chooses the mirrored side that brackets the object-to-pocket direction
    /// together with cue-through-target, then passes through the target center.
    /// </summary>
    public class MirroredCueThroughLineRenderer : DashLine
    {
        [Tooltip("镜像红线颜色")]
        public Color lineColor = new Color(1.8f, 0.12f, 0.65f, 1f);

        LineRenderer lr;

        void Awake()
        {
            lineColor = new Color(1.8f, 0.12f, 0.65f, 1f);
            lineColor.a = 1f;
            lr = CreateDashLineRenderer("LR_MirroredCueThrough", lineColor);
        }

        public void Show(
            Vector3 cueBallCenter, Vector3 targetBallCenter, Vector3 pottingPoint,
            float ballRadius, TableController table)
        {
            if (!ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.MirroredCueThroughTarget))
            {
                Hide();
                return;
            }

            if (!TryComputeLine(
                    cueBallCenter, targetBallCenter, pottingPoint, ballRadius, table,
                    out var nearHitPoint, out var railPoint))
            {
                Hide();
                return;
            }

            if (lr == null) return;
            lr.enabled = true;
            lr.positionCount = 3;
            lr.SetPosition(0, nearHitPoint);
            lr.SetPosition(1, targetBallCenter);
            lr.SetPosition(2, railPoint);
        }

        public void Hide()
        {
            HideLine(lr);
        }

        public static bool TryComputeLine(
            Vector3 cueBallCenter, Vector3 targetBallCenter, Vector3 pottingPoint,
            float ballRadius, TableController table,
            out Vector3 nearHitPoint, out Vector3 railPoint)
        {
            nearHitPoint = Vector3.zero;
            railPoint = Vector3.zero;
            if (table == null || ballRadius <= 0f)
                return false;

            Vector3 redDir = targetBallCenter - cueBallCenter;
            redDir.y = 0f;
            if (redDir.sqrMagnitude < 1e-8f)
                return false;
            redDir.Normalize();

            Vector3 greenDir = pottingPoint - targetBallCenter;
            greenDir.y = 0f;
            if (greenDir.sqrMagnitude < 1e-8f)
                return false;
            greenDir.Normalize();

            Vector3 mirrorDir = 2f * Vector3.Dot(redDir, greenDir) * greenDir - redDir;
            mirrorDir.y = 0f;
            if (mirrorDir.sqrMagnitude < 1e-8f)
                return false;
            mirrorDir.Normalize();

            Vector3 throughDir = ChooseBracketingMirrorDirection(redDir, greenDir, mirrorDir);

            if (throughDir.sqrMagnitude < 1e-8f)
                return false;
            throughDir.Normalize();

            nearHitPoint = targetBallCenter - throughDir * ballRadius;
            nearHitPoint.y = targetBallCenter.y;

            railPoint = PottingToleranceFanRenderer.ClipRayToPlayfield(targetBallCenter, throughDir, table);
            railPoint.y = targetBallCenter.y;
            return true;
        }

        static Vector3 ChooseBracketingMirrorDirection(Vector3 redDir, Vector3 greenDir, Vector3 mirrorDir)
        {
            float redSide = SignedFlatCross(greenDir, redDir);
            float mirrorSide = SignedFlatCross(greenDir, mirrorDir);

            if (Mathf.Abs(redSide) < 1e-6f || Mathf.Sign(redSide) != Mathf.Sign(mirrorSide))
                return mirrorDir;

            return -mirrorDir;
        }

        static float SignedFlatCross(Vector3 axis, Vector3 dir)
        {
            return axis.x * dir.z - axis.z * dir.x;
        }
    }
}
