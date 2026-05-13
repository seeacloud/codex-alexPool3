using UnityEngine;

namespace PoolAimTrainer.Trajectory
{
    public enum TargetBallEndState
    {
        NotHit,
        OnTable,
        AgainstRail,
        InPocket,
        StillMoving
    }

    public struct SimulationResult
    {
        public TargetBallEndState state;
        public Vector3 targetBallEndPos;
        public Vector3 pocketHitPos;
        public Vector3[] targetBallTrajectory;
        public Vector3[] cueBallTrajectory;
        public bool hasBallCollision;
        public Vector3 collisionCueBallCenter;
        public Vector3 collisionTargetBallCenter;
        public Vector3 collisionContactPoint;
    }
}
