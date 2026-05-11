using NUnit.Framework;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;

namespace PoolAimTrainer.Tests
{
    public class PhysicsCollisionTests
    {
        const float R = 0.0286f; // ball radius
        const float PocketRadius = 0.06f;

        [Test]
        public void BallLineSegmentCollisionTime_HeadOnHitsSegment()
        {
            // Ball at (0, 0), moving in +X direction, cushion segment vertical at x=1 from z=-1 to z=1
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 dir = new Vector3(1, 0, 0);
            Vector3 p1 = new Vector3(1, 0, -1);
            Vector3 p2 = new Vector3(1, 0, 1);
            float t = ShotSimulator.BallLineSegmentCollisionTime(start, dir, p1, p2, R);
            Assert.AreEqual(1f - R, t, 1e-4f, "Head-on ball should hit cushion at x=1-R");
        }

        [Test]
        public void BallLineSegmentCollisionTime_MissesEndpoint()
        {
            // Ball moving parallel to cushion but beyond endpoint → no collision
            Vector3 start = new Vector3(0, 0, 5); // way past p2.z=1
            Vector3 dir = new Vector3(1, 0, 0);
            Vector3 p1 = new Vector3(1, 0, -1);
            Vector3 p2 = new Vector3(1, 0, 1);
            float t = ShotSimulator.BallLineSegmentCollisionTime(start, dir, p1, p2, R);
            Assert.AreEqual(float.MaxValue, t, "Contact point beyond segment endpoint — no hit");
        }

        [Test]
        public void RayCircleEntryTime_HeadOnEntersCircle()
        {
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 dir = new Vector3(1, 0, 0);
            Vector3 center = new Vector3(1, 0, 0);
            float radius = 0.1f;
            float t = ShotSimulator.RayCircleEntryTime(start, dir, center, radius);
            Assert.AreEqual(0.9f, t, 1e-4f);
        }

        [Test]
        public void RayCircleEntryTime_Miss()
        {
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 dir = new Vector3(1, 0, 0);
            Vector3 center = new Vector3(1, 0, 0.5f);
            float radius = 0.1f;
            float t = ShotSimulator.RayCircleEntryTime(start, dir, center, radius);
            Assert.AreEqual(float.MaxValue, t);
        }

        [Test]
        public void PocketAtCornerTR_DirectApproach_Pockets()
        {
            // Test 1: target near TR corner, aimed directly at TR pocket.
            // Should judge as pocketed (tPocket < tCushion).
            // Simulating the actual table layout:
            var cushions = new System.Collections.Generic.List<TableGeometry.CushionSegment>();
            // Right rail (simulated): from BR-like (+jawGap Z) to TR-like (-jawGap Z), at x=1.0
            cushions.Add(new TableGeometry.CushionSegment
            {
                p1 = new Vector3(1.0f, 0, -0.442f),
                p2 = new Vector3(1.0f, 0, 0.442f),
            });
            // Top rail segment (TM to TR): from x=jawGap to x=1-jawGap, at z~0.533
            cushions.Add(new TableGeometry.CushionSegment
            {
                p1 = new Vector3(0.08f, 0, 0.533f),
                p2 = new Vector3(0.92f, 0, 0.533f),
            });

            Vector3 pocket = new Vector3(1.0f, 0, 0.522f);
            Vector3 target = new Vector3(0.8f, 0, 0.35f);
            Vector3 dir = (pocket - target).normalized;

            // Check each cushion
            float tCushion = float.MaxValue;
            foreach (var seg in cushions)
            {
                float t = ShotSimulator.BallLineSegmentCollisionTime(target, dir, seg.p1, seg.p2, R);
                if (t < tCushion) tCushion = t;
            }

            float tPocket = ShotSimulator.RayCircleEntryTime(target, dir, pocket, 0.06f);

            Assert.Less(tPocket, tCushion, $"tPocket={tPocket:F4} should be less than tCushion={tCushion:F4} for direct TR approach");
        }

