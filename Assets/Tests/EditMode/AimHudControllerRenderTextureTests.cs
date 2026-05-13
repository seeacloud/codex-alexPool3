using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;
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
