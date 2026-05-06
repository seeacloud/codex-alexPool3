using UnityEngine;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Trajectory
{
    /// <summary>
    /// Pure-geometry shot predictor. Takes a cue aim DIRECTION (not a pocket) and computes:
    ///   - Whether the cue ball hits the target ball (ray-sphere vs sphere(target, 2R))
    ///   - Ghost position (cue ball center at moment of impact)
    ///   - Target ball's post-impact direction (= collision normal)
    ///   - Target path clipped at first rail/pocket (no bounces)
    ///   - Cue ball deflection direction after impact (tangent-rule)
    /// </summary>
    public class ShotSimulator : MonoBehaviour
    {
        [Tooltip("袋口捕捉半径（米）")]
        public float pocketCatchRadius = 0.07f;

        [Tooltip("主球撞击后的偏折显示长度（米，仅可视化）")]
        public float cueAfterImpactDistance = 0.3f;

        public SimulationResult Run(
            Vector3 cuePos, Vector3 targetPos, Vector3 aimDir,
            float ballRadius, TableController table)
        {
            var result = new SimulationResult();
            aimDir = aimDir.normalized;

            // Ray-sphere intersection: does the ray (cuePos, aimDir) hit sphere(targetPos, 2R)?
            float contactRadius = 2f * ballRadius;
            Vector3 toTarget = targetPos - cuePos;
            float b = Vector3.Dot(aimDir, toTarget);
            float c = toTarget.sqrMagnitude - contactRadius * contactRadius;
            float discriminant = b * b - c;

            if (discriminant < 0f || b < 0f)
            {
                // Miss — target ball does not move. Cue ball travels along aimDir until
                // it runs into a rail (we still show a cue path as informative).
                result.state = TargetBallEndState.NotHit;
                result.targetBallEndPos = targetPos;
                result.targetBallTrajectory = new[] { targetPos, targetPos };

                float tRailCue = FirstRailHit(cuePos, aimDir, table, ballRadius);
                Vector3 cueEnd = cuePos + aimDir * Mathf.Max(0f, tRailCue);
                cueEnd.y = ballRadius;
                result.cueBallTrajectory = new[] { cuePos, cueEnd };
                return result;
            }

            float tImpact = b - Mathf.Sqrt(discriminant);
            Vector3 ghostPos = cuePos + aimDir * tImpact;
            Vector3 targetDir = (targetPos - ghostPos).normalized;

            // Target ball path: clip at first rail or pocket.
            float tRail = FirstRailHit(targetPos, targetDir, table, ballRadius);
            int pocketIdx;
            float tPocket = FirstPocketHit(targetPos, targetDir, table, pocketCatchRadius, out pocketIdx);

            if (tPocket < tRail)
            {
                result.state = TargetBallEndState.InPocket;
                result.pocketHitPos = table.Pockets[pocketIdx].Position;
                result.targetBallEndPos = result.pocketHitPos;
            }
            else
            {
                Vector3 endCenter = targetPos + targetDir * tRail;
                endCenter.y = ballRadius;
                result.state = TargetBallEndState.AgainstRail;
                result.targetBallEndPos = endCenter;
            }
            result.targetBallTrajectory = new[] { targetPos, result.targetBallEndPos };

            // Cue ball path: cue → ghost, then perpendicular deflection.
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

        static float FirstPocketHit(Vector3 start, Vector3 dir, TableController table, float catchRadius, out int pocketIdx)
        {
            pocketIdx = -1;
            float tBest = float.MaxValue;
            for (int i = 0; i < table.Pockets.Count; i++)
            {
                var p = table.Pockets[i];
                Vector3 toPocket = p.Position - start;
                float tClosest = Vector3.Dot(toPocket, dir);
                if (tClosest < 0f) continue;
                Vector3 closest = start + dir * tClosest;
                Vector3 delta = p.Position - closest;
                delta.y = 0f;
                float d2 = delta.sqrMagnitude;
                float r2 = catchRadius * catchRadius;
                if (d2 > r2) continue;
                float h = Mathf.Sqrt(r2 - d2);
                float tEnter = Mathf.Max(0f, tClosest - h);
                if (tEnter < tBest) { tBest = tEnter; pocketIdx = i; }
            }
            return tBest;
        }
    }
}
