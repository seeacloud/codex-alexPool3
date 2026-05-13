using System.Reflection;
using NUnit.Framework;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class GhostToggleScopeTests
    {
        const float R = 0.0286f;

        GameObject root;
        GameObject ghostPrefab;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (ghostPrefab != null) Object.DestroyImmediate(ghostPrefab);
        }

        [Test]
        public void Recompute_WhenGhostBallDisabled_StillShowsAimLines()
        {
            var aim = CreateAimManager();
            aim.showGhostBall = false;

            InvokeRecompute(aim);

            Assert.That(FindLine(aim.aimLineRenderer.transform, "LR_CueToGhost").enabled, Is.True);
            Assert.That(FindLine(aim.aimLineRenderer.transform, "LR_ObjToPocket").enabled, Is.True);
            Assert.That(GameObject.Find("GhostPrefab(Clone)"), Is.Null);
        }

        [Test]
        public void GhostToggleButton_TogglesOnlyGhostBallVisibilityFlag()
        {
            root = new GameObject("GhostToggleButtonRoot");
            var aim = root.AddComponent<AimManager>();
            aim.showGhostBall = true;

            var button = root.AddComponent<GhostToggleButton>();
            button.aimManager = aim;

            InvokeToggle(button);

            Assert.That(aim.showGhostBall, Is.False);
        }

        AimManager CreateAimManager()
        {
            root = new GameObject("GhostToggleScopeRoot");

            var tableGo = new GameObject("Table");
            tableGo.transform.SetParent(root.transform, false);
            var table = tableGo.AddComponent<TableController>();
            table.playfieldHalfLength = 1.12f;
            table.playfieldHalfWidth = 0.56f;
            table.ballRadius = R;

            var pocketGo = new GameObject("Pocket");
            pocketGo.transform.SetParent(tableGo.transform, false);
            pocketGo.transform.position = new Vector3(1.12f, R, 0.56f);
            var pocket = pocketGo.AddComponent<PocketMarker>();
            table.Pockets.Add(pocket);

            var cue = CreateBall("Cue", new Vector3(-0.45f, R, 0.18f), BallKind.CueBall, table);
            var target = CreateBall("Target", new Vector3(0.15f, R, 0.18f), BallKind.TargetBall, table);

            var vis = new GameObject("Visualization");
            vis.transform.SetParent(root.transform, false);
            var ghost = vis.AddComponent<GhostBallRenderer>();
            ghostPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghostPrefab.name = "GhostPrefab";
            ghost.ghostPrefab = ghostPrefab;

            var aimLine = vis.AddComponent<AimLineRenderer>();
            InvokeAwake(aimLine);

            var managerGo = new GameObject("AimManager");
            managerGo.transform.SetParent(root.transform, false);
            var aim = managerGo.AddComponent<AimManager>();
            aim.cueBall = cue;
            aim.targetBall = target;
            aim.table = table;
            aim.currentPocket = pocket;
            aim.ghostRenderer = ghost;
            aim.aimLineRenderer = aimLine;
            return aim;
        }

        BallController CreateBall(string name, Vector3 pos, BallKind kind, TableController table)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            var ball = go.AddComponent<BallController>();
            ball.kind = kind;
            ball.table = table;
            return ball;
        }

        static LineRenderer FindLine(Transform root, string childName)
        {
            var child = root.Find(childName);
            Assert.That(child, Is.Not.Null);
            var line = child.GetComponent<LineRenderer>();
            Assert.That(line, Is.Not.Null);
            return line;
        }

        static void InvokeRecompute(AimManager aim)
        {
            var method = typeof(AimManager).GetMethod("Recompute", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(aim, null);
        }

        static void InvokeAwake(MonoBehaviour behaviour)
        {
            var method = behaviour.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(behaviour, null);
        }

        static void InvokeToggle(GhostToggleButton button)
        {
            var method = typeof(GhostToggleButton).GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(button, null);
        }
    }
}
