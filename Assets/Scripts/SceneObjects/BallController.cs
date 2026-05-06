using UnityEngine;

namespace PoolAimTrainer.SceneObjects
{
    public enum BallKind { CueBall, TargetBall }

    public class BallController : MonoBehaviour
    {
        public BallKind kind = BallKind.TargetBall;
        public TableController table;

        public Vector3 Center => transform.position;

        public void MoveTo(Vector3 worldPos)
        {
            transform.position = table != null ? table.ClampBallCenter(worldPos) : worldPos;
        }
    }
}
