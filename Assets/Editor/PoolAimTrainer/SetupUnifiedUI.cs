#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.Puzzles;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;
using PoolAimTrainer.Interaction;
using PoolAimTrainer.Trajectory;

namespace PoolAimTrainer.EditorTools
{
    public static class SetupUnifiedUI
    {
        const int HUD_RT_SIZE = 256;
        const float PANEL_WIDTH = 220f;

        [MenuItem("PoolAimTrainer/Setup Unified UI")]
        public static void Setup()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "No Canvas found.");
                return;
            }

            CleanOldUI();
            CreateToolbar(canvas.transform);
            CreateRightPanel(canvas.transform);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.Debug.Log("[Editor] " + "Done" + ": " + "Unified UI created: left toolbar + auto-sized right panel.");
        }

        static void CleanOldUI()
        {
            string[] names =
            {
                "BtnTopDown", "BtnShot", "BtnGhost",
                "AimHudPanel", "PuzzlePanel", "LeftToolbar", "RightPanel",
                "BtnH", "BtnG", "BtnL", "BtnV"
            };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        // ============ Left Toolbar (horizontal layout) ============
        static void CreateToolbar(Transform parent)
        {
            var toolbar = new GameObject("LeftToolbar", typeof(RectTransform));
            toolbar.transform.SetParent(parent, false);
            var rt = toolbar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -10f);

            var hlg = toolbar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var fitter = toolbar.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var btnTop = CreateToolbarButton(toolbar.transform, "BtnTopDown", "Top", new Color(0.3f, 0.3f, 0.3f, 0.9f));
            var btnHit = CreateToolbarButton(toolbar.transform, "BtnShot", "Hit", new Color(0.85f, 0.25f, 0.25f, 0.95f));
            var btnGhost = CreateToolbarButton(toolbar.transform, "BtnGhost", "Ghost ON", new Color(0.3f, 0.3f, 0.3f, 0.9f));

            BindToolbarButtons(btnTop, btnHit, btnGhost);
        }

        static GameObject CreateToolbarButton(Transform parent, string name, string label, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80f;
            le.preferredHeight = 36f;

            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        static void BindToolbarButtons(GameObject topGo, GameObject hitGo, GameObject ghostGo)
        {
            var mainCam = Camera.main;
            var orbit = mainCam != null ? mainCam.GetComponent<CameraOrbit>() : null;
            var gm = GameObject.Find("_GameManager");
            var aim = gm != null ? gm.GetComponent<AimManager>() : null;
            var sim = gm != null ? gm.GetComponent<ShotSimulator>() : null;
            var table = Object.FindObjectOfType<TableController>();
            var cue = GameObject.Find("CueBall");
            var target = GameObject.Find("TargetBall");

            var topBtn = topGo.GetComponent<TopDownButton>() ?? topGo.AddComponent<TopDownButton>();
            topBtn.cameraOrbit = orbit;

            var hitBtn = hitGo.GetComponent<ShotButton>() ?? hitGo.AddComponent<ShotButton>();
            hitBtn.aimManager = aim;
            hitBtn.shotSimulator = sim;
            hitBtn.table = table;
            if (cue != null) hitBtn.cueBall = cue.GetComponent<BallController>();
            if (target != null) hitBtn.targetBall = target.GetComponent<BallController>();

            var ghostBtn = ghostGo.GetComponent<GhostToggleButton>() ?? ghostGo.AddComponent<GhostToggleButton>();
            ghostBtn.aimManager = aim;
        }

        // ============ Right Panel (auto vertical layout with content fitter) ============
        static void CreateRightPanel(Transform parent)
        {
            var panel = new GameObject("RightPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-10f, -10f);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Panel width is fixed via LayoutElement
            var panelLE = panel.AddComponent<LayoutElement>();
            panelLE.preferredWidth = PANEL_WIDTH;

            rt.sizeDelta = new Vector2(PANEL_WIDTH, 0f); // height will grow to fit content

            BuildHUDSection(panel.transform);
            BuildPuzzleSection(panel.transform);
        }

        // ============ HUD Section ============
        static void BuildHUDSection(Transform parent)
        {
            var gm = GameObject.Find("_GameManager");
            var cueGo = GameObject.Find("CueBall");
            var targetGo = GameObject.Find("TargetBall");
            if (gm == null || cueGo == null || targetGo == null) return;

            // Secondary camera & render texture
            var camGo = GameObject.Find("AimCamera") ?? new GameObject("AimCamera");
            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 0.08f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.3f, 0.15f, 1f);
            cam.cullingMask = ~0;
            cam.depth = -5;

            var rt = new RenderTexture(HUD_RT_SIZE, HUD_RT_SIZE, 16)
            {
                name = "AimHudRT",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            rt.Create();
            cam.targetTexture = rt;

            // Section container (vertical)
            var section = CreateVerticalSection(parent, "HudSection", 4f);

            // Title
            CreateLabel(section.transform, "Title", "瞄准 HUD", 14f, TextAlignmentOptions.Center, 20f);

            // Square aspect ratio image
            var imgGo = new GameObject("RawImage", typeof(RectTransform));
            imgGo.transform.SetParent(section.transform, false);
            var imgLE = imgGo.AddComponent<LayoutElement>();
            imgLE.preferredHeight = PANEL_WIDTH - 32f; // square-ish based on panel width
            var aspect = imgGo.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspect.aspectRatio = 1f;
            var rawImage = imgGo.AddComponent<RawImage>();
            rawImage.texture = rt;
            rawImage.raycastTarget = true;

            var hBar = MakeCrosshairBar(imgGo.transform, "CrosshairH", true);
            var vBar = MakeCrosshairBar(imgGo.transform, "CrosshairV", false);

            // Offset label
            var offsetLabel = CreateLabel(section.transform, "OffsetLabel", "dx = 0.0 mm", 13f, TextAlignmentOptions.Center, 18f);

            // Nudge buttons row
            var nudgeRow = CreateHorizontalRow(section.transform, "NudgeRow", 6f, 28f);
            var leftBtn = CreateRowButton(nudgeRow.transform, "BtnNudgeLeft", "<", new Color(0.25f, 0.25f, 0.25f, 0.85f), 18f);
            var rightBtn = CreateRowButton(nudgeRow.transform, "BtnNudgeRight", ">", new Color(0.25f, 0.25f, 0.25f, 0.85f), 18f);

            // Controller
            var ctrl = section.AddComponent<AimHudController>();
            ctrl.aimCamera = cam;
            ctrl.rawImage = rawImage;
            ctrl.rawImageRect = imgGo.GetComponent<RectTransform>();
            ctrl.cueBall = cueGo.GetComponent<BallController>();
            ctrl.targetBall = targetGo.GetComponent<BallController>();
            ctrl.aimManager = gm.GetComponent<AimManager>();
            ctrl.orthoSize = 0.08f;
            ctrl.crosshairHBar = hBar.GetComponent<RectTransform>();
            ctrl.crosshairVBar = vBar.GetComponent<RectTransform>();
            ctrl.ballDiameter = 0.0572f;
            ctrl.crosshairVerticalExtraMeters = 0.010f;
            ctrl.offsetLabel = offsetLabel.GetComponent<TextMeshProUGUI>();
            ctrl.nudgeLeftBtn = leftBtn.GetComponent<Button>();
            ctrl.nudgeRightBtn = rightBtn.GetComponent<Button>();
            ctrl.hudContent = section;
        }

        static GameObject MakeCrosshairBar(Transform parent, string name, bool horizontal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (horizontal)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 2f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(2f, 60f);
            }
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.3f, 0.3f, 0.95f);
            img.raycastTarget = false;
            return go;
        }

        // ============ Puzzle Section ============
        static void BuildPuzzleSection(Transform parent)
        {
            var gm = GameObject.Find("_GameManager");
            PuzzleGenerator generator = null;
            if (gm != null)
            {
                generator = gm.GetComponent<PuzzleGenerator>() ?? gm.AddComponent<PuzzleGenerator>();
                generator.table = Object.FindObjectOfType<TableController>();
                generator.aimManager = gm.GetComponent<AimManager>();
                var cue = GameObject.Find("CueBall");
                var target = GameObject.Find("TargetBall");
                if (cue != null) generator.cueBall = cue.GetComponent<BallController>();
                if (target != null) generator.targetBall = target.GetComponent<BallController>();
            }

            var section = CreateVerticalSection(parent, "PuzzleSection", 6f);

            CreateLabel(section.transform, "Title", "出题", 15f, TextAlignmentOptions.Center, 22f);

            // Angle row
            var angleRow = CreateHorizontalRow(section.transform, "AngleRow", 6f, 26f);
            CreateLabel(angleRow.transform, "Lbl", "切角", 13f, TextAlignmentOptions.MidlineLeft, 26f, 48f);
            var angleDD = CreateDropdown(angleRow.transform);

            // Pocket row
            var pocketRow = CreateHorizontalRow(section.transform, "PocketRow", 6f, 26f);
            CreateLabel(pocketRow.transform, "Lbl", "袋口", 13f, TextAlignmentOptions.MidlineLeft, 26f, 48f);
            var pocketDD = CreateDropdown(pocketRow.transform);

            CreateLabel(section.transform, "LblTgt", "子球 → 袋口", 12f, TextAlignmentOptions.Center, 18f);
            var tgtRow = CreateHorizontalRow(section.transform, "TgtRow", 6f, 28f);
            var btnTN = CreateRowButton(tgtRow.transform, "BtnTgtN", "近", new Color(0.3f, 0.3f, 0.3f, 0.8f), 14f);
            var btnTM = CreateRowButton(tgtRow.transform, "BtnTgtM", "中", new Color(0.3f, 0.3f, 0.3f, 0.8f), 14f);
            var btnTF = CreateRowButton(tgtRow.transform, "BtnTgtF", "远", new Color(0.3f, 0.3f, 0.3f, 0.8f), 14f);

            CreateLabel(section.transform, "LblCue", "主球 → 子球", 12f, TextAlignmentOptions.Center, 18f);
            var cueRow = CreateHorizontalRow(section.transform, "CueRow", 6f, 28f);
            var btnCN = CreateRowButton(cueRow.transform, "BtnCueN", "近", new Color(0.3f, 0.3f, 0.3f, 0.8f), 14f);
            var btnCM = CreateRowButton(cueRow.transform, "BtnCueM", "中", new Color(0.3f, 0.3f, 0.3f, 0.8f), 14f);
            var btnCF = CreateRowButton(cueRow.transform, "BtnCueF", "远", new Color(0.3f, 0.3f, 0.3f, 0.8f), 14f);

            var actionRow = CreateHorizontalRow(section.transform, "ActionRow", 8f, 40f);
            var btnGen = CreateRowButton(actionRow.transform, "BtnGenerate", "出题", new Color(0.85f, 0.25f, 0.25f, 0.95f), 17f);
            var btnRnd = CreateRowButton(actionRow.transform, "BtnRandom", "随机", new Color(0.25f, 0.45f, 0.85f, 0.95f), 17f);

            var ui = section.AddComponent<PuzzleUI>();
            ui.generator = generator;
            ui.angleDropdown = angleDD;
            ui.pocketDropdown = pocketDD;
            ui.btnTargetNear = btnTN.GetComponent<Button>();
            ui.btnTargetMid = btnTM.GetComponent<Button>();
            ui.btnTargetFar = btnTF.GetComponent<Button>();
            ui.btnCueNear = btnCN.GetComponent<Button>();
            ui.btnCueMid = btnCM.GetComponent<Button>();
            ui.btnCueFar = btnCF.GetComponent<Button>();
            ui.btnGenerate = btnGen.GetComponent<Button>();
            ui.btnRandom = btnRnd.GetComponent<Button>();
        }

        // ============ Layout Helpers ============
        static GameObject CreateVerticalSection(Transform parent, string name, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return go;
        }

        static GameObject CreateHorizontalRow(Transform parent, string name, float spacing, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return go;
        }

        static GameObject CreateLabel(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, float preferredHeight, float preferredWidth = -1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = align;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            if (preferredWidth > 0f) le.preferredWidth = preferredWidth;
            le.flexibleWidth = preferredWidth > 0f ? 0f : 1f;
            return go;
        }

        static GameObject CreateRowButton(Transform parent, string name, string label, Color bg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        static TMP_Dropdown CreateDropdown(Transform parent)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.22f, 0.9f);
            var dd = go.AddComponent<TMP_Dropdown>();
            dd.targetGraphic = bg;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8f, 2f);
            labelRT.offsetMax = new Vector2(-22f, -2f);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.fontSize = 13f;
            labelTmp.color = Color.white;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            dd.captionText = labelTmp;

            var arrow = new GameObject("Arrow", typeof(RectTransform));
            arrow.transform.SetParent(go.transform, false);
            var arrowRT = arrow.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1f, 0.5f);
            arrowRT.anchorMax = new Vector2(1f, 0.5f);
            arrowRT.pivot = new Vector2(1f, 0.5f);
            arrowRT.anchoredPosition = new Vector2(-6f, 0f);
            arrowRT.sizeDelta = new Vector2(14f, 14f);
            var arrowTxt = arrow.AddComponent<TextMeshProUGUI>();
            arrowTxt.text = "v";
            arrowTxt.fontSize = 12f;
            arrowTxt.fontStyle = FontStyles.Bold;
            arrowTxt.color = Color.white;
            arrowTxt.alignment = TextAlignmentOptions.Center;

            BuildDropdownTemplate(dd, go.transform);
            return dd;
        }

        static void BuildDropdownTemplate(TMP_Dropdown dd, Transform parent)
        {
            var template = new GameObject("Template", typeof(RectTransform));
            template.transform.SetParent(parent, false);
            var tRT = template.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0f, 0f);
            tRT.anchorMax = new Vector2(1f, 0f);
            tRT.pivot = new Vector2(0.5f, 1f);
            tRT.anchoredPosition = new Vector2(0f, 2f);
            tRT.sizeDelta = new Vector2(0f, 140f);
            template.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var sr = template.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(template.transform, false);
            var vRT = viewport.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.offsetMin = Vector2.zero;
            vRT.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0f, 1f);
            cRT.anchorMax = new Vector2(1f, 1f);
            cRT.pivot = new Vector2(0.5f, 1f);
            cRT.sizeDelta = new Vector2(0f, 24f);

            var item = new GameObject("Item", typeof(RectTransform));
            item.transform.SetParent(content.transform, false);
            var itemRT = item.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0f, 0.5f);
            itemRT.anchorMax = new Vector2(1f, 0.5f);
            itemRT.sizeDelta = new Vector2(0f, 22f);
            var toggle = item.AddComponent<Toggle>();

            var itemBg = new GameObject("Item Background", typeof(RectTransform));
            itemBg.transform.SetParent(item.transform, false);
            var ibgRT = itemBg.GetComponent<RectTransform>();
            ibgRT.anchorMin = Vector2.zero;
            ibgRT.anchorMax = Vector2.one;
            ibgRT.offsetMin = Vector2.zero;
            ibgRT.offsetMax = Vector2.zero;
            var itemBgImg = itemBg.AddComponent<Image>();
            itemBgImg.color = new Color(0.3f, 0.5f, 0.9f, 0.6f);
            toggle.targetGraphic = itemBgImg;

            var itemCheck = new GameObject("Item Checkmark", typeof(RectTransform));
            itemCheck.transform.SetParent(item.transform, false);
            var cRT2 = itemCheck.GetComponent<RectTransform>();
            cRT2.anchorMin = new Vector2(0f, 0.5f);
            cRT2.anchorMax = new Vector2(0f, 0.5f);
            cRT2.pivot = new Vector2(0.5f, 0.5f);
            cRT2.anchoredPosition = new Vector2(10f, 0f);
            cRT2.sizeDelta = new Vector2(10f, 10f);
            var checkImg = itemCheck.AddComponent<Image>();
            checkImg.color = Color.white;
            toggle.graphic = checkImg;

            var itemLabel = new GameObject("Item Label", typeof(RectTransform));
            itemLabel.transform.SetParent(item.transform, false);
            var itemLabelRT = itemLabel.GetComponent<RectTransform>();
            itemLabelRT.anchorMin = Vector2.zero;
            itemLabelRT.anchorMax = Vector2.one;
            itemLabelRT.offsetMin = new Vector2(22f, 2f);
            itemLabelRT.offsetMax = new Vector2(-8f, -2f);
            var itemLabelTxt = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelTxt.fontSize = 13f;
            itemLabelTxt.color = Color.white;
            itemLabelTxt.alignment = TextAlignmentOptions.MidlineLeft;

            sr.viewport = vRT;
            sr.content = cRT;

            dd.template = tRT;
            dd.itemText = itemLabelTxt;
            template.SetActive(false);
        }
    }
}
#endif
