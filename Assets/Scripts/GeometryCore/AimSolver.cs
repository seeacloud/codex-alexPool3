using UnityEngine;

namespace PoolAimTrainer.GeometryCore
{
    public static class AimSolver
    {
        public static Vector3 ComputeGhostBallCenter(
            Vector3 objectBallCenter, Vector3 pocketCenter, float ballRadius)
        {
            Vector3 dir = (pocketCenter - objectBallCenter).normalized;
            return objectBallCenter - dir * (2f * ballRadius);
        }

        public static float ComputeCutAngle(
            Vector3 cueBallCenter, Vector3 objectBallCenter, Vector3 pocketCenter, float ballRadius)
        {
            Vector3 ghost = ComputeGhostBallCenter(objectBallCenter, pocketCenter, ballRadius);
            Vector3 cueToGhost = (ghost - cueBallCenter).normalized;
            Vector3 objToPocket = (pocketCenter - objectBallCenter).normalized;
            float dot = Vector3.Dot(cueToGhost, objToPocket);
            dot = Mathf.Clamp(dot, -1f, 1f);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }

        public static AimResult Compute(
            Vector3 cueBallCenter, Vector3 objectBallCenter, Vector3 pocketCenter, float ballRadius)
        {
            Vector3 ghost = ComputeGhostBallCenter(objectBallCenter, pocketCenter, ballRadius);
            Vector3 cueToGhost = (ghost - cueBallCenter).normalized;
            Vector3 objToPocket = (pocketCenter - objectBallCenter).normalized;
            float dot = Vector3.Dot(cueToGhost, objToPocket);
            if (dot < 0f)
            {
                return AimResult.Unsolvable("主球位于目标球与袋口之间，无法直接入袋");
            }
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            return new AimResult
            {
                solvable = true,
                ghostBallCenter = ghost,
                aimLineStart = cueBallCenter,
                aimLineEnd = ghost,
                objectBallToPocketStart = objectBallCenter,
                objectBallToPocketEnd = pocketCenter,
                cutAngleDegrees = angle,
                hintText = ""
            };
        }
    }
}
