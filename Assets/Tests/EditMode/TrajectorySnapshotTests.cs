using NUnit.Framework;
using UnityEngine;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.Visualization;

namespace PoolAimTrainer.Tests
{
    public class TrajectorySnapshotTests
    {
        const float R = 0.0286f;

        [Test]
        public void SampleInterior_ReturnsEvenlySpacedIntermediatePoints()
        {
            Vector3[] path =
            {
                new Vector3(0f, R, 0f),
                new Vector3(1f, R, 0f),
            };

            Vector3[] samples = TrajectorySnapshotSampler.SampleInterior(path, 3);

            Assert.AreEqual(3, samples.Length);
            AssertVector(new Vector3(0.25f, R, 0f), samples[0], "first");
            AssertVector(new Vector3(0.50f, R, 0f), samples[1], "second");
            AssertVector(new Vector3(0.75f, R, 0f), samples[2], "third");
        }

        [Test]
        public void SampleInteriorAvoiding_StaysAwayFromEndpointsAndKeyframes()
        {
            Vector3[] path =
            {
                new Vector3(0f, R, 0f),
                new Vector3(1f, R, 0f),
            };
            Vector3[] avoid =
            {
                new Vector3(0.5f, R, 0f),
            };

            Vector3[] samples = TrajectorySnapshotSampler.SampleInteriorAvoiding(path, 3, 0.2f, avoid);

            Assert.AreEqual(2, samples.Length,
                "When there is not enough safe space, the sampler should drop snapshots instead of overlapping markers.");
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.GreaterOrEqual(samples[i].x, 0.2f, "snapshot must stay clear of path start");
                Assert.LessOrEqual(samples[i].x, 0.8f, "snapshot must stay clear of path end");
                Assert.GreaterOrEqual(Mathf.Abs(samples[i].x - 0.5f), 0.2f,
                    "snapshot must stay clear of collision/keyframe marker");
            }
        }

        [Test]
        public void Run_DirectHit_RecordsCollisionSnapshotAndContactPoint()
        {
            var tableGo = new GameObject("Table");
            var simGo = new GameObject("Simulator");
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 2f;
                table.playfieldHalfWidth = 1f;
                table.ballRadius = R;

                var sim = simGo.AddComponent<ShotSimulator>();
                Vector3 cue = new Vector3(0f, R, 0f);
                Vector3 target = new Vector3(0.5f, R, 0f);
                Vector3 aimDir = Vector3.right;

                SimulationResult result = sim.Run(cue, target, aimDir, R, table);

                Assert.IsTrue(result.hasBallCollision);
                AssertVector(new Vector3(0.5f - 2f * R, R, 0f),
                    result.collisionCueBallCenter, "cue collision snapshot");
                AssertVector(new Vector3(0.5f - R, R, 0f),
                    result.collisionContactPoint, "contact point");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                Object.DestroyImmediate(simGo);
            }
        }

        [Test]
        public void Renderer_Show_DirectHitOnlyCreatesCollisionMarkers()
        {
            var rendererGo = new GameObject("Renderer");
            try
            {
                var renderer = rendererGo.AddComponent<TrajectorySnapshotRenderer>();
                var result = new SimulationResult
                {
                    state = TargetBallEndState.OnTable,
                    hasBallCollision = true,
                    cueBallTrajectory = new[]
                    {
                        new Vector3(0f, R, 0f),
                        new Vector3(0.5f - 2f * R, R, 0f),
                    },
                    targetBallTrajectory = new[]
                    {
                        new Vector3(0.5f, R, 0f),
                        new Vector3(1f, R, 0f),
                    },
                    collisionCueBallCenter = new Vector3(0.5f - 2f * R, R, 0f),
                    collisionTargetBallCenter = new Vector3(0.5f, R, 0f),
                    collisionContactPoint = new Vector3(0.5f - R, R, 0f),
                };

                renderer.Show(result, R);

                AssertMissingOrInactive(rendererGo.transform.Find("CueSnapshot_0"), "cue path snapshot");
                AssertMissingOrInactive(rendererGo.transform.Find("TargetSnapshot_0"), "target path snapshot");
                Assert.IsNotNull(rendererGo.transform.Find("CollisionCueSnapshot"));
                Assert.IsNotNull(rendererGo.transform.Find("CollisionContactPoint"));
            }
            finally
            {
                Object.DestroyImmediate(rendererGo);
            }
        }

        static void AssertVector(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 1e-4f, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 1e-4f, label + ".z");
        }

        static void AssertMissingOrInactive(Transform marker, string label)
        {
            if (marker == null) return;
            Assert.IsFalse(marker.gameObject.activeSelf, label + " should not be visible");
        }
    }
}
