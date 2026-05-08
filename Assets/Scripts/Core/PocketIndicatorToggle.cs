using UnityEngine;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Core
{
    /// <summary>
    /// Press P to toggle visibility of all pocket indicator spheres.
    /// Default: hidden (indicators off). Clicking near a pocket still works
    /// via DragInput.FindClosestPocket regardless of visibility.
    /// </summary>
    public class PocketIndicatorToggle : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.P;
        public bool startVisible = false;

        void Start()
        {
            PocketMarker.SetIndicatorsVisible(startVisible);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                PocketMarker.SetIndicatorsVisible(!PocketMarker.IndicatorsVisible);
        }
    }
}