        [Test]
        public void RailSkimDoesNotPocket()
        {
            // Ball near long cushion, moving parallel to it, at an angle that "grazes"
            // past the pocket — per user's bug report earlier, this used to be
            // mis-judged as pocketed.
            var cushions = new System.Collections.Generic.List<TableGeometry.CushionSegment>();
            // Right rail at x=1.0
            cushions.Add(new TableGeometry.CushionSegment
            {
                p1 = new Vector3(1.0f, 0, -0.442f),
                p2 = new Vector3(1.0f, 0, 0.442f),
            });

            Vector3 pocket = new Vector3(1.0f, 0, 0.522f);

            // Target at x=0.97 (very close to right rail), moving almost in +Z direction.
            // Its swept cylinder should hit the right rail segment before reaching pocket.
            Vector3 target = new Vector3(0.97f, 0, 0f);
            Vector3 dir = new Vector3(0.1f, 0, 1f).normalized;

            float tCushion = float.MaxValue;
            foreach (var seg in cushions)
            {
                float t = ShotSimulator.BallLineSegmentCollisionTime(target, dir, seg.p1, seg.p2, R);
                if (t < tCushion) tCushion = t;
            }

            float tPocket = ShotSimulator.RayCircleEntryTime(target, dir, pocket, 0.06f);

            Assert.LessOrEqual(tCushion, tPocket,
                $"Rail-skim must be blocked: tCushion={tCushion:F4}, tPocket={tPocket:F4}");
        }

        [Test]
        public void CornerPocket_PottingPointIsMouthOpening_NotPocketCenter()
        {
            var tableGo = new GameObject("Table");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;

                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -0.522f)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                Vector3 target = new Vector3(0.75f, R, 0.43f);
                Vector3 pocketCenter = createdPockets[1].Position;
                Vector3 pottingPoint = TableGeometry.GetPottingPoint(table, 1, target, 0.08f);

                Assert.That(pottingPoint.x, Is.Not.EqualTo(pocketCenter.x).Within(1e-4f),
                    "Corner-pocket aiming should use the mouth opening, not the pocket center x.");
                Assert.That(pottingPoint.z, Is.Not.EqualTo(pocketCenter.z).Within(1e-4f),
                    "Corner-pocket aiming should use the mouth opening, not the pocket center z.");
                Assert.That(pottingPoint.x, Is.InRange(0.90f, 1.00f));
                Assert.That(pottingPoint.z, Is.InRange(0.42f, 0.50f));
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void CornerPocket_CrossingMouthAwayFromPocketCenter_IsPocketed()
        {
            var tableGo = new GameObject("Table");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;

                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -0.522f)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                var mouth = TableGeometry.BuildPocketMouth(table, 1, 0.08f);
                Vector3 mouthDir = mouth.p2 - mouth.p1;
                mouthDir.y = 0f;
                mouthDir.Normalize();
                Vector3 mouthCrossing = mouth.p1 + mouthDir * (R + 0.024f);
                mouthCrossing.y = R;
                Vector3 target = mouthCrossing - (mouth.center - mouthCrossing).normalized * 0.25f;
                target.y = R;
                Vector3 dir = mouthCrossing - target;
                dir.y = 0f;
                dir.Normalize();

                int pocketIdx;
                bool canReach = ShotSimulator.CanBallReachPocket(
                    target, dir, table, R, PocketRadius, 0.08f, out pocketIdx);

                Assert.IsTrue(canReach,
                    "Crossing the usable mouth opening should count as pocketed even when the ray misses the pocket-center circle.");
                Assert.AreEqual(1, pocketIdx, "The top-right pocket should be selected.");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void CornerPocket_CrossingMouthNearInnerJaw_IsPocketed()
        {
            var tableGo = new GameObject("Table");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;

                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -0.522f)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                var mouth = TableGeometry.BuildPocketMouth(table, 1, 0.08f);
                Vector3 mouthDir = mouth.p2 - mouth.p1;
                mouthDir.y = 0f;
                mouthDir.Normalize();

