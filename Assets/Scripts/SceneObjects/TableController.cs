using System.Collections.Generic;
using UnityEngine;

namespace PoolAimTrainer.SceneObjects
{
    public class TableController : MonoBehaviour
    {
        [Tooltip("桌面可用矩形的一半长度（米），默认美标 8ft 桌内尺寸")]
        public float playfieldHalfLength = 1.12f;
        [Tooltip("桌面可用矩形的一半宽度（米）")]
        public float playfieldHalfWidth = 0.56f;
        [Tooltip("球半径（米）")]
        public float ballRadius = 0.0286f;

        public List<PocketMarker> Pockets { get; } = new List<PocketMarker>();

        void Awake()
        {
            Pockets.AddRange(GetComponentsInChildren<PocketMarker>());
        }

        public Vector3 ClampBallCenter(Vector3 pos)
        {
            float maxX = playfieldHalfLength - ballRadius;
            float maxZ = playfieldHalfWidth - ballRadius;
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.z = Mathf.Clamp(pos.z, -maxZ, maxZ);
            pos.y = ballRadius;
            return pos;
        }
    }
}
