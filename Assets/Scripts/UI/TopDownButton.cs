using UnityEngine;
using UnityEngine.UI;
using PoolAimTrainer.Interaction;

namespace PoolAimTrainer.UI
{
    public class TopDownButton : MonoBehaviour
    {
        public CameraOrbit cameraOrbit;
        public KeyCode shortcutKey = KeyCode.T;

        Button btn;

        void Start()
        {
            btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnClick);
        }

        void Update()
        {
            if (Input.GetKeyDown(shortcutKey)) OnClick();
        }

        void OnClick()
        {
            if (cameraOrbit != null) cameraOrbit.ToggleTopDown();
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null)
                txt.text = cameraOrbit != null && cameraOrbit.isTopDown ? "3D" : "Top";
        }
    }
}