                Vector3 mouthCrossing = mouth.p1 + mouthDir * (R + 0.005f);
                mouthCrossing.y = R;
                Vector3 target = mouthCrossing - (mouth.center - mouthCrossing).normalized * 0.25f;
                target.y = R;
                Vector3 dir = mouthCrossing - target;
                dir.y = 0f;
                dir.Normalize();

                int pocketIdx;
                bool canReach = ShotSimulator.CanBallReachPocket(
                    target, dir, table, R, PocketRadius, 0.08f, out pocketIdx);

                Assert.IsTrue(canReach,
                    "A clean crossing just inside the usable mouth should not be blocked by a jaw circle centered on the mouth endpoint.");
                Assert.AreEqual(1, pocketIdx, "The top-right pocket should be selected.");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void CornerPocket_CrossingMouthTooCloseToRoundedJaw_IsBlocked()
        {
            var tableGo = new GameObject("Table");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;

                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -0.522f)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                var mouth = TableGeometry.BuildPocketMouth(table, 1, 0.08f);
                Vector3 mouthDir = mouth.p2 - mouth.p1;
                mouthDir.y = 0f;
                mouthDir.Normalize();

                // This path crosses the mouth opening, but it skims within the
                // rounded jaw-tip clearance. In the real table it contacts the jaw
                // before it has a clean fall line into the pocket.
                Vector3 mouthCrossing = mouth.p1 + mouthDir * (R + 0.002f);
                mouthCrossing.y = R;
                Vector3 target = mouthCrossing - (mouth.center - mouthCrossing).normalized * 0.25f;
                target.y = R;
                Vector3 dir = mouthCrossing - target;
                dir.y = 0f;
                dir.Normalize();

                int pocketIdx;
                bool canReach = ShotSimulator.CanBallReachPocket(
                    target, dir, table, R, PocketRadius, 0.08f, out pocketIdx);

                Assert.IsFalse(canReach,
                    "Crossing the mouth is not enough: the ball must clear the rounded jaw by its radius.");
                Assert.AreEqual(-1, pocketIdx, "Blocked paths should not report a pocket.");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void Run_MissedCornerMouthGap_ClipsToFiniteTableBounds()
        {
            var tableGo = new GameObject("Table");
            var simGo = new GameObject("ShotSimulator");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;
                table.ballRadius = R;

                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -0.522f)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                var sim = simGo.AddComponent<ShotSimulator>();
                Vector3 target = new Vector3(0.9f, R, 0.3f);
                Vector3 targetDir = new Vector3(0.1f, 0f, 0.2f).normalized;
                Vector3 ghost = target - targetDir * (2f * R);
                Vector3 cue = ghost - targetDir * 0.5f;

                SimulationResult result = sim.Run(cue, target, targetDir, R, table);

