using UnityEngine;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Puzzles
{
    public class PuzzleGenerator : MonoBehaviour
    {
        public TableController table;
        public BallController cueBall;
        public BallController targetBall;
        public AimManager aimManager;

        const float BALL_RADIUS = 0.0286f;
        const int MAX_DIRECTION_ATTEMPTS = 36;
        const int MAX_FALLBACK_ATTEMPTS = 5;

        public bool Generate(PuzzleParams p)
        {
            if (table == null || cueBall == null || targetBall == null) return false;

            int pocketIdx = ResolvePocketIndex(p.pocketIndex);
            if (pocketIdx < 0 || pocketIdx >= table.Pockets.Count) return false;

            float cutAngle = p.SampleCutAngle();
            DistanceRange tpRange = PuzzleParams.GetTargetPocketRange(p.targetToPocket);
            DistanceRange ctRange = PuzzleParams.GetCueTargetRange(p.cueToTarget);

            // Try with sampled distances
            for (int fallback = 0; fallback < MAX_FALLBACK_ATTEMPTS; fallback++)
            {
                float dTP = tpRange.Sample();
                float dCT = ctRange.Sample();

                if (TryPlace(pocketIdx, cutAngle, dTP, dCT))
                    return true;

                // Fallback: shrink distances
                tpRange = new DistanceRange(tpRange.min, Mathf.Lerp(tpRange.min, tpRange.max, 0.5f));
                ctRange = new DistanceRange(ctRange.min, Mathf.Lerp(ctRange.min, ctRange.max, 0.5f));
            }

            // Last resort: try other pockets
            for (int i = 0; i < table.Pockets.Count; i++)
            {
                if (i == pocketIdx) continue;
                float dTP = PuzzleParams.GetTargetPocketRange(p.targetToPocket).min;
                float dCT = PuzzleParams.GetCueTargetRange(p.cueToTarget).min;
                if (TryPlace(i, cutAngle, dTP, dCT))
                    return true;
            }

            return false;
        }

        bool TryPlace(int pocketIdx, float cutAngleDeg, float dTargetPocket, float dCueTarget)
        {
            Vector3 pocketPos = table.Pockets[pocketIdx].Position;

            int[] directions = ShuffledDirections();

            for (int d = 0; d < directions.Length; d++)
            {
                float theta = directions[d] * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));

                Vector3 targetPos = pocketPos + dir * dTargetPocket;
                targetPos.y = BALL_RADIUS;

                if (!IsInsidePlayfield(targetPos)) continue;

                Vector3 pocketDir = (pocketPos - targetPos).normalized;
                Vector3 ghostPos = targetPos - pocketDir * (2f * BALL_RADIUS);

                // Try both left and right cut
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float angleRad = cutAngleDeg * Mathf.Deg2Rad * sign;
                    Vector3 cueDir = RotateY(pocketDir, angleRad);
                    Vector3 cuePos = ghostPos - cueDir * dCueTarget;
                    cuePos.y = BALL_RADIUS;

                    if (!IsInsidePlayfield(cuePos)) continue;

                    // Ensure cue ball doesn't overlap target ball
                    if (Vector3.Distance(cuePos, targetPos) < 2f * BALL_RADIUS + 0.01f) continue;

                    // Success — place balls
                    cueBall.MoveTo(cuePos);
                    targetBall.MoveTo(targetPos);

                    if (aimManager != null)
                    {
                        aimManager.SetUserPocket(table.Pockets[pocketIdx]);
                        aimManager.ClearManualAim();
                        aimManager.ForceRefresh();
                    }
                    return true;
                }
            }
            return false;
        }

        int ResolvePocketIndex(int requested)
        {
            if (requested >= 1 && requested <= table.Pockets.Count)
                return requested - 1;
            return Random.Range(0, table.Pockets.Count);
        }

        bool IsInsidePlayfield(Vector3 pos)
        {
            float maxX = table.playfieldHalfLength - BALL_RADIUS - 0.01f;
            float maxZ = table.playfieldHalfWidth - BALL_RADIUS - 0.01f;
            return Mathf.Abs(pos.x) <= maxX && Mathf.Abs(pos.z) <= maxZ;
        }

        static Vector3 RotateY(Vector3 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector3(v.x * cos - v.z * sin, v.y, v.x * sin + v.z * cos);
        }

        static int[] ShuffledDirections()
        {
            int[] dirs = new int[MAX_DIRECTION_ATTEMPTS];
            for (int i = 0; i < MAX_DIRECTION_ATTEMPTS; i++)
                dirs[i] = i * (360 / MAX_DIRECTION_ATTEMPTS);
            // Fisher-Yates shuffle
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = dirs[i]; dirs[i] = dirs[j]; dirs[j] = tmp;
            }
            return dirs;
        }
    }
}
