using UnityEngine;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Trajectory
{
    /// <summary>
    /// Pure-geometry shot predictor: assumes a perfect cut (aim hits the ghost ball
    /// center exactly). The target ball's post-impact direction is the ideal
    /// "target → pocket" vector, clipped at the first rail or pocket intersection
    /// (no rail bounces). Cue ball path is the straight aim line to the contact
    /// point, then a short perpendicular deflection.
    /// </summary>
    public class ShotSimulator : MonoBehaviour
    {
        [Tooltip("袋口捕捉半径（米）——子球进入此半径视为入袋")]
        public float pocketCatchRadius = 0.07f;

        [Tooltip("主球撞击后的偏折显示长度（米），仅可视化用，不表示真实距离")]
        public float cueAfterImpactDistance = 0.3f;

        public SimulationResult Run(
            Vector3 cuePos, Vector3 targetPos, Vector3 pocketPos,
            float ballRadius, TableController table)
        {
            var result = new SimulationResult();

            // Ideal direction the target ball will travel once hit at the ghost-ball contact point.
            Vector3 targetDir = (pocketPos - targetPos);
            if (targetDir.sqrMagnitude < 1e-8f)
            {
                result.state = TargetBallEndState.NotHit;
                return result;
            }
            targetDir = targetDir.normalized;

            // Clip the target ball's straight path at the first rail or pocket it reaches.
            float tRail = FirstRailHit(targetPos, targetDir, table, ballRadius);
            int pocketIdx;
            float tPocket = FirstPocketHit(targetPos, targetDir, table, pocketCatchRadius, out pocketIdx);

            float t;
            if (tPocket < tRail)
            {
                t = tPocket;
                result.state = TargetBallEndState.InPocket;
                result.pocketHitPos = table.Pockets[pocketIdx].Position;
                result.targetBallEndPos = result.pocketHitPos;
            }
            else
            {
                t = tRail;
                Vector3 endCenter = targetPos + targetDir * t;
                endCenter.y = ballRadius;
                result.state = TargetBallEndState.AgainstRail;
                result.targetBallEndPos = endCenter;
            }

            result.targetBallTrajectory = new[] { targetPos, result.targetBallEndPos };

            // Cue ball geometric path: from cue to ghost contact point, then a perpendicular
            // deflection segment. Deflection direction is aim minus its component along targetDir
            // (classic elastic collision tangent rule).
            Vector3 ghostPos = targetPos - targetDir * (2f * ballRadius);
            Vector3 aimDir = (ghostPos - cuePos).normalized;
            Vector3 cueAfterDir = aimDir - Vector3.Dot(aimDir, targetDir) * targetDir;
            if (cueAfterDir.sqrMagnitude < 1e-8f)
            {
                result.cueBallTrajectory = new[] { cuePos, ghostPos };
            }
            else
            {
                cueAfterDir = cueAfterDir.normalized;
                Vector3 cueEnd = ghostPos + cueAfterDir * cueAfterImpactDistance;
                cueEnd.y = ballRadius;
                result.cueBallTrajectory = new[] { cuePos, ghostPos, cueEnd };
            }

            return result;
        }

        // Distance (in meters along dir) to the first rail wall, treating the ball center
        // boundary as (halfLength - ballRadius, halfWidth - ballRadius). Returns a large
        // finite value if the direction never hits a rail (should not happen with normalized dir).
        static float FirstRailHit(Vector3 start, Vector3 dir, TableController table, float ballRadius)
        {
            float maxX = table.playfieldHalfLength - ballRadius;
            float maxZ = table.playfieldHalfWidth - ballRadius;
            float tBest = float.MaxValue;
            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = (dir.x > 0 ? (maxX - start.x) : (-maxX - start.x)) / dir.x;
                if (tx > 0f && tx < tBest) tBest = tx;
            }
            if (Mathf.Abs(dir.z) > 1e-6f)
            {
                float tz = (dir.z > 0 ? (maxZ - start.z) : (-maxZ - start.z)) / dir.z;
                if (tz > 0f && tz < tBest) tBest = tz;
            }
            return tBest;
        }

        // Distance along dir to the first pocket whose catch-sphere is entered. Returns float.MaxValue
        // and pocketIdx = -1 if no pocket is hit.
        static float FirstPocketHit(Vector3 start, Vector3 dir, TableController table, float catchRadius, out int pocketIdx)
        {
            pocketIdx = -1;
            float tBest = float.MaxValue;
            for (int i = 0; i < table.Pockets.Count; i++)
            {
                var p = table.Pockets[i];
                Vector3 toPocket = p.Position - start;
                float tClosest = Vector3.Dot(toPocket, dir);
                if (tClosest < 0f) continue; // pocket behind
                Vector3 closest = start + dir * tClosest;
                Vector3 delta = p.Position - closest;
                delta.y = 0f;
                float d2 = delta.sqrMagnitude;
                float r2 = catchRadius * catchRadius;
                if (d2 > r2) continue;
                float h = Mathf.Sqrt(r2 - d2);
                float tEnter = tClosest - h;
                if (tEnter < 0f) tEnter = 0f;
                if (tEnter < tBest) { tBest = tEnter; pocketIdx = i; }
            }
            return tBest;
        }
    }
}
