#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Creates a touch-friendly toolbar at the top-left of the Canvas with buttons
    /// for: Resize HUD, Reset Aim, Toggle Labels, Toggle Lines.
    ///
    /// Menu: PoolAimTrainer -> Setup Touch Toolbar
    /// </summary>
    public static class SetupTouchToolbar
    {
        [MenuItem("PoolAimTrainer/Setup Touch Toolbar")]
        public static void Setup()
        {
            var canvasGo = GameObject.Find("Canvas");
            var gm = GameObject.Find("_GameManager");
            var hudPanel = GameObject.Find("AimHudPanel");
            if (canvasGo == null || gm == null)
            {
                EditorUtility.DisplayDialog("Missing", "Canvas or _GameManager not found.", "OK");
                return;
            }

            var toolbarGo = GameObject.Find("TouchToolbar");
            if (toolbarGo == null)
            {
                toolbarGo = new GameObject("TouchToolbar", typeof(RectTransform));
                toolbarGo.transform.SetParent(canvasGo.transform, false);
            }
            var tbRect = toolbarGo.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 1f);
            tbRect.anchorMax = new Vector2(0f, 1f);
            tbRect.pivot = new Vector2(0f, 1f);
            tbRect.anchoredPosition = new Vector2(20f, -20f);
            tbRect.sizeDelta = new Vector2(320f, 50f);

            var hlg = toolbarGo.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = toolbarGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var btnH = CreateToolbarButton(toolbarGo.transform, "BtnH", "H", 60f);
            var btnG = CreateToolbarButton(toolbarGo.transform, "BtnG", "G", 60f);
            var btnL = CreateToolbarButton(toolbarGo.transform, "BtnL", "L", 60f);
            var btnV = CreateToolbarButton(toolbarGo.transform, "BtnV", "V", 60f);

            var toolbar = toolbarGo.GetComponent<TouchToolbar>();
            if (toolbar == null) toolbar = toolbarGo.AddComponent<TouchToolbar>();
            toolbar.btnResizeHud = btnH.GetComponent<Button>();
            toolbar.btnResetAim = btnG.GetComponent<Button>();
            toolbar.btnToggleLabels = btnL.GetComponent<Button>();
            toolbar.btnToggleLines = btnV.GetComponent<Button>();
            toolbar.aimManager = gm.GetComponent<AimManager>();
            if (hudPanel != null)
                toolbar.hudController = hudPanel.GetComponent<AimHudController>();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Touch toolbar created (top-left): H=resize HUD, G=reset aim, L=labels, V=lines. Works with mouse click AND touch.",
                "OK");
        }

        static GameObject CreateToolbarButton(Transform parent, string name, string label, float width)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 44f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            go.AddComponent<Button>().targetGraphic = img;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var tmp = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            var txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            return go;
        }
    }
}
#endif
