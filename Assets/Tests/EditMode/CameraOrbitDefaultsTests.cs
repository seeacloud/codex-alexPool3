using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.Interaction;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class CameraOrbitDefaultsTests
    {
        [Test]
        public void DefaultsAllowLowerPitchAndOnePointFiveTimesCloserZoom()
        {
            var go = new GameObject("CameraOrbitDefaultsTest");
            try
            {
                var orbit = go.AddComponent<CameraOrbit>();

                var minPitchField = typeof(CameraOrbit).GetField(
                    "minPitchDegrees",
                    BindingFlags.Instance | BindingFlags.Public);

                Assert.That(minPitchField, Is.Not.Null);
                Assert.That((float)minPitchField.GetValue(orbit), Is.EqualTo(2f).Within(0.001f));
                Assert.That(orbit.minDistance, Is.EqualTo(0.8f / 1.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TopDownUsesOrthographicProjectionAndRestoresPerspective()
        {
            var go = new GameObject("CameraOrbitProjectionTest");
            try
            {
                var camera = go.AddComponent<Camera>();
                var orbit = go.AddComponent<CameraOrbit>();

                camera.orthographic = false;

                orbit.SetTopDown(true);

                Assert.That(camera.orthographic, Is.True);

                orbit.SetTopDown(false);

                Assert.That(camera.orthographic, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TopDownOrthographicSizeMatchesPerspectiveZoomDistance()
        {
            var go = new GameObject("CameraOrbitOrthographicSizeTest");
            try
            {
                var camera = go.AddComponent<Camera>();
                var orbit = go.AddComponent<CameraOrbit>();
                camera.fieldOfView = 60f;
                camera.orthographicSize = 9f;
                orbit.distance = 2f;

                orbit.SetTopDown(true);

                float expectedSize = 2f * Mathf.Tan(30f * Mathf.Deg2Rad);
                Assert.That(camera.orthographicSize, Is.EqualTo(expectedSize).Within(0.001f));

                orbit.SetTopDown(false);

                Assert.That(camera.orthographicSize, Is.EqualTo(9f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
