#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Sets up the Aim HUD: a secondary orthographic camera that views the target ball
    /// from the cue ball's position, rendered to a RenderTexture, displayed as a
    /// RawImage on the Canvas. Clicking the RawImage sets AimManager's manual aim direction.
    ///
    /// Menu: PoolAimTrainer -> Setup Aim HUD
    /// </summary>
    public static class SetupAimHud
    {
        const int RtSize = 256;

        [MenuItem("PoolAimTrainer/Setup Aim HUD")]
        public static void Setup()
        {
            var cueGo = GameObject.Find("CueBall");
            var targetGo = GameObject.Find("TargetBall");
            var gm = GameObject.Find("_GameManager");
            var canvasGo = GameObject.Find("Canvas");
            if (cueGo == null || targetGo == null || gm == null || canvasGo == null)
            {
                EditorUtility.DisplayDialog("Missing",
                    "Need CueBall / TargetBall / _GameManager / Canvas in scene. Run Sessions A/B/C first.",
                    "OK");
                return;
            }

            // 1. AimCamera GameObject + Camera
            var camGo = GameObject.Find("AimCamera");
            if (camGo == null)
            {
                camGo = new GameObject("AimCamera");
            }
            var cam = camGo.GetComponent<Camera>();
            if (cam == null) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 0.08f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.3f, 0.15f, 1f);
            cam.cullingMask = ~0;
            cam.depth = -5;

            // 2. RenderTexture
            var rt = new RenderTexture(RtSize, RtSize, 16)
            {
                name = "AimHudRT",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            rt.Create();
            cam.targetTexture = rt;

            // 3. HUD Panel on Canvas (RawImage)
            var existing = GameObject.Find("AimHudPanel");
            GameObject panelGo;
            if (existing != null) panelGo = existing;
            else
            {
                panelGo = new GameObject("AimHudPanel", typeof(RectTransform));
                panelGo.transform.SetParent(canvasGo.transform, false);
            }
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(320f, 320f);
            panelRect.anchoredPosition = new Vector2(-20f, -80f);

            // Background
            var bg = panelGo.GetComponent<Image>();
            if (bg == null) bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);

            // RawImage child
            var imageChild = panelGo.transform.Find("RawImage");
            GameObject imgGo;
            if (imageChild != null) imgGo = imageChild.gameObject;
            else
            {
                imgGo = new GameObject("RawImage", typeof(RectTransform));
                imgGo.transform.SetParent(panelGo.transform, false);
            }
            var imgRect = imgGo.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0f, 0f);
            imgRect.anchorMax = new Vector2(1f, 1f);
            imgRect.offsetMin = new Vector2(8f, 8f);
            imgRect.offsetMax = new Vector2(-8f, -8f);
            var rawImage = imgGo.GetComponent<RawImage>();
            if (rawImage == null) rawImage = imgGo.AddComponent<RawImage>();
            rawImage.texture = rt;
            rawImage.raycastTarget = true;

            // Controller component
            var ctrl = panelGo.GetComponent<AimHudController>();
            if (ctrl == null) ctrl = panelGo.AddComponent<AimHudController>();
            ctrl.aimCamera = cam;
            ctrl.rawImage = rawImage;
            ctrl.rawImageRect = imgRect;
            ctrl.cueBall = cueGo.GetComponent<BallController>();
            ctrl.targetBall = targetGo.GetComponent<BallController>();
            ctrl.aimManager = gm.GetComponent<AimManager>();
            ctrl.orthoSize = 0.08f;

            // Crosshair marker (parented to RawImage so its anchored coords are in image space)
            var crossChild = imgGo.transform.Find("Crosshair");
            GameObject crossGo;
            if (crossChild != null) crossGo = crossChild.gameObject;
            else
            {
                crossGo = new GameObject("Crosshair", typeof(RectTransform));
                crossGo.transform.SetParent(imgGo.transform, false);
                var hBar = new GameObject("H", typeof(RectTransform)).GetComponent<RectTransform>();
                hBar.SetParent(crossGo.transform, false);
                var hImg = hBar.gameObject.AddComponent<Image>();
                hImg.color = new Color(1f, 0.3f, 0.3f, 0.95f);
                hBar.sizeDelta = new Vector2(30f, 2f);
                var vBar = new GameObject("V", typeof(RectTransform)).GetComponent<RectTransform>();
                vBar.SetParent(crossGo.transform, false);
                var vImg = vBar.gameObject.AddComponent<Image>();
                vImg.color = new Color(1f, 0.3f, 0.3f, 0.95f);
                vBar.sizeDelta = new Vector2(2f, 30f);
            }
            var crossRect = crossGo.GetComponent<RectTransform>();
            crossRect.anchorMin = new Vector2(0f, 0f);
            crossRect.anchorMax = new Vector2(0f, 0f);
            crossRect.pivot = new Vector2(0.5f, 0.5f);
            crossRect.sizeDelta = Vector2.zero;
            ctrl.crosshair = crossRect;

            // Offset label (below the HUD image)
            var labelChild = panelGo.transform.Find("OffsetLabel");
            GameObject labelGo;
            if (labelChild != null) labelGo = labelChild.gameObject;
            else
            {
                labelGo = new GameObject("OffsetLabel", typeof(RectTransform));
                labelGo.transform.SetParent(panelGo.transform, false);
                var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "Δ = 0.0 mm";
                tmp.fontSize = 20;
                tmp.color = new Color(1f, 1f, 0.7f, 1f);
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
            }
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);
            labelRect.sizeDelta = new Vector2(0f, 22f);
            ctrl.offsetLabel = labelGo.GetComponent<TMPro.TextMeshProUGUI>();

            // Title label
            var titleChild = panelGo.transform.Find("Title");
            GameObject titleGo;
            if (titleChild != null) titleGo = titleChild.gameObject;
            else
            {
                titleGo = new GameObject("Title", typeof(RectTransform));
                titleGo.transform.SetParent(panelGo.transform, false);
                var tmp = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "HUD · 点击定瞄点 · G 键重置";
                tmp.fontSize = 22;
                tmp.color = Color.white;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
            }
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            titleRect.sizeDelta = new Vector2(0f, 28f);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Aim HUD attached. Play, then click inside the HUD panel to set strike point. Press G to return to auto-aim.",
                "OK");
        }
    }
}
#endif
