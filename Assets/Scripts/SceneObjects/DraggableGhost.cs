using UnityEngine;

namespace PoolAimTrainer.SceneObjects
{
    /// <summary>
    /// Marker component on a ghost-ball instance indicating it is draggable by
    /// DragInput. Position changes are pushed by DragInput directly; this component
    /// just serves as a pickable target.
    /// </summary>
    public class DraggableGhost : MonoBehaviour
    {
        public Vector3 Center => transform.position;

        public void MoveTo(Vector3 worldPos)
        {
            transform.position = worldPos;
        }
    }
}
