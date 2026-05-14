using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PoolAimTrainer.Puzzles
{
    public class PuzzleUI : MonoBehaviour
    {
        public PuzzleGenerator generator;
        public TrainingModeController trainingModeController;
        public TMP_Dropdown modeDropdown;
        public UnityEngine.UI.Button btnStudyMode;
        public UnityEngine.UI.Button btnExamMode;
        public TMP_Dropdown angleDropdown;
        public TMP_Dropdown pocketDropdown;
        public GridLayoutGroup angleButtonGrid;
        public RectTransform pocketMap;
        public UnityEngine.UI.Button pocketRandomButton;

        [Header("Distance Buttons")]
        public UnityEngine.UI.Button btnTargetNear;
        public UnityEngine.UI.Button btnTargetMid;
        public UnityEngine.UI.Button btnTargetFar;
        public UnityEngine.UI.Button btnCueNear;
        public UnityEngine.UI.Button btnCueMid;
        public UnityEngine.UI.Button btnCueFar;

        [Header("Action Buttons")]
        public UnityEngine.UI.Button btnGenerate;
        public UnityEngine.UI.Button btnMainGenerate;
        public UnityEngine.UI.Button btnRandom;

        DistanceOption targetDist = DistanceOption.Medium;
        DistanceOption cueDist = DistanceOption.Medium;
        readonly System.Collections.Generic.List<UnityEngine.UI.Button> angleButtons =
            new System.Collections.Generic.List<UnityEngine.UI.Button>();
        readonly System.Collections.Generic.List<UnityEngine.UI.Button> pocketButtons =
            new System.Collections.Generic.List<UnityEngine.UI.Button>();

        Color activeColor = new Color(0.2f, 0.9f, 0.45f, 1f);
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        Color buttonBackgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
        Color pocketMapColor = new Color(0.04f, 0.22f, 0.10f, 1f);
        Color pocketMarkerColor = Color.white;
        const float AngleButtonHeight = 26f;
        const float AngleButtonSpacing = 4f;
        const int AngleButtonColumns = 4;
        const float PocketMapWidth = 150f;
        const float PocketMapHeight = 74f;
        const float PocketMapLineThickness = 2f;
        const float PocketMarkerSize = 24f;
        const float StandardLabelWidth = 70f;
        const float StandardContentWidth = 232f;
        const float StandardColumnSpacing = 6f;
        const int DefaultPocketIndex = 3;
        static Sprite filledPocketSprite;
        static Sprite ringPocketSprite;
        static Sprite roundedButtonSprite;
        static readonly Vector2[] PocketMapAnchors =
        {
            new Vector2(0.06f, 0.82f),
            new Vector2(0.5f, 0.82f),
            new Vector2(0.94f, 0.82f),
            new Vector2(0.94f, 0.18f),
            new Vector2(0.5f, 0.18f),
            new Vector2(0.06f, 0.18f),
        };

        void Start()
        {
            WireTrainingModeController();
            EnsureRuntimeModeControls();
            SetupAngleDropdown();
            EnsureAngleButtonGrid();
            SetupPocketDropdown();
            EnsurePocketMap();
            ApplyStandardPuzzleLayout();

            if (btnTargetNear != null) btnTargetNear.onClick.AddListener(() => SetTargetDist(DistanceOption.Near));
            if (btnTargetMid != null) btnTargetMid.onClick.AddListener(() => SetTargetDist(DistanceOption.Medium));
            if (btnTargetFar != null) btnTargetFar.onClick.AddListener(() => SetTargetDist(DistanceOption.Far));
            if (btnCueNear != null) btnCueNear.onClick.AddListener(() => SetCueDist(DistanceOption.Near));
            if (btnCueMid != null) btnCueMid.onClick.AddListener(() => SetCueDist(DistanceOption.Medium));
            if (btnCueFar != null) btnCueFar.onClick.AddListener(() => SetCueDist(DistanceOption.Far));

            EnsureMainGenerateButton();
            WireGenerateButton(btnGenerate);
            WireGenerateButton(btnMainGenerate);
            if (btnRandom != null) btnRandom.onClick.AddListener(OnRandom);
            WireModeButton(btnStudyMode, TrainingMode.Study);
            WireModeButton(btnExamMode, TrainingMode.Exam);
            if (angleDropdown != null) angleDropdown.onValueChanged.AddListener(_ => RefreshAngleButtons());
            if (pocketDropdown != null) pocketDropdown.onValueChanged.AddListener(_ => RefreshPocketMap());

            UpdateDistButtons();
            RefreshAngleButtons();
            RefreshPocketMap();
            RefreshModeButtons();
        }

        void WireTrainingModeController()
        {
            if (trainingModeController == null && generator != null)
            {
                trainingModeController = generator.GetComponent<TrainingModeController>();
                if (trainingModeController == null)
                    trainingModeController = generator.gameObject.AddComponent<TrainingModeController>();
            }

            if (trainingModeController == null) return;

            if (trainingModeController.aimManager == null && generator != null)
                trainingModeController.aimManager = generator.aimManager;
            if (trainingModeController.table == null && generator != null)
                trainingModeController.table = generator.table;
            if (trainingModeController.cueBall == null && generator != null)
                trainingModeController.cueBall = generator.cueBall;
            if (trainingModeController.targetBall == null && generator != null)
                trainingModeController.targetBall = generator.targetBall;
            trainingModeController.WireDefaults();
        }

        void EnsureRuntimeModeControls()
        {
            if (btnStudyMode != null && btnExamMode != null)
            {
                if (modeDropdown != null)
                    modeDropdown.gameObject.SetActive(false);
                return;
            }

            Transform row = FindModeRow();
            if (row == null)
                return;

            if (modeDropdown == null)
                modeDropdown = row.Find("Dropdown")?.GetComponent<TMP_Dropdown>();
            if (modeDropdown != null)
                modeDropdown.gameObject.SetActive(false);

            Transform existingStudy = row.Find("BtnStudyMode");
            if (existingStudy != null)
                btnStudyMode = existingStudy.GetComponent<UnityEngine.UI.Button>();
            Transform existingExam = row.Find("BtnExamMode");
            if (existingExam != null)
                btnExamMode = existingExam.GetComponent<UnityEngine.UI.Button>();

            if (btnStudyMode == null)
                btnStudyMode = CreateTextButton(row, "BtnStudyMode", "学习", 14f);
            if (btnExamMode == null)
                btnExamMode = CreateTextButton(row, "BtnExamMode", "考试", 14f);
        }

        Transform FindModeRow()
        {
            if (modeDropdown != null && modeDropdown.transform.parent != null)
                return modeDropdown.transform.parent;
            if (btnStudyMode != null && btnStudyMode.transform.parent != null)
                return btnStudyMode.transform.parent;
            if (btnExamMode != null && btnExamMode.transform.parent != null)
                return btnExamMode.transform.parent;
            if (angleDropdown == null || angleDropdown.transform.parent == null)
                return null;

            Transform angleRow = angleDropdown.transform.parent;
            Transform section = angleRow.parent;
            if (section == null)
                return null;

            Transform existing = section.Find("ModeRow");
            if (existing != null)
                return existing;

            var rowGo = new GameObject("ModeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(section, false);
            rowGo.transform.SetSiblingIndex(angleRow.GetSiblingIndex());

            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var layoutElement = rowGo.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 26f;

            var labelGo = new GameObject("Lbl", typeof(RectTransform), typeof(LayoutElement));
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelLayout = labelGo.GetComponent<LayoutElement>();
            labelLayout.preferredWidth = StandardLabelWidth;
            labelLayout.flexibleWidth = 0f;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "模式";
            label.fontSize = 13f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return rowGo.transform;
        }

        void SetupAngleDropdown()
        {
            if (angleDropdown == null) return;
            angleDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string> { "Any" };
            for (int a = 0; a <= 90; a += 5)
                opts.Add(a + "°");
            angleDropdown.AddOptions(opts);
            angleDropdown.value = 0;
        }

        void EnsureAngleButtonGrid()
        {
            if (angleDropdown == null || angleDropdown.transform.parent == null)
                return;

            Transform row = angleDropdown.transform.parent;
            if (angleButtonGrid == null)
            {
                Transform existing = row.Find("AngleButtonGrid");
                if (existing != null)
                    angleButtonGrid = existing.GetComponent<GridLayoutGroup>();
            }

            if (angleButtonGrid == null)
            {
                var gridGo = new GameObject("AngleButtonGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
                gridGo.transform.SetParent(row, false);
                angleButtonGrid = gridGo.GetComponent<GridLayoutGroup>();
                var layout = gridGo.GetComponent<LayoutElement>();
                layout.flexibleWidth = 1f;
                layout.preferredHeight = 7f * 26f + 6f * 4f;
            }

            angleDropdown.gameObject.SetActive(false);
            angleButtonGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            angleButtonGrid.constraintCount = AngleButtonColumns;
            angleButtonGrid.spacing = new Vector2(AngleButtonSpacing, AngleButtonSpacing);
            angleButtonGrid.childAlignment = TextAnchor.UpperLeft;
            angleButtonGrid.cellSize = new Vector2(GetStandardAngleButtonWidth(), AngleButtonHeight);

            angleButtons.Clear();
            for (int i = angleButtonGrid.transform.childCount - 1; i >= 0; i--)
                DestroyRuntimeOrImmediate(angleButtonGrid.transform.GetChild(i).gameObject);

            if (angleDropdown.options == null || angleDropdown.options.Count == 0)
                return;

            for (int i = 0; i < angleDropdown.options.Count; i++)
            {
                int captured = i;
                var button = CreateAngleButton(angleButtonGrid.transform, angleDropdown.options[i].text);
                button.onClick.AddListener(() => SetAngleIndex(captured));
                angleButtons.Add(button);
            }

            UpdateAngleRowHeight(row, angleDropdown.options.Count);
        }

        UnityEngine.UI.Button CreateAngleButton(Transform parent, string labelText)
        {
            var go = new GameObject("Angle_" + labelText, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            ConfigureButtonBackground(go.GetComponent<Image>());

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            text.fontSize = 14f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            return go.GetComponent<UnityEngine.UI.Button>();
        }

        void UpdateAngleRowHeight(Transform row, int optionCount)
        {
            int rows = Mathf.CeilToInt(optionCount / (float)AngleButtonColumns);
            float height = rows * AngleButtonHeight + Mathf.Max(0, rows - 1) * AngleButtonSpacing;

            var rowLayout = row.GetComponent<LayoutElement>();
            if (rowLayout != null)
                rowLayout.preferredHeight = height;

            var label = row.Find("Lbl");
            if (label != null)
            {
                var labelLayout = label.GetComponent<LayoutElement>();
                if (labelLayout != null)
                    labelLayout.preferredHeight = height;
            }

            if (angleButtonGrid != null)
            {
                var gridLayout = angleButtonGrid.GetComponent<LayoutElement>();
                if (gridLayout != null)
                    gridLayout.preferredHeight = height;
            }
        }

        void SetAngleIndex(int index)
        {
            if (angleDropdown != null)
                angleDropdown.SetValueWithoutNotify(index);
            RefreshAngleButtons();
        }

        void RefreshAngleButtons()
        {
            int selected = angleDropdown != null ? angleDropdown.value : 0;
            for (int i = 0; i < angleButtons.Count; i++)
                SetBtnColor(angleButtons[i], i == selected);
        }

        void WireModeButton(UnityEngine.UI.Button button, TrainingMode mode)
        {
            if (button == null)
                return;

            if (mode == TrainingMode.Study)
            {
                button.onClick.RemoveListener(SetStudyMode);
                button.onClick.AddListener(SetStudyMode);
            }
            else
            {
                button.onClick.RemoveListener(SetExamMode);
                button.onClick.AddListener(SetExamMode);
            }
        }

        void SetStudyMode()
        {
            SetMode(TrainingMode.Study);
        }

        void SetExamMode()
        {
            SetMode(TrainingMode.Exam);
        }

        void SetMode(TrainingMode mode)
        {
            if (trainingModeController != null)
                trainingModeController.SetMode(mode);
            RefreshModeButtons();
        }

        void RefreshModeButtons()
        {
            TrainingMode mode = trainingModeController != null
                ? trainingModeController.mode
                : TrainingMode.Study;
            SetBtnColor(btnStudyMode, mode == TrainingMode.Study);
            SetBtnColor(btnExamMode, mode == TrainingMode.Exam);
        }

        void SetupPocketDropdown()
        {
            if (pocketDropdown == null) return;
            pocketDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string> { "Random", "1", "2", "3", "4", "5", "6" };
            pocketDropdown.AddOptions(opts);
            pocketDropdown.value = DefaultPocketIndex;
        }

        void EnsurePocketMap()
        {
            if (pocketDropdown == null || pocketDropdown.transform.parent == null)
                return;

            Transform row = pocketDropdown.transform.parent;
            if (pocketMap == null)
            {
                Transform existing = row.Find("PocketMap");
                if (existing != null)
                    pocketMap = existing.GetComponent<RectTransform>();
            }

            if (pocketMap == null)
            {
                var mapGo = new GameObject("PocketMap", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                mapGo.transform.SetParent(row, false);
                pocketMap = mapGo.GetComponent<RectTransform>();
            }

            ConfigurePocketMap(row);
            RebuildPocketMarkers();
            EnsurePocketRandomButton(row);
            pocketDropdown.gameObject.SetActive(false);
            UpdatePocketRowHeight(row);
        }

        void ConfigurePocketMap(Transform row)
        {
            var mapLayout = pocketMap.GetComponent<LayoutElement>();
            if (mapLayout != null)
            {
                mapLayout.preferredWidth = PocketMapWidth;
                mapLayout.preferredHeight = PocketMapHeight;
                mapLayout.flexibleWidth = 0f;
            }

            pocketMap.sizeDelta = new Vector2(PocketMapWidth, PocketMapHeight);

            var mapImage = pocketMap.GetComponent<Image>();
            if (mapImage != null)
            {
                mapImage.color = pocketMapColor;
                mapImage.raycastTarget = false;
            }

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = Mathf.Max(layout.spacing, 6f);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
            }
        }

        void RebuildPocketMarkers()
        {
            pocketButtons.Clear();
            for (int i = pocketMap.childCount - 1; i >= 0; i--)
                DestroyRuntimeOrImmediate(pocketMap.GetChild(i).gameObject);

            CreatePocketMapLine("RailTop", new Vector2(0.5f, 0.82f), new Vector2(PocketMapWidth * 0.9f, PocketMapLineThickness));
            CreatePocketMapLine("RailBottom", new Vector2(0.5f, 0.18f), new Vector2(PocketMapWidth * 0.9f, PocketMapLineThickness));
            CreatePocketMapLine("RailLeft", new Vector2(0.06f, 0.5f), new Vector2(PocketMapLineThickness, PocketMapHeight * 0.64f));
            CreatePocketMapLine("RailRight", new Vector2(0.94f, 0.5f), new Vector2(PocketMapLineThickness, PocketMapHeight * 0.64f));

            for (int i = 0; i < PocketMapAnchors.Length; i++)
            {
                int pocketIndex = i + 1;
                var button = CreatePocketMarker(pocketMap, pocketIndex, PocketMapAnchors[i]);
                button.onClick.AddListener(() => SetPocketIndex(pocketIndex));
                pocketButtons.Add(button);
            }
        }

        void CreatePocketMapLine(string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pocketMap, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = pocketMarkerColor;
            image.raycastTarget = false;
        }

        UnityEngine.UI.Button CreatePocketMarker(Transform parent, int pocketIndex, Vector2 anchor)
        {
            var go = new GameObject("Pocket_" + pocketIndex, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PocketMarkerSize, PocketMarkerSize);

            var hitImage = go.GetComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            hitImage.raycastTarget = true;

            var markerGo = new GameObject("MarkerVisual", typeof(RectTransform), typeof(Image));
            markerGo.transform.SetParent(go.transform, false);
            var markerRt = markerGo.GetComponent<RectTransform>();
            markerRt.anchorMin = new Vector2(0.5f, 0.5f);
            markerRt.anchorMax = new Vector2(0.5f, 0.5f);
            markerRt.pivot = new Vector2(0.5f, 0.5f);
            markerRt.anchoredPosition = Vector2.zero;
            markerRt.sizeDelta = new Vector2(16f, 16f);

            var markerImage = markerGo.GetComponent<Image>();
            markerImage.sprite = GetPocketCircleSprite(false);
            markerImage.color = pocketMarkerColor;
            markerImage.raycastTarget = false;

            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = hitImage;
            return button;
        }

        void EnsurePocketRandomButton(Transform row)
        {
            if (pocketRandomButton == null)
            {
                Transform existing = row.Find("PocketRandom");
                if (existing != null)
                    pocketRandomButton = existing.GetComponent<UnityEngine.UI.Button>();
            }

            if (pocketRandomButton == null)
            {
                pocketRandomButton = CreateTextButton(row, "PocketRandom", "随机袋", 13f);
            }

            var layout = pocketRandomButton.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = 74f;
                layout.preferredHeight = PocketMapHeight;
                layout.flexibleWidth = 0f;
            }
            SetButtonText(pocketRandomButton, "随机袋");

            pocketRandomButton.onClick.RemoveAllListeners();
            pocketRandomButton.onClick.AddListener(() => SetPocketIndex(0));
        }

        void ApplyStandardPuzzleLayout()
        {
            Transform section = transform;
            Transform tgtLabel = section.Find("LblTgt");
            Transform cueLabel = section.Find("LblCue");

            ApplyStandardRow(section.Find("ModeRow"), "模式");
            ApplyStandardRow(section.Find("AngleRow"), "∠1");
            ApplyStandardRow(section.Find("PocketRow"), "袋口");
            ApplyStandardRow(section.Find("TgtRow"), GetText(tgtLabel, "子球 → 袋口"));
            ApplyStandardRow(section.Find("CueRow"), GetText(cueLabel, "主球 → 子球"));
            HideDetachedLabel(tgtLabel);
            HideDetachedLabel(cueLabel);
            HideActionRandomButton();
            ApplyActionRowStandard(section.Find("ActionRow"));
        }

        void ApplyStandardRow(Transform row, string labelText)
        {
            if (row == null)
                return;

            var layout = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = StandardColumnSpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var label = EnsureRowLabel(row, labelText);
            var content = EnsureRowContent(row);
            MoveRowChildrenIntoContent(row, label, content);
            ConfigureLabelColumn(label, labelText);
            ConfigureContentColumn(content);
            ConfigureContentLayout(content);
        }

        Transform EnsureRowLabel(Transform row, string labelText)
        {
            Transform label = row.Find("Lbl");
            if (label != null)
                return label;

            var labelGo = new GameObject("Lbl", typeof(RectTransform), typeof(LayoutElement));
            labelGo.transform.SetParent(row, false);
            var text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            text.fontSize = 13f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return labelGo.transform;
        }

        Transform EnsureRowContent(Transform row)
        {
            Transform content = row.Find("Content");
            if (content != null)
                return content;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            contentGo.transform.SetParent(row, false);
            return contentGo.transform;
        }

        void MoveRowChildrenIntoContent(Transform row, Transform label, Transform content)
        {
            label.SetSiblingIndex(0);
            content.SetSiblingIndex(1);
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in row)
            {
                if (child == label || child == content)
                    continue;
                children.Add(child);
            }

            for (int i = 0; i < children.Count; i++)
            {
                children[i].SetParent(content, false);
                children[i].SetSiblingIndex(i);
            }
        }

        void ConfigureLabelColumn(Transform label, string labelText)
        {
            var layout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = StandardLabelWidth;
            layout.flexibleWidth = 0f;

            var text = label.GetComponent<TextMeshProUGUI>() ?? label.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            text.fontSize = 13f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
        }

        void ConfigureContentColumn(Transform content)
        {
            var layout = content.GetComponent<LayoutElement>() ?? content.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = StandardContentWidth;
            layout.flexibleWidth = 0f;
        }

        void ConfigureContentLayout(Transform content)
        {
            var layout = content.GetComponent<HorizontalLayoutGroup>() ?? content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = StandardColumnSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        void ApplyActionRowStandard(Transform row)
        {
            if (row == null)
                return;

            var layout = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = StandardColumnSpacing;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var rowLayout = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = StandardContentWidth;
            rowLayout.flexibleWidth = 0f;
        }

        static string GetText(Transform label, string fallback)
        {
            var text = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
            return text != null && !string.IsNullOrWhiteSpace(text.text) ? text.text : fallback;
        }

        static void HideDetachedLabel(Transform label)
        {
            if (label == null)
                return;
            label.gameObject.SetActive(false);
            var layout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 0f;
            layout.ignoreLayout = true;
        }

        void HideActionRandomButton()
        {
            if (btnRandom == null)
                return;

            btnRandom.onClick.RemoveAllListeners();
            btnRandom.gameObject.SetActive(false);
            var layout = btnRandom.GetComponent<LayoutElement>() ?? btnRandom.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 0f;
        }

        static float GetStandardAngleButtonWidth()
        {
            return (StandardContentWidth - AngleButtonSpacing * (AngleButtonColumns - 1)) / AngleButtonColumns;
        }

        UnityEngine.UI.Button CreateTextButton(Transform parent, string name, string labelText, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            ConfigureButtonBackground(go.GetComponent<Image>());

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        static void SetButtonText(UnityEngine.UI.Button button, string labelText)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = labelText;
        }

        void UpdatePocketRowHeight(Transform row)
        {
            var rowLayout = row.GetComponent<LayoutElement>();
            if (rowLayout != null)
                rowLayout.preferredHeight = PocketMapHeight;

            var label = row.Find("Lbl");
            if (label != null)
            {
                var labelLayout = label.GetComponent<LayoutElement>();
                if (labelLayout != null)
                    labelLayout.preferredHeight = PocketMapHeight;
            }
        }

        void SetPocketIndex(int index)
        {
            if (pocketDropdown != null)
                pocketDropdown.SetValueWithoutNotify(index);
            RefreshPocketMap();
        }

        void RefreshPocketMap()
        {
            int selected = pocketDropdown != null ? pocketDropdown.value : 0;
            for (int i = 0; i < pocketButtons.Count; i++)
            {
                bool active = selected == i + 1;
                var marker = pocketButtons[i].transform.Find("MarkerVisual");
                var image = marker != null ? marker.GetComponent<Image>() : null;
                if (image != null)
                {
                    image.sprite = GetPocketCircleSprite(active);
                    image.color = active ? activeColor : pocketMarkerColor;
                }
            }

            SetBtnColor(pocketRandomButton, selected == 0);
        }

        void SetTargetDist(DistanceOption d)
        {
            targetDist = d;
            UpdateDistButtons();
        }

        void SetCueDist(DistanceOption d)
        {
            cueDist = d;
            UpdateDistButtons();
        }

        void UpdateDistButtons()
        {
            SetBtnColor(btnTargetNear, targetDist == DistanceOption.Near);
            SetBtnColor(btnTargetMid, targetDist == DistanceOption.Medium);
            SetBtnColor(btnTargetFar, targetDist == DistanceOption.Far);
            SetBtnColor(btnCueNear, cueDist == DistanceOption.Near);
            SetBtnColor(btnCueMid, cueDist == DistanceOption.Medium);
            SetBtnColor(btnCueFar, cueDist == DistanceOption.Far);
        }

        void SetBtnColor(UnityEngine.UI.Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            ConfigureButtonBackground(img);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
                return;
            label.color = active ? activeColor : Color.white;
            label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        }

        void ConfigureButtonBackground(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = buttonBackgroundColor;
        }

        static void DestroyRuntimeOrImmediate(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        static Sprite GetPocketCircleSprite(bool filled)
        {
            if (filled && filledPocketSprite != null)
                return filledPocketSprite;
            if (!filled && ringPocketSprite != null)
                return ringPocketSprite;

            const int size = 32;
            const float ringThickness = 3f;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = filled ? "PocketFilledDot" : "PocketRingDot",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float edgeAlpha = Mathf.Clamp01(radius + 0.75f - distance);
                    float alpha = filled
                        ? edgeAlpha
                        : Mathf.Clamp01(edgeAlpha * Mathf.Clamp01(distance - (radius - ringThickness) + 0.75f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;

            if (filled)
                filledPocketSprite = sprite;
            else
                ringPocketSprite = sprite;
            return sprite;
        }

        static Sprite GetRoundedButtonSprite()
        {
            if (roundedButtonSprite != null)
                return roundedButtonSprite;

            const int size = 32;
            const float radius = 5f;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = "PuzzleRoundedButton",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(radius - px, 0f, px - (size - radius));
                    float dy = Mathf.Max(radius - py, 0f, py - (size - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);

            roundedButtonSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedButtonSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedButtonSprite;
        }

        PuzzleParams BuildParams()
        {
            var p = new PuzzleParams();

            int angleIdx = angleDropdown != null ? angleDropdown.value : 0;
            if (angleIdx == 0)
            {
                p.anyAngle = true;
                p.cutAngleDeg = -1f;
            }
            else
            {
                p.anyAngle = false;
                p.cutAngleDeg = (angleIdx - 1) * 5f;
            }

            p.targetToPocket = targetDist;
            p.cueToTarget = cueDist;
            p.pocketIndex = pocketDropdown != null ? pocketDropdown.value : 0;

            return p;
        }

        void WireGenerateButton(UnityEngine.UI.Button button)
        {
            if (button == null)
                return;

            ConfigureActionButtonBackground(button.GetComponent<Image>());
            button.onClick.RemoveListener(OnGenerate);
            button.onClick.AddListener(OnGenerate);
        }

        void EnsureMainGenerateButton()
        {
            if (btnMainGenerate != null)
                return;

            Transform toolbar = GameObject.Find("LeftToolbar")?.transform;
            if (toolbar == null)
                return;

            Transform existing = toolbar.Find("BtnMainGenerate");
            if (existing != null)
            {
                btnMainGenerate = existing.GetComponent<UnityEngine.UI.Button>();
                return;
            }

            btnMainGenerate = CreateToolbarGenerateButton(toolbar);
        }

        UnityEngine.UI.Button CreateToolbarGenerateButton(Transform toolbar)
        {
            var go = new GameObject("BtnMainGenerate", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button), typeof(LayoutElement));
            go.transform.SetParent(toolbar, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 80f;
            layout.preferredHeight = 36f;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;

            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "出题";
            text.fontSize = 16f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            return button;
        }

        void ConfigureActionButtonBackground(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
        }

        void OnGenerate()
        {
            GenerateCurrentPuzzle();
        }

        public virtual bool GenerateCurrentPuzzle()
        {
            if (generator == null)
                return false;

            bool generated = generator.Generate(BuildParams());
            if (generated && trainingModeController != null)
                trainingModeController.OnPuzzleGenerated();
            return generated;
        }

        void OnRandom()
        {
            if (generator == null) return;
            var p = new PuzzleParams
            {
                anyAngle = true,
                cutAngleDeg = -1f,
                targetToPocket = (DistanceOption)Random.Range(0, 3),
                cueToTarget = (DistanceOption)Random.Range(0, 3),
                pocketIndex = 0
            };
            if (generator.Generate(p) && trainingModeController != null)
                trainingModeController.OnPuzzleGenerated();
        }

    }
}