                Assert.AreEqual(TargetBallEndState.AgainstRail, result.state);
                AssertFinite(result.targetBallEndPos, "target end");
                foreach (Vector3 p in result.targetBallTrajectory)
                    AssertFinite(p, "target trajectory point");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                Object.DestroyImmediate(simGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void Run_TargetBallThroughCornerPocketStopsBeforeOuterFrame()
        {
            var tableGo = new GameObject("Table");
            var simGo = new GameObject("ShotSimulator");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;
                table.ballRadius = R;

                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(1f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(1f, R, -0.522f)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, 0.522f)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -0.522f)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                var mouth = TableGeometry.BuildPocketMouth(table, 1, 0.08f);
                Vector3 mouthDir = mouth.p2 - mouth.p1;
                mouthDir.y = 0f;
                mouthDir.Normalize();
                Vector3 mouthCrossing = mouth.p1 + mouthDir * (R + 0.024f);
                mouthCrossing.y = R;

                Vector3 targetDir = mouth.center - mouthCrossing;
                targetDir.y = 0f;
                targetDir.Normalize();
                Vector3 target = mouthCrossing - targetDir * 0.25f;
                target.y = R;
                Vector3 ghost = target - targetDir * (2f * R);
                Vector3 cue = ghost - targetDir * 0.5f;
                cue.y = R;

                var sim = simGo.AddComponent<ShotSimulator>();
                sim.pocketCatchRadius = PocketRadius;
                sim.jawGap = 0.08f;
                SimulationResult result = sim.Run(cue, target, targetDir, R, table);

                Assert.AreNotEqual(TargetBallEndState.InPocket, result.state,
                    "The display model should report the farthest reachable ball center, not classify the shot as pocketed.");
                Assert.That(result.targetBallEndPos.x, Is.LessThanOrEqualTo(table.playfieldHalfLength + 1e-4f),
                    "The object ball center must not move into the physical outer frame beyond the right cushion nose.");
                Assert.That(result.targetBallEndPos.z, Is.LessThanOrEqualTo(table.playfieldHalfWidth + 1e-4f),
                    "The object ball center must not move into the physical outer frame beyond the top cushion nose.");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                Object.DestroyImmediate(simGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void TableController_PreservesSerializedPlayableBoundsAfterCollectingPockets()
        {
            var tableGo = new GameObject("Table");
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;

                CreatePocket(tableGo.transform, "TL", new Vector3(-1f, R, 0.5f));
                CreatePocket(tableGo.transform, "TR", new Vector3(1f, R, 0.5f));
                CreatePocket(tableGo.transform, "BL", new Vector3(-1f, R, -0.5f));
                CreatePocket(tableGo.transform, "BR", new Vector3(1f, R, -0.5f));
                CreatePocket(tableGo.transform, "TM", new Vector3(0f, R, 0.5f));
                CreatePocket(tableGo.transform, "BM", new Vector3(0f, R, -0.5f));

                InvokeAwake(table);

                Assert.AreEqual(1f, table.playfieldHalfLength, 1e-4f,
                    "Serialized playfield bounds are the cushion nose line and must not be inset again.");
                Assert.AreEqual(0.5f, table.playfieldHalfWidth, 1e-4f,
                    "Serialized playfield bounds are the cushion nose line and must not be inset again.");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
            }
        }

        [Test]
        public void BuildCushionSegments_UsesInboardCushionNoseLine()
        {
            var tableGo = new GameObject("Table");
            var createdPockets = new System.Collections.Generic.List<PocketMarker>();
            try
            {
                var table = tableGo.AddComponent<TableController>();
                table.playfieldHalfLength = 1f;
                table.playfieldHalfWidth = 0.5f;
                float pocketHalfLength = table.playfieldHalfLength + R;
                float pocketHalfWidth = table.playfieldHalfWidth + R;
                createdPockets.Add(CreatePocket(null, "TL", new Vector3(-pocketHalfLength, R, pocketHalfWidth)));
                createdPockets.Add(CreatePocket(null, "TR", new Vector3(pocketHalfLength, R, pocketHalfWidth)));
                createdPockets.Add(CreatePocket(null, "BL", new Vector3(-pocketHalfLength, R, -pocketHalfWidth)));
                createdPockets.Add(CreatePocket(null, "BR", new Vector3(pocketHalfLength, R, -pocketHalfWidth)));
                createdPockets.Add(CreatePocket(null, "TM", new Vector3(0f, R, pocketHalfWidth)));
                createdPockets.Add(CreatePocket(null, "BM", new Vector3(0f, R, -pocketHalfWidth)));
                foreach (var pocket in createdPockets)
                    table.Pockets.Add(pocket);

                var cushions = TableGeometry.BuildCushionSegments(table, jawGap: 0.08f);

                bool hasTopNoseSegment = false;
                bool hasRightNoseSegment = false;
                foreach (var seg in cushions)
                {
                    hasTopNoseSegment |= Mathf.Abs(seg.p1.z - table.playfieldHalfWidth) < 1e-4f
                        && Mathf.Abs(seg.p2.z - table.playfieldHalfWidth) < 1e-4f;
                    hasRightNoseSegment |= Mathf.Abs(seg.p1.x - table.playfieldHalfLength) < 1e-4f
                        && Mathf.Abs(seg.p2.x - table.playfieldHalfLength) < 1e-4f;
                }

                Assert.IsTrue(hasTopNoseSegment, "Top cushion segment should lie on the inboard nose line.");
                Assert.IsTrue(hasRightNoseSegment, "Right cushion segment should lie on the inboard nose line.");
            }
            finally
            {
                Object.DestroyImmediate(tableGo);
                foreach (var pocket in createdPockets)
                    if (pocket != null) Object.DestroyImmediate(pocket.gameObject);
            }
        }

        [Test]
        public void MainScene_TableBoundsMatchBumperNoseLines()
        {
            const string ScenePath = "Assets/Scenes/MainScene.unity";
            EditorSceneManager.OpenScene(ScenePath);

            var table = Object.FindObjectOfType<TableController>();
            var bumper = GameObject.Find("Pool-Table/Bumper");
            Assert.NotNull(table, "MainScene must contain TableController.");
            Assert.NotNull(bumper, "MainScene must contain the FBX Bumper mesh.");

            float noseX = EstimatePositiveNoseCoordinate(bumper.transform, axis: 0, min: 0.90f, max: 1.02f);
            float noseZ = EstimatePositiveNoseCoordinate(bumper.transform, axis: 2, min: 0.40f, max: 0.54f);

            Assert.AreEqual(noseX, table.playfieldHalfLength, 0.006f,
                "X playfield bound should match the bumper nose line, not the pocket center.");
            Assert.AreEqual(noseZ, table.playfieldHalfWidth, 0.006f,
                "Z playfield bound should match the bumper nose line; otherwise balls visually enter the top/bottom cushion.");
        }

        static PocketMarker CreatePocket(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject("Pocket_" + name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var marker = go.AddComponent<PocketMarker>();
            marker.pocketRadius = PocketRadius;
            return marker;
        }

        static void InvokeAwake(TableController table)
        {
            typeof(TableController)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(table, null);
        }

        static void AssertFinite(Vector3 value, string label)
        {
            Assert.IsFalse(float.IsNaN(value.x) || float.IsInfinity(value.x), $"{label}.x must be finite");
            Assert.IsFalse(float.IsNaN(value.y) || float.IsInfinity(value.y), $"{label}.y must be finite");
            Assert.IsFalse(float.IsNaN(value.z) || float.IsInfinity(value.z), $"{label}.z must be finite");
        }

        static float EstimatePositiveNoseCoordinate(Transform meshRoot, int axis, float min, float max)
        {
            var filter = meshRoot.GetComponent<MeshFilter>();
            Assert.NotNull(filter, "Bumper mesh must have a MeshFilter.");
            Assert.NotNull(filter.sharedMesh, "Bumper MeshFilter must have a shared mesh.");

            var values = new SortedSet<float>();
            foreach (Vector3 local in filter.sharedMesh.vertices)
            {
                Vector3 world = meshRoot.TransformPoint(local);
                float raw = axis == 0 ? world.x : world.z;
                float rounded = Mathf.Round(raw * 1000f) / 1000f;
                if (rounded >= min && rounded <= max)
                    values.Add(rounded);
            }

            var descending = values.OrderByDescending(v => v).ToList();
            Assert.Greater(descending.Count, 2, "Need enough bumper vertex clusters to estimate a nose line.");

            float bestGap = 0f;
            float nose = descending[0];
            for (int i = 0; i < descending.Count - 1; i++)
            {
                float gap = descending[i] - descending[i + 1];
                if (gap > bestGap)
                {
                    bestGap = gap;
                    nose = descending[i];
                }
            }

            return nose;
        }
    }
}
