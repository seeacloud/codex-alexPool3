using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.UI;
using PoolAimTrainer.Visualization;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Tests.EditMode
{
    public class AimHudControllerRenderTextureTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void LateUpdate_CreatesHighResolutionSquareRenderTextureForHudImage()
        {
            var hud = CreateHud(new Vector2(320f, 240f));

            InvokeLateUpdate(hud.controller);

            var rt = hud.camera.targetTexture;
            Assert.That(rt, Is.Not.Null);
            Assert.That(rt.width, Is.EqualTo(640));
            Assert.That(rt.height, Is.EqualTo(640));
            Assert.That(rt.antiAliasing, Is.EqualTo(4));
            Assert.That(rt.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(hud.rawImage.texture, Is.SameAs(rt));
        }

        [Test]
        public void LateUpdate_RebuildsRenderTextureWhenHudImageSizeChanges()
        {
            var hud = CreateHud(new Vector2(240f, 240f));

            InvokeLateUpdate(hud.controller);
            var first = hud.camera.targetTexture;
            hud.rawImageRect.sizeDelta = new Vector2(360f, 300f);

            InvokeLateUpdate(hud.controller);

            var second = hud.camera.targetTexture;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.width, Is.EqualTo(720));
            Assert.That(second.height, Is.EqualTo(720));
            Assert.That(hud.rawImage.texture, Is.SameAs(second));
        }

        [Test]
        public void ShowIdealAnswerMarker_PlacesMarkerAtProjectedWorldPoint()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            var markerGo = new GameObject("IdealAnswerV", typeof(RectTransform));
            markerGo.transform.SetParent(hud.rawImageRect, false);
            var marker = markerGo.GetComponent<RectTransform>();
            hud.controller.idealAnswerVBar = marker;

            InvokeLateUpdate(hud.controller);

            hud.controller.ShowIdealAnswerMarker(Vector3.forward);

            Assert.That(marker.gameObject.activeSelf, Is.True);
            Assert.That(marker.anchoredPosition.x, Is.EqualTo(160f).Within(0.01f));
        }

        [Test]
        public void HudMarkers_UseConfiguredPixelWidth()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            hud.controller.hudLineWidthPx = 1.5f;

            var hGo = new GameObject("CrosshairH", typeof(RectTransform));
            hGo.transform.SetParent(hud.rawImageRect, false);
            var hBar = hGo.GetComponent<RectTransform>();
            hBar.sizeDelta = new Vector2(999f, 12f);

            var vGo = new GameObject("CrosshairV", typeof(RectTransform));
            vGo.transform.SetParent(hud.rawImageRect, false);
            var vBar = vGo.GetComponent<RectTransform>();
            vBar.sizeDelta = new Vector2(12f, 999f);

            var idealGo = new GameObject("IdealAnswerV", typeof(RectTransform));
            idealGo.transform.SetParent(hud.rawImageRect, false);
            var ideal = idealGo.GetComponent<RectTransform>();
            ideal.sizeDelta = new Vector2(12f, 999f);

            hud.controller.crosshairHBar = hBar;
            hud.controller.crosshairVBar = vBar;
            hud.controller.idealAnswerVBar = ideal;

            InvokePrivate(hud.controller, "UpdateCrosshair");
            InvokeLateUpdate(hud.controller);
            hud.controller.ShowIdealAnswerMarker(Vector3.forward);

            Assert.That(hBar.sizeDelta.y, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(vBar.sizeDelta.x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(ideal.sizeDelta.x, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void HudMarkers_ConvertConfiguredWidthFromScreenPixelsToCanvasUnits()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.scaleFactor = 2f;
            hud.rawImage.transform.SetParent(canvasGo.transform, false);
            hud.controller.hudLineWidthPx = 1f;

            var hGo = new GameObject("CrosshairH", typeof(RectTransform));
            hGo.transform.SetParent(hud.rawImageRect, false);
            var hBar = hGo.GetComponent<RectTransform>();
            hBar.sizeDelta = new Vector2(999f, 12f);

            var vGo = new GameObject("CrosshairV", typeof(RectTransform));
            vGo.transform.SetParent(hud.rawImageRect, false);
            var vBar = vGo.GetComponent<RectTransform>();
            vBar.sizeDelta = new Vector2(12f, 999f);

            hud.controller.crosshairHBar = hBar;
            hud.controller.crosshairVBar = vBar;

            InvokePrivate(hud.controller, "UpdateCrosshair");

            Assert.That(hBar.sizeDelta.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(vBar.sizeDelta.x, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void ResetAimSelectionToDefault_UpdatesOffsetLabelFromCurrentGhostBall()
        {
            var hud = CreateHud(new Vector2(320f, 240f));

            var table = root.AddComponent<TableController>();
            var pocketGo = new GameObject("Pocket");
            pocketGo.transform.SetParent(root.transform, false);
            pocketGo.transform.position = new Vector3(1f, 0f, 1.2f);
            var pocket = pocketGo.AddComponent<PocketMarker>();
            table.Pockets.Add(pocket);

            var aimManager = root.AddComponent<AimManager>();
            aimManager.table = table;
            aimManager.cueBall = hud.controller.cueBall;
            aimManager.targetBall = hud.controller.targetBall;
            aimManager.currentPocket = pocket;
            hud.controller.aimManager = aimManager;

            var label = new GameObject("OffsetLabel").AddComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(root.transform, false);
            label.text = "dx = +99.0 mm  R";
            hud.controller.offsetLabel = label;

            InvokeLateUpdate(hud.controller);

            hud.controller.ResetAimSelectionToDefault();

            Assert.That(label.text, Is.Not.EqualTo("dx = +99.0 mm  R"));
            Assert.That(label.text, Does.StartWith("dx = "));
            Assert.That(label.text, Does.Contain(" L"));
        }

        [Test]
        public void LateUpdate_CreatesGreenObjectBallCenterCrossAtHudCenter()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.scaleFactor = 2f;
            hud.rawImage.transform.SetParent(canvasGo.transform, false);

            InvokeLateUpdate(hud.controller);

            Transform marker = hud.rawImageRect.Find("ObjectBallCenterCross");
            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.gameObject.activeSelf, Is.True);

            var markerRt = marker.GetComponent<RectTransform>();
            Assert.That(markerRt.anchoredPosition.x, Is.EqualTo(160f).Within(0.001f));
            Assert.That(markerRt.anchoredPosition.y, Is.EqualTo(120f).Within(0.001f));

            var hBar = marker.Find("H").GetComponent<RectTransform>();
            var vBar = marker.Find("V").GetComponent<RectTransform>();
            Assert.That(hBar.sizeDelta.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(vBar.sizeDelta.y, Is.EqualTo(10f).Within(0.001f));

            var hImage = hBar.GetComponent<Image>();
            var vImage = vBar.GetComponent<Image>();
            Assert.That(hImage.color.g, Is.GreaterThan(0.8f));
            Assert.That(vImage.color.g, Is.GreaterThan(0.8f));
        }

        [Test]
        public void LateUpdate_CreatesClockFaceOverlayAroundObjectBallInHud()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            hud.controller.ballDiameter = 0.0572f;
            hud.controller.orthoSize = 0.08f;
            hud.controller.fitStrikeAreaToHudWindow = false;

            InvokeLateUpdate(hud.controller);

            Transform marker = hud.rawImageRect.Find("ObjectBallCenterCross");
            Transform clock = marker.Find("ObjectBallClockFace");
            Assert.That(clock, Is.Not.Null);
            Assert.That(clock.gameObject.activeSelf, Is.True);
            Assert.That(clock.Find("Tick12"), Is.Not.Null);
            Assert.That(clock.Find("Label12").GetComponent<TMPro.TextMeshProUGUI>().text, Is.EqualTo("12"));
            Assert.That(clock.Find("Tick6"), Is.Not.Null);
            Assert.That(clock.Find("Label6").GetComponent<TMPro.TextMeshProUGUI>().text, Is.EqualTo("6"));

            float ballDiameterPx = 0.0572f / (2f * 0.08f) * 240f;
            float expectedRadius = ballDiameterPx * 0.5f * hud.controller.objectBallClockRadiusScale;
            Assert.That(clock.Find("Tick12").GetComponent<RectTransform>().anchoredPosition.y,
                Is.EqualTo(expectedRadius).Within(0.01f));
            Assert.That(clock.Find("Label6").GetComponent<RectTransform>().anchoredPosition.y,
                Is.EqualTo(-expectedRadius - hud.controller.objectBallClockLabelOffsetPx).Within(0.01f));
        }

        [Test]
        public void LateUpdate_HidesClockFaceOverlayWhenDisabled()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            hud.controller.showObjectBallClockFace = false;

            InvokeLateUpdate(hud.controller);

            Transform marker = hud.rawImageRect.Find("ObjectBallCenterCross");
            Transform clock = marker.Find("ObjectBallClockFace");
            Assert.That(clock, Is.Not.Null);
            Assert.That(clock.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Defaults_CompressHudVertically()
        {
            var hud = CreateHud(new Vector2(320f, 190f));

            Assert.That(hud.controller.sizeSmall.y, Is.EqualTo(330f).Within(0.001f));
            Assert.That(hud.controller.sizeLarge.y, Is.EqualTo(540f).Within(0.001f));
            Assert.That(hud.controller.orthoSize, Is.EqualTo(0.08f).Within(0.001f));
        }

        [Test]
        public void LateUpdate_ZoomsStrikeAreaToFillHudWindow()
        {
            var hud = CreateHud(new Vector2(480f, 300f));
            hud.controller.orthoSize = 0.08f;
            hud.controller.ballDiameter = 0.0572f;
            hud.controller.crosshairVerticalExtraMeters = 0.010f;
            hud.controller.strikeAreaViewportFill = 0.8f;

            InvokeLateUpdate(hud.controller);

            float expectedOrthoSize = (0.0572f + 0.010f) / (2f * 0.8f);
            Assert.That(hud.camera.orthographicSize, Is.EqualTo(expectedOrthoSize).Within(0.0001f));
        }

        [Test]
        public void LateUpdate_DisablesLegacySquareAspectAndAppliesCompressedHeight()
        {
            var hud = CreateHud(new Vector2(320f, 320f));
            var aspect = hud.rawImage.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            var layout = hud.rawImage.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 320f;

            InvokeLateUpdate(hud.controller);

            Assert.That(aspect.enabled, Is.False);
            Assert.That(layout.preferredHeight, Is.EqualTo(190f).Within(0.001f));
        }

        [Test]
        public void Defaults_HideWhiteRedGreenAndOrangeSceneReferenceLinesInHud()
        {
            var hud = CreateHud(new Vector2(320f, 240f));

            Assert.That(hud.controller.hideSelectedReferenceLinesInHud, Is.True);
            Assert.That(hud.controller.hideNonBallSceneRenderersInHud, Is.True);
            Assert.That(hud.controller.hideTableInHud, Is.True);
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.CueToGhost));
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.CueThroughTarget));
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.MirroredCueThroughTarget));
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.ObjectToPocket));
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.TargetBallPath));
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.EstimatedAimLine));
            Assert.That(hud.controller.hudHiddenReferenceLayers, Does.Contain(ReferenceVisualLayer.TargetPathToCueThroughAngle));
        }

        [Test]
        public void HideRenderersForHudCamera_HidesNonBallSceneRenderersButKeepsBallsAndHudUiCrosshairs()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            var referenceLine = new GameObject("LR_EstimatedAimLine", typeof(LineRenderer));
            referenceLine.transform.SetParent(root.transform, false);
            var lineRenderer = referenceLine.GetComponent<LineRenderer>();

            var tableGo = new GameObject("TableMesh", typeof(MeshRenderer));
            tableGo.transform.SetParent(root.transform, false);
            var tableRenderer = tableGo.GetComponent<MeshRenderer>();

            var cueRenderer = hud.controller.cueBall.gameObject.AddComponent<MeshRenderer>();
            var targetRenderer = hud.controller.targetBall.gameObject.AddComponent<MeshRenderer>();

            var crosshairGo = new GameObject("CrosshairH", typeof(RectTransform), typeof(Image));
            crosshairGo.transform.SetParent(hud.rawImageRect, false);
            crosshairGo.SetActive(true);
            hud.controller.crosshairHBar = crosshairGo.GetComponent<RectTransform>();
            var crosshairImage = crosshairGo.GetComponent<Image>();

            InvokePrivate(hud.controller, "HideRenderersForHudCamera");

            Assert.That(lineRenderer.enabled, Is.False);
            Assert.That(tableRenderer.enabled, Is.False);
            Assert.That(cueRenderer.enabled, Is.True);
            Assert.That(targetRenderer.enabled, Is.True);
            Assert.That(crosshairGo.activeSelf, Is.True);
            Assert.That(crosshairImage.enabled, Is.True);

            InvokePrivate(hud.controller, "RestoreHudHiddenRenderers");

            Assert.That(lineRenderer.enabled, Is.True);
            Assert.That(tableRenderer.enabled, Is.True);
        }

        [Test]
        public void HideRenderersForHudCamera_KeepsGhostBallProjectionVisible()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            var aimManager = root.AddComponent<AimManager>();
            var ghostRenderer = root.AddComponent<GhostBallRenderer>();
            var ghostPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghostPrefab.name = "GhostProjection";
            ghostRenderer.ghostPrefab = ghostPrefab;
            ghostRenderer.Show(Vector3.forward);
            aimManager.ghostRenderer = ghostRenderer;
            hud.controller.aimManager = aimManager;
            var ghostInstanceRenderer = ghostRenderer.CurrentInstance.GetComponent<Renderer>();

            var tableGo = new GameObject("TableMesh", typeof(MeshRenderer));
            tableGo.transform.SetParent(root.transform, false);
            var tableRenderer = tableGo.GetComponent<MeshRenderer>();

            InvokePrivate(hud.controller, "HideRenderersForHudCamera");

            Assert.That(tableRenderer.enabled, Is.False);
            Assert.That(ghostInstanceRenderer.enabled, Is.True);

            InvokePrivate(hud.controller, "RestoreHudHiddenRenderers");
            Object.DestroyImmediate(ghostPrefab);
        }

        [Test]
        public void HideRenderersForHudCamera_KeepsCollisionCueSnapshotAndContactPointVisible()
        {
            var hud = CreateHud(new Vector2(320f, 240f));
            var aimManager = root.AddComponent<AimManager>();
            var snapshotRenderer = root.AddComponent<TrajectorySnapshotRenderer>();
            snapshotRenderer.snapshotPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            snapshotRenderer.snapshotPrefab.name = "SnapshotPrefab";
            snapshotRenderer.contactMarkerPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            snapshotRenderer.contactMarkerPrefab.name = "ContactPrefab";
            snapshotRenderer.Show(new SimulationResult
            {
                hasBallCollision = true,
                collisionCueBallCenter = new Vector3(0.25f, 0.0286f, 0f),
                collisionContactPoint = new Vector3(0.3f, 0.0286f, 0f),
            }, 0.0286f);
            aimManager.snapshotRenderer = snapshotRenderer;
            hud.controller.aimManager = aimManager;

            var collisionCueRenderer = snapshotRenderer.CurrentCollisionCueSnapshot.GetComponent<Renderer>();
            var contactRenderer = snapshotRenderer.CurrentContactMarker.GetComponent<Renderer>();

            var referenceLine = new GameObject("LR_EstimatedAimLine", typeof(LineRenderer));
            referenceLine.transform.SetParent(root.transform, false);
            var lineRenderer = referenceLine.GetComponent<LineRenderer>();

            InvokePrivate(hud.controller, "HideRenderersForHudCamera");

            Assert.That(lineRenderer.enabled, Is.False);
            Assert.That(collisionCueRenderer.enabled, Is.True);
            Assert.That(contactRenderer.enabled, Is.True);

            InvokePrivate(hud.controller, "RestoreHudHiddenRenderers");
            Object.DestroyImmediate(snapshotRenderer.snapshotPrefab);
            Object.DestroyImmediate(snapshotRenderer.contactMarkerPrefab);
        }

        (AimHudController controller, Camera camera, RawImage rawImage, RectTransform rawImageRect) CreateHud(Vector2 rawSize)
        {
            root = new GameObject("AimHudRenderTextureTestRoot");

            var cameraGo = new GameObject("AimCamera", typeof(Camera));
            cameraGo.transform.SetParent(root.transform);
            var camera = cameraGo.GetComponent<Camera>();

            var rawImageGo = new GameObject("RawImage", typeof(RectTransform), typeof(RawImage));
            rawImageGo.transform.SetParent(root.transform);
            var rawImageRect = rawImageGo.GetComponent<RectTransform>();
            rawImageRect.sizeDelta = rawSize;
            var rawImage = rawImageGo.GetComponent<RawImage>();

            var cueGo = new GameObject("CueBall");
            cueGo.transform.SetParent(root.transform);
            cueGo.transform.position = Vector3.zero;
            var cue = cueGo.AddComponent<BallController>();

            var targetGo = new GameObject("TargetBall");
            targetGo.transform.SetParent(root.transform);
            targetGo.transform.position = Vector3.forward;
            var target = targetGo.AddComponent<BallController>();

            var controller = root.AddComponent<AimHudController>();
            controller.aimCamera = camera;
            controller.rawImage = rawImage;
            controller.rawImageRect = rawImageRect;
            controller.cueBall = cue;
            controller.targetBall = target;

            return (controller, camera, rawImage, rawImageRect);
        }

        static void InvokeLateUpdate(AimHudController controller)
        {
            typeof(AimHudController)
                .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
        }

        static void InvokePrivate(AimHudController controller, string methodName)
        {
            typeof(AimHudController)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
        }
    }
}
