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
    }
}
