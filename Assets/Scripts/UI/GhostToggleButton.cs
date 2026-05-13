using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.UI
{
    public class GhostToggleButton : MonoBehaviour
    {
        public AimManager aimManager;
        public KeyCode shortcutKey = KeyCode.G;

        UnityEngine.UI.Button btn;
        UnityEngine.UI.Toggle toggle;
        bool refreshing;

        void Start()
        {
            btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.AddListener(Toggle);
            toggle = GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null) toggle.onValueChanged.AddListener(SetVisible);
            UpdateLabel();
        }

        void Update()
        {
            if (Input.GetKeyDown(shortcutKey)) Toggle();
        }

        void Toggle()
        {
            if (aimManager == null) return;
            SetVisible(!aimManager.showGhostBall);
        }

        void SetVisible(bool visible)
        {
            if (refreshing || aimManager == null) return;
            aimManager.showGhostBall = visible;
            aimManager.ForceRefresh();
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (aimManager == null) return;

            refreshing = true;
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(aimManager.showGhostBall);
            refreshing = false;

            var txt = GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null)
                txt.text = aimManager.showGhostBall ? "Ghost ON" : "Ghost OFF";
        }
    }
}
