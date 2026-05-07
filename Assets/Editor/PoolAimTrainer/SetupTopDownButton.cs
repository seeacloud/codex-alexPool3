#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Interaction;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.EditorTools
{
    public static class SetupTopDownButton
    {
        [MenuItem("PoolAimTrainer/Setup Top-Down Button")]
        public static void Setup()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Missing", "No Canvas found.", "OK");
                return;
            }

            var cam = Camera.main;
            CameraOrbit orbit = null;
            if (cam != null) orbit = cam.GetComponent<CameraOrbit>();

            var existing = GameObject.Find("BtnTopDown");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("BtnTopDown");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -20f);
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
            tmp.text = "Top";
            tmp.fontSize = 24f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var topDown = go.AddComponent<TopDownButton>();
            topDown.cameraOrbit = orbit;

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Top-Down button added (top-left). Click it or press T to toggle. Right-drag rotates in top-down mode.",
                "OK");
        }
    }
}
#endif
