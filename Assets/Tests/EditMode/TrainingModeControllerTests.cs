using NUnit.Framework;
using TMPro;
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

        [Test]
        public void RetakeAnswer_ReturnsToSameExamQuestionForAnotherAttempt()
        {
            root = new GameObject("TrainingModeRetakeRoot");
            var aimManager = root.AddComponent<AimManager>();
            aimManager.showGhostBall = true;
            aimManager.showPredictionMarkers = true;
            aimManager.currentPocket = CreatePocket("TargetPocket", 1, Vector3.zero);
            aimManager.SetManualAimDir(Vector3.forward);

            var hud = root.AddComponent<AimHudController>();
            var markerGo = new GameObject("IdealAnswerV", typeof(RectTransform));
            markerGo.transform.SetParent(root.transform, false);
            markerGo.SetActive(true);
            hud.idealAnswerVBar = markerGo.GetComponent<RectTransform>();

            var submitButton = new GameObject("Submit", typeof(Button)).GetComponent<Button>();
            submitButton.transform.SetParent(root.transform, false);
            var retakeButton = new GameObject("Retake", typeof(Button)).GetComponent<Button>();
            retakeButton.transform.SetParent(root.transform, false);
            var resultText = new GameObject("Result").AddComponent<TextMeshProUGUI>();
            resultText.transform.SetParent(root.transform, false);

            var controller = root.AddComponent<TrainingModeController>();
            controller.aimManager = aimManager;
            controller.aimHud = hud;
            controller.submitButton = submitButton;
            controller.retakeButton = retakeButton;
            controller.resultLabel = resultText;

            controller.SetMode(TrainingMode.Exam);
            controller.examState = ExamState.Result;
            resultText.text = "未进 | 偏移 +5.0 mm R | 角差 1.00°";
            controller.ApplyModeStateForTests();

            Assert.That(submitButton.gameObject.activeSelf, Is.False);
            Assert.That(retakeButton.gameObject.activeSelf, Is.True);

            controller.RetakeAnswer();

            Assert.That(controller.examState, Is.EqualTo(ExamState.Answering));
            Assert.That(aimManager.currentPocket.name, Is.EqualTo("TargetPocket"));
            Assert.That(aimManager.hasManualAim, Is.False);
            Assert.That(resultText.text, Is.EqualTo("请瞄准后提交"));
            Assert.That(markerGo.activeSelf, Is.False);
            Assert.That(submitButton.gameObject.activeSelf, Is.True);
            Assert.That(retakeButton.gameObject.activeSelf, Is.False);
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
