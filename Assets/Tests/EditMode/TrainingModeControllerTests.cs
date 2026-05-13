using NUnit.Framework;
using PoolAimTrainer.Core;
using PoolAimTrainer.Puzzles;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Tests.EditMode
{
    public class TrainingModeControllerTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void ExamAnswering_HidesAnswerLayersWithoutChangingLayerChoices()
        {
            root = new GameObject("TrainingModeControllerTestRoot");
            var visibility = root.AddComponent<ReferenceLineVisibility>();
            visibility.playerPrefsKeyPrefix = "PoolAimTrainer.Tests.TrainingMode.";
            visibility.ActivateAsCurrent();
            visibility.SetLayerVisible(ReferenceVisualLayer.CueToGhost, true);
            visibility.SetLayerVisible(ReferenceVisualLayer.ManualCuePath, true);

            var aimManager = root.AddComponent<AimManager>();
            aimManager.showGhostBall = true;
            aimManager.showPredictionMarkers = true;

            var submitButton = new GameObject("Submit", typeof(Button)).GetComponent<Button>();
            submitButton.transform.SetParent(root.transform, false);

            var controller = root.AddComponent<TrainingModeController>();
            controller.visibility = visibility;
            controller.aimManager = aimManager;
            controller.submitButton = submitButton;

            controller.SetMode(TrainingMode.Exam);

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.False);
            Assert.That(visibility.IsVisible(ReferenceVisualLayer.ManualCuePath), Is.False);
            Assert.That(visibility.IsLayerEnabled(ReferenceVisualLayer.CueToGhost), Is.True);
            Assert.That(aimManager.showGhostBall, Is.False);
            Assert.That(aimManager.showPredictionMarkers, Is.False);
            Assert.That(submitButton.gameObject.activeSelf, Is.True);

            controller.SetMode(TrainingMode.Study);

            Assert.That(visibility.IsVisible(ReferenceVisualLayer.CueToGhost), Is.True);
            Assert.That(visibility.IsVisible(ReferenceVisualLayer.ManualCuePath), Is.True);
            Assert.That(aimManager.showGhostBall, Is.True);
            Assert.That(aimManager.showPredictionMarkers, Is.True);
            Assert.That(submitButton.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ComputeSignedOffsetMm_ReportsRightOffsetAtIdealGhostPlane()
        {
            Vector3 cue = Vector3.zero;
            Vector3 idealGhost = new Vector3(0f, 0f, 1f);
            Vector3 manualAim = new Vector3(0.002f, 0f, 1f).normalized;

            float offsetMm = TrainingModeController.ComputeSignedOffsetMm(cue, idealGhost, manualAim);

            Assert.That(offsetMm, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void ExamAnswering_HidesIdealAnswerMarker()
        {
            root = new GameObject("TrainingModeIdealAnswerMarkerRoot");
            var hud = root.AddComponent<AimHudController>();
            var markerGo = new GameObject("IdealAnswerV", typeof(RectTransform));
            markerGo.transform.SetParent(root.transform, false);
            markerGo.SetActive(true);
            hud.idealAnswerVBar = markerGo.GetComponent<RectTransform>();

            var controller = root.AddComponent<TrainingModeController>();
            controller.aimHud = hud;

            controller.SetMode(TrainingMode.Exam);

            Assert.That(markerGo.activeSelf, Is.False);
        }

        [Test]
        public void ExamAnswering_MarksOnlyCurrentPocketAsTarget()
        {
            root = new GameObject("TrainingModeTargetPocketMarkerRoot");
            var table = root.AddComponent<TableController>();
            var firstPocket = CreatePocket("Pocket1", 1, new Vector3(-1f, 0f, 0f));
            var secondPocket = CreatePocket("Pocket2", 2, new Vector3(1f, 0f, 0f));
            table.Pockets.Add(firstPocket);
            table.Pockets.Add(secondPocket);

            var aimManager = root.AddComponent<AimManager>();
            aimManager.table = table;
            aimManager.currentPocket = secondPocket;

            var controller = root.AddComponent<TrainingModeController>();
            controller.table = table;
            controller.aimManager = aimManager;

            controller.SetMode(TrainingMode.Exam);

            Assert.That(firstPocket.IsTargetMarked, Is.False);
            Assert.That(secondPocket.IsTargetMarked, Is.True);

            controller.SetMode(TrainingMode.Study);

            Assert.That(firstPocket.IsTargetMarked, Is.False);
            Assert.That(secondPocket.IsTargetMarked, Is.False);
        }

        [Test]
        public void ExamAnswering_MarksUserSelectedPocketBeforeAimManagerUpdatesCurrentPocket()
        {
            root = new GameObject("TrainingModeSelectedTargetPocketMarkerRoot");
            var table = root.AddComponent<TableController>();
            var firstPocket = CreatePocket("Pocket1", 1, new Vector3(-1f, 0f, 0f));
            var secondPocket = CreatePocket("Pocket2", 2, new Vector3(1f, 0f, 0f));
            table.Pockets.Add(firstPocket);
            table.Pockets.Add(secondPocket);

            var aimManager = root.AddComponent<AimManager>();
            aimManager.table = table;
            aimManager.currentPocket = firstPocket;
            aimManager.SetUserPocket(secondPocket);

            var controller = root.AddComponent<TrainingModeController>();
            controller.table = table;
            controller.aimManager = aimManager;

            controller.SetMode(TrainingMode.Exam);

            Assert.That(firstPocket.IsTargetMarked, Is.False);
            Assert.That(secondPocket.IsTargetMarked, Is.True);
        }

        PocketMarker CreatePocket(string name, int number, Vector3 position)
        {
            var pocketGo = new GameObject(name);
            pocketGo.transform.SetParent(root.transform, false);
            pocketGo.transform.position = position;
            var pocket = pocketGo.AddComponent<PocketMarker>();
            pocket.pocketNumber = number;
            return pocket;
        }
    }
}
