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

            var camGo = GameObject.Find("AimCamera") ?? new GameObject("AimCamera");
            var cam = camGo.GetComponent<Camera>();
            if (cam == null) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 0.08f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.3f, 0.15f, 1f);
            cam.cullingMask = ~0;
            cam.depth = -5;

            var rt = new RenderTexture(RtSize, RtSize, 16)
            {
                name = "AimHudRT",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            rt.Create();
            cam.targetTexture = rt;

            var panelGo = GameObject.Find("AimHudPanel");
            if (panelGo == null)
            {
                panelGo = new GameObject("AimHudPanel", typeof(RectTransform));
                panelGo.transform.SetParent(canvasGo.transform, false);
            }
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(320f, 340f);
            panelRect.anchoredPosition = new Vector2(-20f, -80f);

            var bg = panelGo.GetComponent<Image>();
            if (bg == null) bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.4f);

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
            imgRect.offsetMin = new Vector2(8f, 40f);
            imgRect.offsetMax = new Vector2(-8f, -40f);
            var rawImage = imgGo.GetComponent<RawImage>();
            if (rawImage == null) rawImage = imgGo.AddComponent<RawImage>();
            rawImage.texture = rt;
            rawImage.raycastTarget = true;

            // Delete old nested crosshair container if present.
            var oldCross = imgGo.transform.Find("Crosshair");
            if (oldCross != null) Object.DestroyImmediate(oldCross.gameObject);

            // Horizontal crosshair bar: stretches across RawImage width, fixed 2px tall,
            // Y position set at runtime by AimHudController.
            var hBar = EnsureChild(imgGo.transform, "CrosshairH");
            var hRect = hBar.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0f);
            hRect.anchorMax = new Vector2(1f, 0f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.offsetMin = new Vector2(0f, 0f);
            hRect.offsetMax = new Vector2(0f, 0f);
            hRect.sizeDelta = new Vector2(0f, 2f);
            var hImg = hBar.GetComponent<Image>();
            if (hImg == null) hImg = hBar.AddComponent<Image>();
            hImg.color = new Color(1f, 0.3f, 0.3f, 0.95f);
            hImg.raycastTarget = false;

            // Vertical crosshair bar: fixed 2px wide, height computed at runtime from
            // (ballDiameter + extra) / (2 * orthoSize) * HUD height.
            var vBar = EnsureChild(imgGo.transform, "CrosshairV");
            var vRect = vBar.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0f, 0f);
            vRect.anchorMax = new Vector2(0f, 0f);
            vRect.pivot = new Vector2(0.5f, 0.5f);
            vRect.sizeDelta = new Vector2(2f, 100f);
            var vImg = vBar.GetComponent<Image>();
            if (vImg == null) vImg = vBar.AddComponent<Image>();
            vImg.color = new Color(1f, 0.3f, 0.3f, 0.95f);
            vImg.raycastTarget = false;

            var ctrl = panelGo.GetComponent<AimHudController>();
            if (ctrl == null) ctrl = panelGo.AddComponent<AimHudController>();
            ctrl.aimCamera = cam;
            ctrl.rawImage = rawImage;
            ctrl.rawImageRect = imgRect;
            ctrl.cueBall = cueGo.GetComponent<BallController>();
            ctrl.targetBall = targetGo.GetComponent<BallController>();
            ctrl.aimManager = gm.GetComponent<AimManager>();
            ctrl.orthoSize = 0.08f;
            ctrl.crosshairHBar = hRect;
            ctrl.crosshairVBar = vRect;
            ctrl.ballDiameter = 0.0572f;
            ctrl.crosshairVerticalExtraMeters = 0.010f;

            // Title label
            var titleGo = EnsureChild(panelGo.transform, "Title");
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            titleRect.sizeDelta = new Vector2(0f, 30f);
            var titleTmp = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
            if (titleTmp == null) titleTmp = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
            titleTmp.text = "HUD  click=aim  G=reset";
            titleTmp.fontSize = 20;
            titleTmp.color = Color.white;
            titleTmp.alignment = TMPro.TextAlignmentOptions.Center;

            // Offset label (below RawImage)
            var labelGo = EnsureChild(panelGo.transform, "OffsetLabel");
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 6f);
            labelRect.sizeDelta = new Vector2(0f, 28f);
            var labelTmp = labelGo.GetComponent<TMPro.TextMeshProUGUI>();
            if (labelTmp == null) labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            labelTmp.text = "dx = 0.0 mm";
            labelTmp.fontSize = 22;
            labelTmp.color = new Color(1f, 1f, 0.7f, 1f);
            labelTmp.alignment = TMPro.TextAlignmentOptions.Center;
            ctrl.offsetLabel = labelTmp;

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Aim HUD updated: horizontal crosshair bar spans HUD width, vertical bar = ball diameter + 10mm. Offset readout in mm below. Press Play, click to aim.",
                "OK");
        }

        static GameObject EnsureChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
#endif
