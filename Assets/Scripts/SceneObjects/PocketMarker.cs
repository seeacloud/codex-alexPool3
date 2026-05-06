using UnityEngine;

namespace PoolAimTrainer.SceneObjects
{
    public class PocketMarker : MonoBehaviour
    {
        [Tooltip("袋口半径（米）")]
        public float pocketRadius = 0.06f;

        public Vector3 Position => transform.position;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pocketRadius);
        }
    }
}
