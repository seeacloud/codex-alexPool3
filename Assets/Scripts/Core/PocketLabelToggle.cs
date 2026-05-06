using UnityEngine;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Core
{
    /// <summary>
    /// Listens for a keyboard shortcut to toggle pocket number labels on/off.
    /// Attach to _GameManager.
    /// </summary>
    public class PocketLabelToggle : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.L;
        public bool startVisible = true;

        void Start()
        {
            PocketMarker.SetLabelsVisible(startVisible);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                PocketMarker.SetLabelsVisible(!PocketMarker.LabelsVisible);
            }
        }
    }
}
