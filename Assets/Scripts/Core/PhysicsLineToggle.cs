using UnityEngine;
using PoolAimTrainer.Visualization;

namespace PoolAimTrainer.Core
{
    /// <summary>
    /// Press V to toggle visibility of the target-ball and cue-ball trajectory lines
    /// (the "physics" overlay). Attach to _GameManager.
    /// </summary>
    public class PhysicsLineToggle : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.V;
        public bool startVisible = true;

        public TargetBallPathRenderer targetPath;
        public CueBallPathRenderer cuePath;
        public ReferenceLineVisibility referenceLineVisibility;

        bool visible;

        void Start()
        {
            if (referenceLineVisibility == null)
                referenceLineVisibility = ReferenceLineVisibility.Active
                    ?? FindObjectOfType<ReferenceLineVisibility>();

            visible = startVisible;
            ApplyVisibility();
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
                ApplyVisibility();
            }
        }

        void ApplyVisibility()
        {
            if (referenceLineVisibility != null)
            {
                referenceLineVisibility.SetLayerVisible(ReferenceVisualLayer.TargetBallPath, visible);
                referenceLineVisibility.SetLayerVisible(ReferenceVisualLayer.ManualCuePath, visible);
                return;
            }

            if (targetPath != null) targetPath.gameObject.SetActive(visible);
            if (cuePath != null) cuePath.gameObject.SetActive(visible);
            if (!visible)
            {
                if (targetPath != null) targetPath.Hide();
                if (cuePath != null) cuePath.Hide();
            }
        }
    }
}
