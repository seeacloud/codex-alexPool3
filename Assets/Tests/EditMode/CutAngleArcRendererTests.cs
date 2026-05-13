using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class CutAngleArcRendererTests
    {
        [TearDown]
        public void TearDown()
        {
            var canvas = GameObject.Find("CutAngleLabelCanvas");
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        [Test]
        public void ComputeRedGreenAngleDegrees_ReturnsCueThroughTargetToObjectPathAngle()
        {
            var target = Vector3.zero;
            var pocket = Vector3.right;
            var redDir = Quaternion.AngleAxis(12f, Vector3.up) * (pocket - target).normalized;
            var cue = target - redDir * 0.6f;

            float angle = CutAngleArcRenderer.ComputeRedGreenAngleDegrees(cue, target, pocket);

            Assert.That(angle, Is.EqualTo(12f).Within(0.01f));
        }

        [Test]
        public void Show_LabelsRedGreenAngle()
        {
            var target = Vector3.zero;
            var pocket = Vector3.right;
            var redDir = Quaternion.AngleAxis(12f, Vector3.up) * (pocket - target).normalized;
            var cue = target - redDir * 0.6f;

            var go = new GameObject("CutAngleArcRendererTest");
            try
            {
                var renderer = go.AddComponent<CutAngleArcRenderer>();

                renderer.Show(cue, target, pocket);

                Assert.That(GetLabelText(renderer), Is.EqualTo("∠1 12°"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static string GetLabelText(CutAngleArcRenderer renderer)
        {
            var field = typeof(CutAngleArcRenderer).GetField("label", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            var label = field.GetValue(renderer);
            Assert.That(label, Is.Not.Null);

            var textProperty = label.GetType().GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(label);
        }
    }
}
