using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class AimVsTargetArcRendererTests
    {
        [TearDown]
        public void TearDown()
        {
            var canvas = GameObject.Find("CutAngleLabelCanvas");
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        [Test]
        public void Show_LabelsAngleNameAndValue()
        {
            var cue = Vector3.zero;
            var target = Vector3.right;
            var aim = Quaternion.AngleAxis(8f, Vector3.up) * Vector3.right;

            var go = new GameObject("AimVsTargetArcRendererTest");
            try
            {
                var renderer = go.AddComponent<AimVsTargetArcRenderer>();

                renderer.Show(cue, target, aim);

                Assert.That(GetLabelText(renderer), Is.EqualTo("∠2 8°"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static string GetLabelText(AimVsTargetArcRenderer renderer)
        {
            var field = typeof(AimVsTargetArcRenderer).GetField("label", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            var label = field.GetValue(renderer);
            Assert.That(label, Is.Not.Null);

            var textProperty = label.GetType().GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(label);
        }
    }
}
