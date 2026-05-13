using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.Core;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class TargetLineAngleArcRendererTests
    {
        const string TestPrefsPrefix = "PoolAimTrainer.Tests.TargetLineAngleArc.";

        GameObject root;

        [SetUp]
        public void SetUp()
        {
            ClearTestPrefs();
            root = new GameObject("TargetLineAngleArcRendererTest");
            var visibility = root.AddComponent<ReferenceLineVisibility>();
            visibility.playerPrefsKeyPrefix = TestPrefsPrefix;
            visibility.ActivateAsCurrent();
            visibility.SetMasterVisible(true);
            foreach (ReferenceVisualLayer layer in System.Enum.GetValues(typeof(ReferenceVisualLayer)))
                visibility.SetLayerVisible(layer, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);

            var canvas = GameObject.Find("CutAngleLabelCanvas");
            if (canvas != null)
                Object.DestroyImmediate(canvas);

            ClearTestPrefs();
        }

        [Test]
        public void Show_LabelsMagentaOrangeAndOrangeRedAngles()
        {
            var renderer = root.AddComponent<TargetLineAngleArcRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                Vector3.zero,
                Quaternion.AngleAxis(30f, Vector3.up) * Vector3.right,
                true,
                Vector3.right,
                Quaternion.AngleAxis(-10f, Vector3.up) * Vector3.right);

            Assert.That(FindLine("EstimatedToTargetPathAngleArc").enabled, Is.True);
            Assert.That(FindLine("TargetPathToCueThroughAngleArc").enabled, Is.True);
            Assert.That(GetLabelText(renderer, "estimatedToTargetPathLabel"), Is.EqualTo("∠3 30°"));
            Assert.That(GetLabelText(renderer, "targetPathToCueThroughLabel"), Is.EqualTo("∠4 10°"));
        }

        [Test]
        public void Show_WhenNoEstimatedDirection_HidesOnlyMagentaOrangeAngle()
        {
            var renderer = root.AddComponent<TargetLineAngleArcRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                Vector3.zero,
                Vector3.zero,
                false,
                Vector3.right,
                Quaternion.AngleAxis(12f, Vector3.up) * Vector3.right);

            Assert.That(FindLine("EstimatedToTargetPathAngleArc").enabled, Is.False);
            Assert.That(FindLine("TargetPathToCueThroughAngleArc").enabled, Is.True);
            Assert.That(GetLabelText(renderer, "targetPathToCueThroughLabel"), Is.EqualTo("∠4 12°"));
        }

        [Test]
        public void Show_RespectsIndependentAngleToggles()
        {
            var visibility = root.GetComponent<ReferenceLineVisibility>();
            visibility.SetLayerVisible(ReferenceVisualLayer.EstimatedAimToTargetPathAngle, false);

            var renderer = root.AddComponent<TargetLineAngleArcRenderer>();
            InvokeAwake(renderer);

            renderer.Show(
                Vector3.zero,
                Quaternion.AngleAxis(25f, Vector3.up) * Vector3.right,
                true,
                Vector3.right,
                Quaternion.AngleAxis(-15f, Vector3.up) * Vector3.right);

            Assert.That(FindLine("EstimatedToTargetPathAngleArc").enabled, Is.False);
            Assert.That(FindLine("TargetPathToCueThroughAngleArc").enabled, Is.True);
            Assert.That(GetLabelText(renderer, "targetPathToCueThroughLabel"), Is.EqualTo("∠4 15°"));
        }

        LineRenderer FindLine(string childName)
        {
            var child = root.transform.Find(childName);
            Assert.That(child, Is.Not.Null);
            var line = child.GetComponent<LineRenderer>();
            Assert.That(line, Is.Not.Null);
            return line;
        }

        static string GetLabelText(TargetLineAngleArcRenderer renderer, string fieldName)
        {
            var field = typeof(TargetLineAngleArcRenderer).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            var label = field.GetValue(renderer);
            Assert.That(label, Is.Not.Null);

            var textProperty = label.GetType().GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(label);
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
