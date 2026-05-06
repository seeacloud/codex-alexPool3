using UnityEngine;

namespace PoolAimTrainer.SceneObjects
{
    public class SelectableBall : MonoBehaviour
    {
        public bool isCurrentTarget;
        public void Select() { isCurrentTarget = true; }
        public void Deselect() { isCurrentTarget = false; }
    }
}
