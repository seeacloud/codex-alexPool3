using NUnit.Framework;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class PottingToleranceFanRendererTests
    {
        const float R = 0.0286f;

        GameObject tableRoot;
        GameObject rendererRoot;
        GameObject visibilityRoot;

        [TearDown]
        public void TearDown()
        {
            if (tableRoot != null)
                Object.DestroyImmediate(tableRoot);
            if (rendererRoot != null)
                Object.DestroyImmediate(rendererRoot);
            if (visibilityRoot != null)
                Object.DestroyImmediate(visibilityRoot);
        }

        [Test]
        public void TryComputeFanGeometry_ReturnsBoundariesAroundIdealDirection()
        {
            var table = CreateTableWithSixPockets();
            var pocket = table.Pockets[5];
            var cue = new Vector3(-0.35f, R, 0f);
            var target = new Vector3(0.25f, R, 0.18f);

            bool ok = PottingToleranceFanRenderer.TryComputeFanGeometry(
                cue, target, pocket, R, table, 0.08f, 1f, 40f,
                out var geometry);

            Assert.That(ok, Is.True);
            Assert.That(geometry.leftDir.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(geometry.rightDir.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(geometry.idealDir.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(Vector3.SignedAngle(geometry.leftDir, geometry.idealDir, Vector3.up),
                Is.GreaterThanOrEqualTo(0f));
            Assert.That(Vector3.SignedAngle(geometry.idealDir, geometry.rightDir, Vector3.up),
                Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void TryComputeFanGeometry_ReturnsFalseForCoincidentCueAndTarget()
        {
            var table = CreateTableWithSixPockets();
            var pos = new Vector3(0f, R, 0f);

            bool ok = PottingToleranceFanRenderer.TryComputeFanGeometry(
                pos, pos, table.Pockets[0], R, table, 0.08f, 1f, 40f,
                out _);

            Assert.That(ok, Is.False);
        }

        [Test]
        public void ClipRayToPlayfield_ReturnsPointOnOuterBoundary()
        {
            var table = CreateTableWithSixPockets();
            var start = new Vector3(0f, R, 0f);

            Vector3 hit = PottingToleranceFanRenderer.ClipRayToPlayfield(
                start, new Vector3(1f, 0f, 0.25f).normalized, table);

            Assert.That(hit.x, Is.EqualTo(table.playfieldHalfLength).Within(1e-4f));
            Assert.That(Mathf.Abs(hit.z), Is.LessThanOrEqualTo(table.playfieldHalfWidth + 1e-4f));
            Assert.That(hit.y, Is.EqualTo(R).Within(1e-4f));
        }

        [Test]
        public void Show_CreatesFilledFanAndTargetBoundaryPathsWithoutCueFanBoundaryLines()
        {
            var table = CreateTableWithSixPockets();
            rendererRoot = new GameObject("FanRenderer");
            var renderer = rendererRoot.AddComponent<PottingToleranceFanRenderer>();
            var cue = new Vector3(-0.35f, R, 0f);
            var target = new Vector3(0.25f, R, 0.18f);

            renderer.Show(cue, target, table.Pockets[5], R, table, 0.08f);

            Assert.That(rendererRoot.transform.Find("PottingToleranceFanMesh"), Is.Not.Null);
            Assert.That(rendererRoot.transform.Find("PottingToleranceFanLeftBoundary"), Is.Null);
            Assert.That(rendererRoot.transform.Find("PottingToleranceFanRightBoundary"), Is.Null);

            var lower = FindLine(rendererRoot.transform, "LR_ToleranceLowerTargetPath");
            var upper = FindLine(rendererRoot.transform, "LR_ToleranceUpperTargetPath");
            AssertBoundaryTargetPath(lower, target, table);
            AssertBoundaryTargetPath(upper, target, table);
        }

        [Test]
        public void Show_RespectsIndependentBoundaryTargetPathToggles()
        {
            var visibility = CreateVisibility();
            visibility.SetLayerVisible(ReferenceVisualLayer.ToleranceUpperTargetPath, false);

            var table = CreateTableWithSixPockets();
            rendererRoot = new GameObject("FanRenderer");
            var renderer = rendererRoot.AddComponent<PottingToleranceFanRenderer>();
            var cue = new Vector3(-0.35f, R, 0f);
            var target = new Vector3(0.25f, R, 0.18f);

            renderer.Show(cue, target, table.Pockets[5], R, table, 0.08f);

            Assert.That(FindLine(rendererRoot.transform, "LR_ToleranceLowerTargetPath").enabled, Is.True);
            Assert.That(FindLine(rendererRoot.transform, "LR_ToleranceUpperTargetPath").enabled, Is.False);
        }

        [Test]
        public void TryComputeFanGeometry_DoesNotIncludeCueDirectionThatHitsCushionBeforePocket()
        {
            var table = CreateTableWithSixPockets();
            var pocket = table.Pockets[5];
            var cue = new Vector3(-0.55f, R, 0.10f);
            var target = new Vector3(0.82f, R, 0.20f);

            bool ok = PottingToleranceFanRenderer.TryComputeFanGeometry(
                cue, target, pocket, R, table, 0.08f, 1f, 40f,
                out var geometry);

            Assert.That(ok, Is.True);

            Vector3 blockedCueDir = Quaternion.AngleAxis(-0.5f, Vector3.up) * geometry.idealDir;
            Assert.That(TryGetTargetDirection(cue, target, blockedCueDir, out var targetDir), Is.True);
            Assert.That(ShotSimulator.CanBallReachPocket(
                    target, targetDir, table, R, 0.06f, 0.08f, out _),
                Is.False,
                "This cue direction still contacts the object ball, but the resulting object-ball path is blocked before the pocket.");
            Assert.That(DirectionInsideFan(geometry, blockedCueDir), Is.False);
        }

        TableController CreateTableWithSixPockets()
        {
            tableRoot = new GameObject("Table");
            var table = tableRoot.AddComponent<TableController>();
            table.playfieldHalfLength = 1.12f;
            table.playfieldHalfWidth = 0.56f;
            table.ballRadius = R;

            AddPocket(table, -1.12f, -0.56f);
            AddPocket(table, 0f, -0.56f);
            AddPocket(table, 1.12f, -0.56f);
            AddPocket(table, -1.12f, 0.56f);
            AddPocket(table, 0f, 0.56f);
            AddPocket(table, 1.12f, 0.56f);
            return table;
        }

        static void AddPocket(TableController table, float x, float z)
        {
            var go = new GameObject("Pocket");
            go.transform.SetParent(table.transform, false);
            go.transform.position = new Vector3(x, 0f, z);
            table.Pockets.Add(go.AddComponent<PocketMarker>());
        }

        ReferenceLineVisibility CreateVisibility()
        {
            visibilityRoot = new GameObject("ReferenceLineVisibility");
            var visibility = visibilityRoot.AddComponent<ReferenceLineVisibility>();
            visibility.ActivateAsCurrent();
            return visibility;
        }

        static LineRenderer FindLine(Transform root, string childName)
        {
            var child = root.Find(childName);
            Assert.That(child, Is.Not.Null);
            var line = child.GetComponent<LineRenderer>();
            Assert.That(line, Is.Not.Null);
            return line;
        }

        static void AssertBoundaryTargetPath(LineRenderer line, Vector3 target, TableController table)
        {
            Assert.That(line.enabled, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(2));

            Vector3 start = line.GetPosition(0);
            Vector3 end = line.GetPosition(1);
            Assert.That(start.x, Is.EqualTo(target.x).Within(1e-4f));
            Assert.That(start.z, Is.EqualTo(target.z).Within(1e-4f));

            bool touchesRail =
                Mathf.Abs(Mathf.Abs(end.x) - table.playfieldHalfLength) < 1e-4f
                || Mathf.Abs(Mathf.Abs(end.z) - table.playfieldHalfWidth) < 1e-4f;
            Assert.That(touchesRail, Is.True, "Boundary target path should extend to the table edge.");
        }

        static bool TryGetTargetDirection(Vector3 cue, Vector3 target, Vector3 cueDir, out Vector3 targetDir)
        {
            targetDir = Vector3.zero;
            cueDir.y = 0f;
            cueDir.Normalize();

            Vector3 toTarget = target - cue;
            toTarget.y = 0f;
            float b = Vector3.Dot(cueDir, toTarget);
            float c = toTarget.sqrMagnitude - 4f * R * R;
            float discriminant = b * b - c;
            if (discriminant < 0f || b < 0f)
                return false;

            Vector3 ghost = cue + cueDir * (b - Mathf.Sqrt(discriminant));
            ghost.y = R;
            targetDir = target - ghost;
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude < 1e-8f)
                return false;

            targetDir.Normalize();
            return true;
        }

        static bool DirectionInsideFan(PottingToleranceFanRenderer.FanGeometry geometry, Vector3 cueDir)
        {
            cueDir.y = 0f;
            cueDir.Normalize();

            float left = Vector3.SignedAngle(geometry.idealDir, geometry.leftDir, Vector3.up);
            float right = Vector3.SignedAngle(geometry.idealDir, geometry.rightDir, Vector3.up);
            if (left > right)
            {
                float tmp = left;
                left = right;
                right = tmp;
            }

            float angle = Vector3.SignedAngle(geometry.idealDir, cueDir, Vector3.up);
            return angle >= left - 1e-5f && angle <= right + 1e-5f;
        }
    }
}
