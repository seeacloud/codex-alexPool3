using UnityEngine;
using UnityEngine.UI;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.Core
{
    /// <summary>
    /// Touch-friendly toolbar with buttons for all keyboard shortcuts.
    /// Attach to a Canvas panel. Wire buttons in editor.
    /// </summary>
    public class TouchToolbar : MonoBehaviour
    {
        public Button btnResizeHud;
        public Button btnResetAim;
        public Button btnToggleLabels;
        public Button btnToggleLines;

        public AimHudController hudController;
        public AimManager aimManager;

        void Start()
        {
            if (btnResizeHud != null) btnResizeHud.onClick.AddListener(OnResizeHud);
            if (btnResetAim != null) btnResetAim.onClick.AddListener(OnResetAim);
            if (btnToggleLabels != null) btnToggleLabels.onClick.AddListener(OnToggleLabels);
            if (btnToggleLines != null) btnToggleLines.onClick.AddListener(OnToggleLines);
        }

        void OnResizeHud()
        {
            if (hudController != null)
                hudController.SendMessage("ToggleSize", SendMessageOptions.DontRequireReceiver);
        }

        void OnResetAim()
        {
            if (aimManager != null) aimManager.ClearManualAim();
        }

        void OnToggleLabels()
        {
            PocketMarker.SetLabelsVisible(!PocketMarker.LabelsVisible);
        }

        void OnToggleLines()
        {
            var toggle = FindObjectOfType<PhysicsLineToggle>();
            if (toggle != null) toggle.SendMessage("ApplyVisibility", SendMessageOptions.DontRequireReceiver);
        }
    }
}
