using UnityEngine;

namespace PoolAimTrainer.GeometryCore
{
    public struct AimResult
    {
        public bool solvable;
        public Vector3 ghostBallCenter;
        public Vector3 aimLineStart;
        public Vector3 aimLineEnd;
        public Vector3 objectBallToPocketStart;
        public Vector3 objectBallToPocketEnd;
        public float cutAngleDegrees;
        public string hintText;

        public static AimResult Unsolvable(string reason)
        {
            return new AimResult
            {
                solvable = false,
                hintText = reason
            };
        }
    }
}
