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

        bool visible;

        void Start()
        {
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
