using System.Reflection;
using System.Linq;
using NUnit.Framework;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;
using PoolAimTrainer.Visualization;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Tests.EditMode
{
    public class ReferenceLineVisibilityTests
    {
        GameObject root;
        const string TestPrefsPrefix = "PoolAimTrainer.Tests.ReferenceLineVisibility.";

        [SetUp]
        public void SetUp()
        {
            ClearTestPrefs();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
            ClearTestPrefs();
        }

        [Test]
        public void MasterToggleHidesAllLayersWithoutForgettingIndividualChoices()
        {
            root = new GameObject("ReferenceLineVisibilityRoot");
            var visibility = CreateTestVisibility(root);

            visibility.SetLayerVisible(ReferenceVisualLayer.ObjectToPocket, false);

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.True);
            Assert.That(visibility.IsVisible(ReferenceVisualLayer.ObjectToPocket), Is.False);

            visibility.SetMasterVisible(false);

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.False);
            Assert.That(visibility.IsVisible(ReferenceVisualLayer.ObjectToPocket), Is.False);

            visibility.SetMasterVisible(true);

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.True);
            Assert.That(visibility.IsVisible(ReferenceVisualLayer.ObjectToPocket), Is.False);
        }

        [Test]
        public void AimLineRenderer_RespectsIndependentLayerToggles()
        {
            root = new GameObject("AimLineVisibilityRoot");
            var visibility = CreateTestVisibility(root);
            visibility.SetLayerVisible(ReferenceVisualLayer.ObjectToPocket, false);

            var renderer = root.AddComponent<AimLineRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                new Vector3(0f, 0.03f, 0f),
                new Vector3(0.4f, 0.03f, 0f),
                new Vector3(0.4f, 0.03f, 0f),
                new Vector3(0.8f, 0.03f, 0f));

            Assert.That(FindLine(root.transform, "LR_CueToGhost").enabled, Is.True);
            Assert.That(FindLine(root.transform, "LR_ObjToPocket").enabled, Is.False);

            visibility.SetMasterVisible(false);
            renderer.Show(
                new Vector3(0f, 0.03f, 0f),
                new Vector3(0.4f, 0.03f, 0f),
                new Vector3(0.4f, 0.03f, 0f),
                new Vector3(0.8f, 0.03f, 0f));

            Assert.That(FindLine(root.transform, "LR_CueToGhost").enabled, Is.False);
            Assert.That(FindLine(root.transform, "LR_ObjToPocket").enabled, Is.False);
        }

        [Test]
        public void ActivateAsCurrent_LoadsSavedMasterAndLayerState()
        {
            root = new GameObject("ReferenceLinePersistenceRoot");
            var visibility = CreateTestVisibility(root);

            visibility.SetMasterVisible(false);
            visibility.SetLayerVisible(ReferenceVisualLayer.ObjectToPocket, false);
            visibility.SetLayerVisible(ReferenceVisualLayer.CueToGhost, true);
            visibility.SavePreferences();
            Object.DestroyImmediate(root);

            root = new GameObject("ReferenceLinePersistenceReloadRoot");
            var reloaded = root.AddComponent<ReferenceLineVisibility>();
            reloaded.playerPrefsKeyPrefix = TestPrefsPrefix;
            reloaded.ActivateAsCurrent();

            Assert.That(reloaded.masterVisible, Is.False);
            Assert.That(reloaded.IsLayerEnabled(ReferenceVisualLayer.ObjectToPocket), Is.False);
            Assert.That(reloaded.IsLayerEnabled(ReferenceVisualLayer.CueToGhost), Is.True);
        }

        [Test]
        public void EnsureDefaultLayers_IncludesEveryReferenceVisualLayer()
        {
            root = new GameObject("ReferenceLineFutureLayerRoot");
            var visibility = CreateTestVisibility(root);

            var configured = visibility.Layers.Select(layer => layer.layer).ToArray();

            foreach (ReferenceVisualLayer layer in System.Enum.GetValues(typeof(ReferenceVisualLayer)))
                Assert.That(configured, Does.Contain(layer), layer + " should get a toggle automatically.");
        }

        [Test]
        public void AngleLayerLabels_IncludeAngleNames()
        {
            root = new GameObject("ReferenceLineAngleLabelRoot");
            var visibility = CreateTestVisibility(root);

            Assert.That(visibility.GetLabel(ReferenceVisualLayer.CutAngleArc), Does.StartWith("∠1 "));
            Assert.That(visibility.GetLabel(ReferenceVisualLayer.AimVsTargetArc), Does.StartWith("∠2 "));
            Assert.That(visibility.GetLabel(ReferenceVisualLayer.EstimatedAimToTargetPathAngle), Does.StartWith("∠3 "));
            Assert.That(visibility.GetLabel(ReferenceVisualLayer.TargetPathToCueThroughAngle), Does.StartWith("∠4 "));
        }

        [Test]
        public void RuntimeLayerOverride_HidesLayerWithoutChangingSavedLayerChoice()
        {
            root = new GameObject("ReferenceLineRuntimeOverrideRoot");
            var visibility = CreateTestVisibility(root);
            visibility.SetLayerVisible(ReferenceVisualLayer.CueToGhost, true);

            visibility.SetRuntimeLayerVisibilityOverride(ReferenceVisualLayer.CueToGhost, false);

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.False);
            Assert.That(visibility.IsLayerEnabled(ReferenceVisualLayer.CueToGhost), Is.True);

            visibility.ClearRuntimeLayerVisibilityOverrides();

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.True);
        }

        [Test]
        public void EnsureDefaultLayers_MigratesLegacyAngleLabels()
        {
            root = new GameObject("ReferenceLineLegacyAngleLabelRoot");
            var visibility = root.AddComponent<ReferenceLineVisibility>();
            var layers = GetMutableLayers(visibility);
            layers.Clear();
            layers.Add(new ReferenceLineVisibility.LayerState
            {
                layer = ReferenceVisualLayer.CutAngleArc,
                label = "黄弧 切角",
                visible = true,
            });
            layers.Add(new ReferenceLineVisibility.LayerState
            {
                layer = ReferenceVisualLayer.AimVsTargetArc,
                label = "my custom aim angle",
                visible = true,
            });

            visibility.EnsureDefaultLayers();

            Assert.That(visibility.GetLabel(ReferenceVisualLayer.CutAngleArc), Is.EqualTo("∠1 黄弧 红-绿夹角"));
            Assert.That(visibility.GetLabel(ReferenceVisualLayer.AimVsTargetArc), Is.EqualTo("my custom aim angle"));
        }

        [Test]
        public void TogglePanel_BuildsInsideRightPanelAfterExistingContent()
        {
            root = new GameObject("Canvas", typeof(Canvas));
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(root.transform, false);
            var existingSection = new GameObject("PuzzleSection", typeof(RectTransform));
            existingSection.transform.SetParent(rightPanel.transform, false);
            var visibility = CreateTestVisibility(root);
            var togglePanel = root.AddComponent<ReferenceLineTogglePanel>();
            togglePanel.visibility = visibility;

            togglePanel.BuildPanel();

            Transform embeddedPanel = rightPanel.transform.Find("ReferenceLineTogglePanel");
            Assert.That(embeddedPanel, Is.Not.Null);
            Assert.That(embeddedPanel.GetSiblingIndex(), Is.EqualTo(rightPanel.transform.childCount - 1));
            Assert.That(root.transform.Find("ReferenceLineTogglePanel"), Is.Null);
        }

        [Test]
        public void TogglePanel_ConfiguresWideRightPanelHudAndCollapseButton()
        {
            root = new GameObject("Canvas", typeof(Canvas));
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform), typeof(LayoutElement));
            rightPanel.transform.SetParent(root.transform, false);
            var hudSection = new GameObject("HudSection", typeof(RectTransform));
            hudSection.transform.SetParent(rightPanel.transform, false);
            var hudImage = new GameObject("RawImage", typeof(RectTransform), typeof(LayoutElement));
            hudImage.transform.SetParent(hudSection.transform, false);
            var visibility = CreateTestVisibility(root);
            var togglePanel = root.AddComponent<ReferenceLineTogglePanel>();
            togglePanel.visibility = visibility;

            togglePanel.BuildPanel();

            var rightPanelRect = rightPanel.GetComponent<RectTransform>();
            var rightPanelLayout = rightPanel.GetComponent<LayoutElement>();
            var rightPanelImage = rightPanel.GetComponent<Image>();
            var rightPanelGroup = rightPanel.GetComponent<VerticalLayoutGroup>();
            var hudLayout = hudImage.GetComponent<LayoutElement>();
            Assert.That(rightPanelRect.sizeDelta.x, Is.EqualTo(350f).Within(0.01f));
            Assert.That(rightPanelLayout.preferredWidth, Is.EqualTo(350f).Within(0.01f));
            Assert.That(rightPanelImage, Is.Not.Null);
            Assert.That(rightPanelImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(rightPanelImage.color, Is.EqualTo(new Color(0.08f, 0.08f, 0.08f, 0.92f)));
            Assert.That(rightPanelGroup, Is.Not.Null);
            Assert.That(rightPanelGroup.padding.left, Is.EqualTo(10));
            Assert.That(rightPanelGroup.padding.right, Is.EqualTo(10));
            Assert.That(rightPanelGroup.padding.top, Is.EqualTo(10));
            Assert.That(rightPanelGroup.padding.bottom, Is.EqualTo(10));
            Assert.That(rightPanelGroup.spacing, Is.EqualTo(8f));
            Assert.That(
                rightPanelRect.sizeDelta.x - rightPanelGroup.padding.left - rightPanelGroup.padding.right,
                Is.EqualTo(330f).Within(0.01f));
            Assert.That(hudLayout.preferredHeight, Is.EqualTo(190f).Within(0.01f));
            Assert.That(root.transform.Find("RightPanelToggleButton"), Is.Not.Null);
        }

        [Test]
        public void RightPanelToggleButton_HidesAndShowsRightPanel()
        {
            root = new GameObject("Canvas", typeof(Canvas));
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(root.transform, false);
            var visibility = CreateTestVisibility(root);
            var togglePanel = root.AddComponent<ReferenceLineTogglePanel>();
            togglePanel.visibility = visibility;
            togglePanel.BuildPanel();

            var button = root.transform.Find("RightPanelToggleButton").GetComponent<Button>();

            button.onClick.Invoke();
            Assert.That(rightPanel.activeSelf, Is.False);

            button.onClick.Invoke();
            Assert.That(rightPanel.activeSelf, Is.True);
        }

        [Test]
        public void TogglePanel_UsesTwoColumnGridForLayerToggles()
        {
            root = new GameObject("Canvas", typeof(Canvas));
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(root.transform, false);
            var visibility = CreateTestVisibility(root);
            var togglePanel = root.AddComponent<ReferenceLineTogglePanel>();
            togglePanel.visibility = visibility;

            togglePanel.BuildPanel();

            Transform layersGrid = rightPanel.transform.Find("ReferenceLineTogglePanel/LayersGrid");
            Assert.That(layersGrid, Is.Not.Null);
            var grid = layersGrid.GetComponent<GridLayoutGroup>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(2));
        }

        [Test]
        public void TogglePanel_MakesPuzzleAndReferenceSectionsCollapsible()
        {
            root = new GameObject("Canvas", typeof(Canvas));
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(root.transform, false);

            var puzzleSection = new GameObject("PuzzleSection", typeof(RectTransform));
            puzzleSection.transform.SetParent(rightPanel.transform, false);
            var puzzleTitle = new GameObject("Title", typeof(RectTransform));
            puzzleTitle.transform.SetParent(puzzleSection.transform, false);
            puzzleTitle.AddComponent<TMPro.TextMeshProUGUI>().text = "出题";
            var puzzleBody = new GameObject("ModeRow", typeof(RectTransform));
            puzzleBody.transform.SetParent(puzzleSection.transform, false);
            var hiddenDetachedLabel = new GameObject("LblTgt", typeof(RectTransform), typeof(LayoutElement));
            hiddenDetachedLabel.transform.SetParent(puzzleSection.transform, false);
            hiddenDetachedLabel.SetActive(false);
            hiddenDetachedLabel.GetComponent<LayoutElement>().ignoreLayout = true;

            var visibility = CreateTestVisibility(root);
            var togglePanel = root.AddComponent<ReferenceLineTogglePanel>();
            togglePanel.visibility = visibility;

            togglePanel.BuildPanel();

            var puzzleButton = puzzleTitle.GetComponent<Button>();
            Assert.That(puzzleButton, Is.Not.Null);
            puzzleButton.onClick.Invoke();
            Assert.That(puzzleTitle.activeSelf, Is.True);
            Assert.That(puzzleBody.activeSelf, Is.False);
            Assert.That(hiddenDetachedLabel.activeSelf, Is.False);

            puzzleButton.onClick.Invoke();
            Assert.That(puzzleBody.activeSelf, Is.True);
            Assert.That(hiddenDetachedLabel.activeSelf, Is.False);

            Transform referencePanel = rightPanel.transform.Find("ReferenceLineTogglePanel");
            var referenceTitle = referencePanel.Find("Title").gameObject;
            var referenceGrid = referencePanel.Find("LayersGrid").gameObject;
            var referenceButton = referenceTitle.GetComponent<Button>();
            Assert.That(referenceButton, Is.Not.Null);
            referenceButton.onClick.Invoke();
            Assert.That(referenceTitle.activeSelf, Is.True);
            Assert.That(referenceGrid.activeSelf, Is.False);
        }

        [Test]
        public void TogglePanel_StylesPuzzleSectionLikeMockupCard()
        {
            root = new GameObject("Canvas", typeof(Canvas));
            var rightPanel = new GameObject("RightPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(root.transform, false);

            var puzzleSection = new GameObject("PuzzleSection", typeof(RectTransform), typeof(VerticalLayoutGroup));
            puzzleSection.transform.SetParent(rightPanel.transform, false);
            var puzzleTitle = new GameObject("Title", typeof(RectTransform));
            puzzleTitle.transform.SetParent(puzzleSection.transform, false);
            var titleLabel = puzzleTitle.AddComponent<TMPro.TextMeshProUGUI>();
            titleLabel.text = "出题";
            titleLabel.alignment = TMPro.TextAlignmentOptions.Center;
            var puzzleBody = new GameObject("ModeRow", typeof(RectTransform));
            puzzleBody.transform.SetParent(puzzleSection.transform, false);

            var visibility = CreateTestVisibility(root);
            var togglePanel = root.AddComponent<ReferenceLineTogglePanel>();
            togglePanel.visibility = visibility;

            togglePanel.BuildPanel();

            var sectionImage = puzzleSection.GetComponent<Image>();
            var sectionOutline = puzzleSection.GetComponent<Outline>();
            var sectionLayout = puzzleSection.GetComponent<VerticalLayoutGroup>();
            Assert.That(sectionImage, Is.Not.Null);
            Assert.That(sectionImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(sectionImage.color, Is.EqualTo(new Color(0.11f, 0.11f, 0.11f, 0.9f)));
            Assert.That(sectionOutline, Is.Not.Null);
            Assert.That(sectionOutline.effectColor, Is.EqualTo(new Color(0.32f, 0.32f, 0.32f, 0.55f)));
            Assert.That(sectionLayout.padding.left, Is.EqualTo(10));
            Assert.That(sectionLayout.padding.right, Is.EqualTo(10));
            Assert.That(sectionLayout.padding.top, Is.EqualTo(10));
            Assert.That(sectionLayout.padding.bottom, Is.EqualTo(10));

            Assert.That(titleLabel.text, Is.EqualTo("出题"));
            Assert.That(titleLabel.alignment, Is.EqualTo(TMPro.TextAlignmentOptions.Left));
            Assert.That(puzzleTitle.GetComponent<Button>(), Is.Not.Null);
            var collapseIcon = puzzleTitle.transform.Find("CollapseIcon");
            Assert.That(collapseIcon, Is.Not.Null);
            Assert.That(collapseIcon.GetComponent<TMPro.TextMeshProUGUI>(), Is.Null);
            Assert.That(collapseIcon.GetComponent<Image>(), Is.Not.Null);
            Assert.That(collapseIcon.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(puzzleSection.transform.GetChild(1).name, Is.EqualTo("TitleDivider"));
            Assert.That(puzzleSection.transform.Find("TitleDivider").GetComponent<Image>().color, Is.EqualTo(new Color(1f, 1f, 1f, 0.08f)));

            var expandedSprite = collapseIcon.GetComponent<Image>().sprite;
            puzzleTitle.GetComponent<Button>().onClick.Invoke();
            Assert.That(titleLabel.text, Is.EqualTo("出题"));
            Assert.That(collapseIcon.GetComponent<Image>().sprite, Is.Not.SameAs(expandedSprite));
            Assert.That(puzzleBody.activeSelf, Is.False);
        }

        [Test]
        public void PuzzleUI_AppliesStandardLabelAndContentColumns()
        {
            root = new GameObject("PuzzleLayoutRoot");
            var section = new GameObject("PuzzleSection", typeof(RectTransform), typeof(VerticalLayoutGroup));
            section.transform.SetParent(root.transform, false);
            section.AddComponent<Puzzles.PuzzleUI>();

            var modeRow = CreatePuzzleRow(section.transform, "ModeRow", "模式");
            CreateButton(modeRow.transform, "BtnStudyMode", "学习");
            CreateButton(modeRow.transform, "BtnExamMode", "考试");

            var angleRow = CreatePuzzleRow(section.transform, "AngleRow", "∠1");
            var angleDropdown = CreateDropdown(angleRow.transform, "AngleDropdown");

            var pocketRow = CreatePuzzleRow(section.transform, "PocketRow", "袋口");
            var pocketDropdown = CreateDropdown(pocketRow.transform, "PocketDropdown");

            CreateStandalonePuzzleLabel(section.transform, "LblTgt", "子球 → 袋口");
            var tgtRow = CreatePuzzleRow(section.transform, "TgtRow", "");
            CreateButton(tgtRow.transform, "BtnTgtN", "近");
            CreateButton(tgtRow.transform, "BtnTgtM", "中");
            CreateButton(tgtRow.transform, "BtnTgtF", "远");

            CreateStandalonePuzzleLabel(section.transform, "LblCue", "主球 → 子球");
            var cueRow = CreatePuzzleRow(section.transform, "CueRow", "");
            CreateButton(cueRow.transform, "BtnCueN", "近");
            CreateButton(cueRow.transform, "BtnCueM", "中");
            CreateButton(cueRow.transform, "BtnCueF", "远");

            var actionRow = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actionRow.transform.SetParent(section.transform, false);
            CreateButton(actionRow.transform, "BtnGenerate", "出题");
            CreateButton(actionRow.transform, "BtnRandom", "随机");

            var ui = section.GetComponent<Puzzles.PuzzleUI>();
            ui.angleDropdown = angleDropdown;
            ui.pocketDropdown = pocketDropdown;
            ui.btnStudyMode = modeRow.transform.Find("BtnStudyMode").GetComponent<Button>();
            ui.btnExamMode = modeRow.transform.Find("BtnExamMode").GetComponent<Button>();
            ui.btnTargetNear = tgtRow.transform.Find("BtnTgtN").GetComponent<Button>();
            ui.btnTargetMid = tgtRow.transform.Find("BtnTgtM").GetComponent<Button>();
            ui.btnTargetFar = tgtRow.transform.Find("BtnTgtF").GetComponent<Button>();
            ui.btnCueNear = cueRow.transform.Find("BtnCueN").GetComponent<Button>();
            ui.btnCueMid = cueRow.transform.Find("BtnCueM").GetComponent<Button>();
            ui.btnCueFar = cueRow.transform.Find("BtnCueF").GetComponent<Button>();
            ui.btnGenerate = actionRow.transform.Find("BtnGenerate").GetComponent<Button>();
            ui.btnRandom = actionRow.transform.Find("BtnRandom").GetComponent<Button>();

            InvokeStart(ui);

            AssertPuzzleLabelColumn(modeRow.transform.Find("Lbl"));
            AssertPuzzleLabelColumn(angleRow.transform.Find("Lbl"));
            AssertPuzzleLabelColumn(pocketRow.transform.Find("Lbl"));
            AssertPuzzleLabelColumn(tgtRow.transform.Find("Lbl"));
            AssertPuzzleLabelColumn(cueRow.transform.Find("Lbl"));

            AssertContentWidth(modeRow.transform.Find("Content"), 232f);
            AssertContentWidth(angleRow.transform.Find("Content"), 232f);
            AssertContentWidth(pocketRow.transform.Find("Content"), 232f);
            AssertContentWidth(tgtRow.transform.Find("Content"), 232f);
            AssertContentWidth(cueRow.transform.Find("Content"), 232f);
            AssertContentWidth(actionRow.transform, 232f);

            Assert.That(modeRow.transform.Find("Content/BtnStudyMode"), Is.Not.Null);
            Assert.That(angleRow.transform.Find("Content/AngleButtonGrid"), Is.Not.Null);
            Assert.That(pocketRow.transform.Find("Content/PocketMap"), Is.Not.Null);
            Assert.That(pocketRow.transform.Find("Content/PocketMap"), Is.Not.Null);
            Assert.That(pocketRow.transform.Find("Content/PocketRandom"), Is.Not.Null);
            Assert.That(tgtRow.transform.Find("Content/BtnTgtM"), Is.Not.Null);

            var angleGrid = angleRow.transform.Find("Content/AngleButtonGrid").GetComponent<GridLayoutGroup>();
            Assert.That(angleGrid.cellSize.x, Is.EqualTo(55f).Within(0.01f));
            Assert.That(angleGrid.spacing.x, Is.EqualTo(4f).Within(0.01f));
            Assert.That(4f * angleGrid.cellSize.x + 3f * angleGrid.spacing.x, Is.EqualTo(232f).Within(0.01f));

            Assert.That(actionRow.transform.Find("BtnRandom").gameObject.activeSelf, Is.False);
            Assert.That(FirstActiveChild(modeRow.transform.Find("Content")).name, Is.EqualTo("BtnStudyMode"));
            Assert.That(FirstActiveChild(pocketRow.transform.Find("Content")).name, Is.EqualTo("PocketMap"));

            Assert.That(pocketDropdown.value, Is.EqualTo(3));
            AssertPuzzleButtonRounded(modeRow.transform.Find("Content/BtnStudyMode"));
            AssertPuzzleButtonRounded(angleGrid.transform.GetChild(0));
            AssertPuzzleButtonRounded(actionRow.transform.Find("BtnGenerate"));

            var studyButton = modeRow.transform.Find("Content/BtnStudyMode").GetComponent<Button>();
            var examButton = modeRow.transform.Find("Content/BtnExamMode").GetComponent<Button>();
            Assert.That(studyButton.GetComponent<Image>().color, Is.EqualTo(examButton.GetComponent<Image>().color));
            AssertSelectedButtonText(studyButton, true);
            AssertSelectedButtonText(examButton, false);

            var anyAngleButton = angleGrid.transform.GetChild(0).GetComponent<Button>();
            var zeroAngleButton = angleGrid.transform.GetChild(1).GetComponent<Button>();
            Assert.That(anyAngleButton.GetComponent<Image>().color, Is.EqualTo(zeroAngleButton.GetComponent<Image>().color));
            AssertSelectedButtonText(anyAngleButton, true);
            AssertSelectedButtonText(zeroAngleButton, false);

            AssertSelectedButtonText(tgtRow.transform.Find("Content/BtnTgtM").GetComponent<Button>(), true);
            AssertSelectedButtonText(tgtRow.transform.Find("Content/BtnTgtN").GetComponent<Button>(), false);
        }

        [Test]
        public void EstimatedAimLineRenderer_StartsAtNearAimHitAndExtendsThroughTargetToRail()
        {
            root = new GameObject("EstimatedAimLineRoot");
            CreateTestVisibility(root);
            var table = root.AddComponent<TableController>();
            table.playfieldHalfLength = 2f;
            table.playfieldHalfWidth = 1f;
            table.ballRadius = 0.0286f;
            var renderer = root.AddComponent<EstimatedAimLineRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                new Vector3(0f, table.ballRadius, 0f),
                new Vector3(1f, table.ballRadius, 0f),
                Vector3.right,
                table.ballRadius,
                table);

            LineRenderer line = FindLine(root.transform, "LR_EstimatedAimLine");
            Assert.That(line.enabled, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(3));
            AssertVector(new Vector3(1f - table.ballRadius, table.ballRadius, 0f), line.GetPosition(0), "near hit");
            AssertVector(new Vector3(1f, table.ballRadius, 0f), line.GetPosition(1), "target center");
            AssertVector(new Vector3(2f, table.ballRadius, 0f), line.GetPosition(2), "rail");
        }

        [Test]
        public void EstimatedAimLineRenderer_RespectsIndependentToggle()
        {
            root = new GameObject("EstimatedAimLineToggleRoot");
            var visibility = CreateTestVisibility(root);
            visibility.SetLayerVisible(ReferenceVisualLayer.EstimatedAimLine, false);
            var table = root.AddComponent<TableController>();
            table.playfieldHalfLength = 2f;
            table.playfieldHalfWidth = 1f;
            table.ballRadius = 0.0286f;
            var renderer = root.AddComponent<EstimatedAimLineRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                new Vector3(0f, table.ballRadius, 0f),
                new Vector3(1f, table.ballRadius, 0f),
                Vector3.right,
                table.ballRadius,
                table);

            Assert.That(FindLine(root.transform, "LR_EstimatedAimLine").enabled, Is.False);
        }

        [Test]
        public void MirroredCueThroughLineRenderer_ChoosesSideThatBracketsPocketDirection()
        {
            root = new GameObject("MirroredCueThroughLineRoot");
            CreateTestVisibility(root);
            var table = root.AddComponent<TableController>();
            table.playfieldHalfLength = 2f;
            table.playfieldHalfWidth = 1f;
            table.ballRadius = 0.0286f;
            var renderer = root.AddComponent<MirroredCueThroughLineRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                new Vector3(0f, table.ballRadius, 0f),
                new Vector3(1f, table.ballRadius, 0f),
                new Vector3(1f, table.ballRadius, 1f),
                table.ballRadius,
                table);

            LineRenderer line = FindLine(root.transform, "LR_MirroredCueThrough");
            Assert.That(line.enabled, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(3));
            AssertVector(new Vector3(1f + table.ballRadius, table.ballRadius, 0f), line.GetPosition(0), "bracketing mirrored hit");
            AssertVector(new Vector3(1f, table.ballRadius, 0f), line.GetPosition(1), "target center");
            AssertVector(new Vector3(-2f, table.ballRadius, 0f), line.GetPosition(2), "rail");
        }

        [Test]
        public void MirroredCueThroughLineRenderer_RespectsIndependentToggle()
        {
            root = new GameObject("MirroredCueThroughLineToggleRoot");
            var visibility = CreateTestVisibility(root);
            visibility.SetLayerVisible(ReferenceVisualLayer.MirroredCueThroughTarget, false);
            var table = root.AddComponent<TableController>();
            table.playfieldHalfLength = 2f;
            table.playfieldHalfWidth = 1f;
            table.ballRadius = 0.0286f;
            var renderer = root.AddComponent<MirroredCueThroughLineRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                new Vector3(0f, table.ballRadius, 0f),
                new Vector3(1f, table.ballRadius, 0f),
                new Vector3(1f, table.ballRadius, 1f),
                table.ballRadius,
                table);

            Assert.That(FindLine(root.transform, "LR_MirroredCueThrough").enabled, Is.False);
        }

        [Test]
        public void MirroredCueThroughLineRenderer_UsesSameColorAsRedLine()
        {
            root = new GameObject("MirroredCueThroughLineStyleRoot");
            CreateTestVisibility(root);
            var renderer = root.AddComponent<MirroredCueThroughLineRenderer>();

            InvokeAwake(renderer);

            LineRenderer line = FindLine(root.transform, "LR_MirroredCueThrough");
            Assert.That(line.startColor.r, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(line.startColor.g, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(line.startColor.b, Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(line.sharedMaterial.color.r, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(line.sharedMaterial.color.g, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(line.sharedMaterial.color.b, Is.EqualTo(0.65f).Within(0.001f));
        }

        [Test]
        public void CueThroughTargetLineRenderer_UsesBrightReadableRedColorWithoutChangingWidth()
        {
            root = new GameObject("CueThroughTargetStyleRoot");
            CreateTestVisibility(root);
            var renderer = root.AddComponent<CueThroughTargetLineRenderer>();
            renderer.lineColor = new Color(0.35f, 0f, 0f, 1f);
            renderer.lineWidth = 0.0005f;

            InvokeAwake(renderer);

            LineRenderer line = FindLine(root.transform, "LR_CueThroughTarget");
            Assert.That(line.startColor.r, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(line.startColor.g, Is.GreaterThanOrEqualTo(0.1f));
            Assert.That(line.startColor.b, Is.GreaterThanOrEqualTo(0.6f));
            Assert.That(line.sharedMaterial.color.r, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(line.startWidth, Is.EqualTo(0.0005f).Within(0.0001f));
            Assert.That(line.endWidth, Is.EqualTo(line.startWidth).Within(0.0001f));
        }

        [Test]
        public void LineWidthCompensator_ScalesDashTextureWithCameraDistance()
        {
            root = new GameObject("DashScaleRoot");
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(root.transform, false);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -5f);
            cameraGo.AddComponent<Camera>();

            var lineGo = new GameObject("LineHost");
            lineGo.transform.SetParent(root.transform, false);
            var line = lineGo.AddComponent<LineRenderer>();
            line.startWidth = 0.0005f;
            line.endWidth = 0.0005f;
            line.textureScale = Vector2.one * 66.6667f;

            var compensator = lineGo.AddComponent<LineWidthCompensator>();
            compensator.referenceDistance = 2.5f;

            InvokeStart(compensator);
            InvokeLateUpdate(compensator);

            Assert.That(line.startWidth, Is.EqualTo(0.001f).Within(0.0001f));
            Assert.That(line.textureScale.x, Is.EqualTo(33.3333f).Within(0.01f));
        }

        [Test]
        public void LineWidthCompensator_ScalesDashTextureWithOrthographicZoom()
        {
            root = new GameObject("OrthographicDashScaleRoot");
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(root.transform, false);
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.5f;

            var lineGo = new GameObject("LineHost");
            lineGo.transform.SetParent(root.transform, false);
            var line = lineGo.AddComponent<LineRenderer>();
            line.startWidth = 0.0005f;
            line.endWidth = 0.0005f;
            line.textureScale = Vector2.one * 66.6667f;

            var compensator = lineGo.AddComponent<LineWidthCompensator>();
            compensator.referenceOrthographicSize = 1.25f;

            InvokeStart(compensator);
            InvokeLateUpdate(compensator);

            Assert.That(line.startWidth, Is.EqualTo(0.001f).Within(0.0001f));
            Assert.That(line.textureScale.x, Is.EqualTo(33.3333f).Within(0.01f));
        }

        static ReferenceLineVisibility CreateTestVisibility(GameObject host)
        {
            var visibility = host.AddComponent<ReferenceLineVisibility>();
            visibility.playerPrefsKeyPrefix = TestPrefsPrefix;
            visibility.ActivateAsCurrent();
            visibility.SetMasterVisible(true);
            foreach (ReferenceVisualLayer layer in System.Enum.GetValues(typeof(ReferenceVisualLayer)))
                visibility.SetLayerVisible(layer, true);
            visibility.SavePreferences();
            return visibility;
        }

        static System.Collections.Generic.List<ReferenceLineVisibility.LayerState> GetMutableLayers(
            ReferenceLineVisibility visibility)
        {
            var field = typeof(ReferenceLineVisibility).GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (System.Collections.Generic.List<ReferenceLineVisibility.LayerState>)field.GetValue(visibility);
        }

        static LineRenderer FindLine(Transform root, string childName)
        {
            var child = root.Find(childName);
            Assert.That(child, Is.Not.Null);
            var line = child.GetComponent<LineRenderer>();
            Assert.That(line, Is.Not.Null);
            return line;
        }

        static GameObject CreatePuzzleRow(Transform parent, string name, string labelText)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var label = new GameObject("Lbl", typeof(RectTransform), typeof(LayoutElement));
            label.transform.SetParent(row.transform, false);
            var text = label.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = labelText;
            text.color = Color.white;
            return row;
        }

        static void CreateStandalonePuzzleLabel(Transform parent, string name, string text)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            label.transform.SetParent(parent, false);
            var tmp = label.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
        }

        static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var text = new GameObject("Text", typeof(RectTransform));
            text.transform.SetParent(go.transform, false);
            text.AddComponent<TMPro.TextMeshProUGUI>().text = label;
            return go.GetComponent<Button>();
        }

        static TMPro.TMP_Dropdown CreateDropdown(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TMP_Dropdown), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            return go.GetComponent<TMPro.TMP_Dropdown>();
        }

        static void AssertPuzzleLabelColumn(Transform label)
        {
            Assert.That(label, Is.Not.Null);
            var layout = label.GetComponent<LayoutElement>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.preferredWidth, Is.EqualTo(70f).Within(0.01f));
            Assert.That(layout.flexibleWidth, Is.EqualTo(0f).Within(0.01f));
        }

        static void AssertContentWidth(Transform content, float expectedWidth)
        {
            Assert.That(content, Is.Not.Null);
            var layout = content.GetComponent<LayoutElement>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.preferredWidth, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(layout.flexibleWidth, Is.EqualTo(0f).Within(0.01f));
        }

        static void AssertPuzzleButtonRounded(Transform buttonTransform)
        {
            Assert.That(buttonTransform, Is.Not.Null);
            var image = buttonTransform.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
        }

        static void AssertSelectedButtonText(Button button, bool selected)
        {
            Assert.That(button, Is.Not.Null);
            var text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            Assert.That(text, Is.Not.Null);
            Assert.That(text.color, Is.EqualTo(selected ? new Color(0.2f, 0.9f, 0.45f, 1f) : Color.white));
            Assert.That(text.fontStyle, Is.EqualTo(selected ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal));
        }

        static Transform FirstActiveChild(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf)
                    return child;
            }
            return null;
        }

        static void AssertVector(Vector3 expected, Vector3 actual, string label)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-4f), label + ".x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-4f), label + ".y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(1e-4f), label + ".z");
        }

        static void InvokeAwake(MonoBehaviour behaviour)
        {
            var method = behaviour.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(behaviour, null);
        }

        static void InvokeStart(MonoBehaviour behaviour)
        {
            var method = behaviour.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(behaviour, null);
        }

        static void InvokeLateUpdate(MonoBehaviour behaviour)
        {
            var method = behaviour.GetType().GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(behaviour, null);
        }

        static void ClearTestPrefs()
        {
            PlayerPrefs.DeleteKey(TestPrefsPrefix + "Master");
            foreach (ReferenceVisualLayer layer in System.Enum.GetValues(typeof(ReferenceVisualLayer)))
                PlayerPrefs.DeleteKey(TestPrefsPrefix + "Layer." + layer);
            PlayerPrefs.Save();
        }
    }
}
