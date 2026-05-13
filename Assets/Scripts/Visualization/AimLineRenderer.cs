using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.Visualization
{
    public class AimLineRenderer : DashLine
    {
        [Tooltip("主球→Ghost 线颜色")]
        public Color cueToGhostColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("目标球→袋口线颜色")]
        public Color objectToPocketColor = new Color(0.4f, 1f, 0.4f, 1f);

        LineRenderer lrCueToGhost;
        LineRenderer lrObjToPocket;

        void Awake()
        {
            lrCueToGhost = CreateDashLineRenderer("LR_CueToGhost", cueToGhostColor);
            lrObjToPocket = CreateDashLineRenderer("LR_ObjToPocket", objectToPocketColor);
        }

        public void Show(Vector3 cue, Vector3 ghost, Vector3 obj, Vector3 pocket)
        {
            if (ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.CueToGhost))
                SetLine(lrCueToGhost, cue, ghost);
            else
                HideLine(lrCueToGhost);

            if (ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.ObjectToPocket))
                SetLine(lrObjToPocket, obj, pocket);
            else
                HideLine(lrObjToPocket);
        }

        public void Hide()
        {
            HideLine(lrCueToGhost);
            HideLine(lrObjToPocket);
        }
    }
}
