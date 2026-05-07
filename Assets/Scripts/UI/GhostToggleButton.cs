using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.UI
{
    public class GhostToggleButton : MonoBehaviour
    {
        public AimManager aimManager;
        public KeyCode shortcutKey = KeyCode.G;

        UnityEngine.UI.Button btn;

        void Start()
        {
            btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.AddListener(Toggle);
        }

        void Update()
        {
            if (Input.GetKeyDown(shortcutKey)) Toggle();
        }

        void Toggle()
        {
            if (aimManager == null) return;
            aimManager.showGhostAndAimLines = !aimManager.showGhostAndAimLines;
            aimManager.ForceRefresh();
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null)
                txt.text = aimManager.showGhostAndAimLines ? "Ghost ON" : "Ghost OFF";
        }
    }
}
