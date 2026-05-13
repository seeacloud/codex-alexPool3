using System.Collections.Generic;
using NUnit.Framework;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class AngleLabelLayoutTests
    {
        [Test]
        public void PlaceAvoidingOverlap_OffsetsSecondLabelFromSamePreferredPoint()
        {
            var occupied = new List<Rect>();
            Vector2 preferred = new Vector2(200f, 100f);
            Vector2 size = new Vector2(56f, 18f);
            Vector2 direction = Vector2.right;

            Vector2 first = AngleLabelLayout.PlaceAvoidingOverlap(
                preferred, size, direction, occupied);
            Vector2 second = AngleLabelLayout.PlaceAvoidingOverlap(
                preferred, size, direction, occupied);

            Assert.That(first, Is.EqualTo(preferred));
            Assert.That(second.x, Is.EqualTo(preferred.x).Within(0.01f));
            Assert.That(Mathf.Abs(second.y - preferred.y), Is.GreaterThan(18f));
            Assert.That(occupied[0].Overlaps(occupied[1]), Is.False);
        }
    }
}
