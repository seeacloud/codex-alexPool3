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
            var hudLayout = hudImage.GetComponent<LayoutElement>();
            Assert.That(rightPanelRect.sizeDelta.x, Is.EqualTo(330f).Within(0.01f));
            Assert.That(rightPanelLayout.preferredWidth, Is.EqualTo(330f).Within(0.01f));
            Assert.That(hudLayout.preferredHeight, Is.EqualTo(298f).Within(0.01f));
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

        static void ClearTestPrefs()
        {
            PlayerPrefs.DeleteKey(TestPrefsPrefix + "Master");
            foreach (ReferenceVisualLayer layer in System.Enum.GetValues(typeof(ReferenceVisualLayer)))
                PlayerPrefs.DeleteKey(TestPrefsPrefix + "Layer." + layer);
            PlayerPrefs.Save();
        }
    }
}
