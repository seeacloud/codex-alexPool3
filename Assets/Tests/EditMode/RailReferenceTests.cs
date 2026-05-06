using NUnit.Framework;
using UnityEngine;
using PoolAimTrainer.GeometryCore;

namespace PoolAimTrainer.Tests.EditMode
{
    public class RailReferenceTests
    {
        [Test]
        public void BottomRail_8Points_EvenlyDistributed()
        {
            var points = RailReference.GenerateRailPoints(
                railStart: new Vector3(0f, 0f, 0f),
                railEnd: new Vector3(2.44f, 0f, 0f),
                count: 8);
            Assert.That(points.Length, Is.EqualTo(8));
            float expectedStep = 2.44f / 9f;
            Assert.That(points[0].x, Is.EqualTo(expectedStep).Within(1e-4f));
            Assert.That(points[7].x, Is.EqualTo(expectedStep * 8f).Within(1e-4f));
        }

        [Test]
        public void ClosestPointIndex_MidpointSelectsMiddle()
        {
            var points = RailReference.GenerateRailPoints(
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), 5);
            int i = RailReference.FindClosestIndex(points, new Vector3(0.5f, 0f, 0f));
            Assert.That(i, Is.EqualTo(2));
        }
    }
}
