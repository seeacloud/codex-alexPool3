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
    }
}
