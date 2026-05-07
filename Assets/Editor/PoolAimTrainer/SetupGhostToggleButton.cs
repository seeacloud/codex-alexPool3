#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.EditorTools
{
    public static class SetupGhostToggleButton
    {
        [MenuItem("PoolAimTrainer/Setup Ghost Toggle Button")]
        public static void Setup()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Missing", "No Canvas found.", "OK");
                return;
            }

            var existing = GameObject.Find("BtnGhost");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("BtnGhost");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(260f, -20f);
            rt.sizeDelta = new Vector2(100f, 50f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Ghost";
            tmp.fontSize = 20f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var ghostToggle = go.AddComponent<GhostToggleButton>();
            var gm = GameObject.Find("_GameManager");
            if (gm != null)
                ghostToggle.aimManager = gm.GetComponent<AimManager>();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Ghost toggle button added. Click it or press G to show/hide ghost ball + aim lines.",
                "OK");
        }
    }
}
#endif
