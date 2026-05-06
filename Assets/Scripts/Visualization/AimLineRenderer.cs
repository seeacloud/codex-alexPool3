using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    public class AimLineRenderer : MonoBehaviour
    {
        public LineRenderer cueToGhost;
        public LineRenderer objectToPocket;

        public void Show(Vector3 cue, Vector3 ghost, Vector3 obj, Vector3 pocket)
        {
            if (cueToGhost != null)
            {
                cueToGhost.enabled = true;
                cueToGhost.positionCount = 2;
                cueToGhost.SetPosition(0, cue);
                cueToGhost.SetPosition(1, ghost);
            }

            if (objectToPocket != null)
            {
                objectToPocket.enabled = true;
                objectToPocket.positionCount = 2;
                objectToPocket.SetPosition(0, obj);
                objectToPocket.SetPosition(1, pocket);
            }
        }

        public void Hide()
        {
            if (cueToGhost != null) cueToGhost.enabled = false;
            if (objectToPocket != null) objectToPocket.enabled = false;
        }
    }
}
