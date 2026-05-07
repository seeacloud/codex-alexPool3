#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.Interaction;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.EditorTools
{
    public static class SetupShotButton
    {
        [MenuItem("PoolAimTrainer/Setup Shot Button")]
        public static void Setup()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Missing", "No Canvas found.", "OK");
                return;
            }

            var existing = GameObject.Find("BtnShot");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("BtnShot");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(140f, -20f);
            rt.sizeDelta = new Vector2(100f, 50f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f, 0.85f);

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
            tmp.text = "Hit";
            tmp.fontSize = 24f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var shotBtn = go.AddComponent<ShotButton>();

            var gm = GameObject.Find("_GameManager");
            if (gm != null)
            {
                shotBtn.aimManager = gm.GetComponent<AimManager>();
                shotBtn.shotSimulator = gm.GetComponent<ShotSimulator>();
            }
            var cue = GameObject.Find("CueBall");
            var target = GameObject.Find("TargetBall");
            var tableGO = GameObject.Find("Pool-Table");
            if (cue != null) shotBtn.cueBall = cue.GetComponent<BallController>();
            if (target != null) shotBtn.targetBall = target.GetComponent<BallController>();
            if (tableGO != null) shotBtn.table = tableGO.GetComponent<TableController>();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Shot button added (top-left, red). Click it or press Space to shoot.",
                "OK");
        }
    }
}
#endif
