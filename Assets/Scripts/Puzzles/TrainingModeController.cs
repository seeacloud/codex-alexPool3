using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.GeometryCore;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.Puzzles
{
    public enum TrainingMode
    {
        Study,
        Exam
    }

    public enum ExamState
    {
        Inactive,
        Answering,
        Result
    }

    public struct AimAnswerEvaluation
    {
        public bool hasAnswer;
        public bool pocketed;
        public float signedOffsetMm;
        public float signedAngleErrorDegrees;
    }

    public class TrainingModeController : MonoBehaviour
    {
        static readonly ReferenceVisualLayer[] AnswerHiddenLayers =
        {
            ReferenceVisualLayer.CueToGhost,
            ReferenceVisualLayer.ObjectToPocket,
            ReferenceVisualLayer.CueThroughTarget,
            ReferenceVisualLayer.MirroredCueThroughTarget,
            ReferenceVisualLayer.TargetBallPath,
            ReferenceVisualLayer.ManualCuePath,
            ReferenceVisualLayer.EstimatedAimLine,
            ReferenceVisualLayer.CutAngleArc,
            ReferenceVisualLayer.AimVsTargetArc,
            ReferenceVisualLayer.EstimatedAimToTargetPathAngle,
            ReferenceVisualLayer.TargetPathToCueThroughAngle,
            ReferenceVisualLayer.ToleranceFanArea,
            ReferenceVisualLayer.ToleranceLowerTargetPath,
            ReferenceVisualLayer.ToleranceUpperTargetPath,
        };

        public TrainingMode mode = TrainingMode.Study;
        public ExamState examState = ExamState.Inactive;

        public ReferenceLineVisibility visibility;
        public AimManager aimManager;
        public ShotSimulator shotSimulator;
        public TableController table;
        public BallController cueBall;
        public BallController targetBall;
        public AimHudController aimHud;
        public Button submitButton;
        public Button retakeButton;
        public TMP_Text resultLabel;

        bool hasGhostSnapshot;
        bool savedShowGhostBall;
        bool savedShowPredictionMarkers;
        Button boundSubmitButton;
        Button boundRetakeButton;
        PocketMarker markedTargetPocket;

        void Start()
        {
            WireDefaults();
            EnsureSubmitButtonListener();
            EnsureRetakeButtonListener();
            ApplyModeState();
        }

        void Update()
        {
            if (mode != TrainingMode.Exam)
                return;

            PocketMarker desired = GetExamTargetPocket();
            if (desired != markedTargetPocket)
                SyncTargetPocketMarker();
        }

        public void WireDefaults()
        {
            if (visibility == null)
                visibility = ReferenceLineVisibility.Active ?? FindObjectOfType<ReferenceLineVisibility>();
            if (visibility != null)
                visibility.ActivateAsCurrent();

            if (aimManager == null)
                aimManager = FindObjectOfType<AimManager>();
            if (shotSimulator == null)
                shotSimulator = FindObjectOfType<ShotSimulator>();
            if (aimHud == null)
                aimHud = FindObjectOfType<AimHudController>();
            if (table == null)
                table = FindObjectOfType<TableController>();
            if (cueBall == null)
            {
                var cue = GameObject.Find("CueBall");
                if (cue != null) cueBall = cue.GetComponent<BallController>();
            }
            if (targetBall == null)
            {
                var target = GameObject.Find("TargetBall");
                if (target != null) targetBall = target.GetComponent<BallController>();
            }

            EnsureRuntimeHudWidgets();
            EnsureSubmitButtonListener();
            EnsureRetakeButtonListener();
        }

        public void SetMode(TrainingMode nextMode)
        {
            WireDefaults();
            mode = nextMode;
            examState = mode == TrainingMode.Exam ? ExamState.Answering : ExamState.Inactive;
            if (resultLabel != null)
                resultLabel.text = mode == TrainingMode.Exam ? "请瞄准后提交" : "";
            ApplyModeState();
        }

        public void OnPuzzleGenerated()
        {
            WireDefaults();
            examState = mode == TrainingMode.Exam ? ExamState.Answering : ExamState.Inactive;
            if (aimHud != null)
                aimHud.ResetAimSelectionToDefault();
            if (resultLabel != null)
                resultLabel.text = mode == TrainingMode.Exam ? "请瞄准后提交" : "";
            ApplyModeState();
        }

        public void SubmitAnswer()
        {
            WireDefaults();
            if (mode != TrainingMode.Exam)
                return;

            AimAnswerEvaluation evaluation = EvaluateCurrentAnswer();
            if (!evaluation.hasAnswer)
            {
                if (resultLabel != null)
                    resultLabel.text = "请先在瞄准窗口选择答案";
                return;
            }

            examState = ExamState.Result;
            ClearAnsweringOverrides();
            ShowIdealAnswerMarker();
            if (resultLabel != null)
                resultLabel.text = FormatEvaluation(evaluation);
            if (aimManager != null)
                aimManager.ForceRefresh();
            ApplyModeState();
        }

        public void RetakeAnswer()
        {
            WireDefaults();
            if (mode != TrainingMode.Exam)
                return;

            examState = ExamState.Answering;
            if (aimManager != null)
                aimManager.ClearManualAim();
            if (aimHud != null)
                aimHud.ClearAimSelection();
            if (resultLabel != null)
                resultLabel.text = "请瞄准后提交";
            HideIdealAnswerMarker();
            ApplyModeState();
        }

        public AimAnswerEvaluation EvaluateCurrentAnswer()
        {
            if (aimManager == null ||
                cueBall == null ||
                targetBall == null ||
                table == null ||
                aimManager.currentPocket == null ||
                !aimManager.hasManualAim ||
                aimManager.manualAimDir.sqrMagnitude < 1e-6f)
            {
                return new AimAnswerEvaluation { hasAnswer = false };
            }

            Vector3 pottingPoint = aimManager.GetPottingPointFor(aimManager.currentPocket);
            AimResult ideal = AimSolver.Compute(cueBall.Center, targetBall.Center, pottingPoint, table.ballRadius);
            if (!ideal.solvable)
                return new AimAnswerEvaluation { hasAnswer = false };

            Vector3 idealDir = (ideal.ghostBallCenter - cueBall.Center).normalized;
            Vector3 manualDir = aimManager.manualAimDir.normalized;
            bool pocketed = EvaluatePocketed(manualDir);

            return new AimAnswerEvaluation
            {
                hasAnswer = true,
                pocketed = pocketed,
                signedOffsetMm = ComputeSignedOffsetMm(cueBall.Center, ideal.ghostBallCenter, manualDir),
                signedAngleErrorDegrees = Vector3.SignedAngle(idealDir, manualDir, Vector3.up),
            };
        }

        bool EvaluatePocketed(Vector3 manualDir)
        {
            if (shotSimulator == null || table == null || cueBall == null || targetBall == null || aimManager == null)
                return false;

            SimulationResult sim = shotSimulator.Run(
                cueBall.Center, targetBall.Center, manualDir, table.ballRadius, table);
            if (!sim.hasBallCollision)
                return false;

            Vector3 targetDir = targetBall.Center - sim.collisionCueBallCenter;
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude < 1e-8f)
                return false;
            targetDir.Normalize();

            int expectedPocket = table.Pockets.IndexOf(aimManager.currentPocket);
            if (expectedPocket < 0)
                return false;

            bool reachesPocket = ShotSimulator.CanBallReachPocket(
                targetBall.Center,
                targetDir,
                table,
                table.ballRadius,
                shotSimulator.pocketCatchRadius,
                shotSimulator.jawGap,
                out int pocketIdx);
            return reachesPocket && pocketIdx == expectedPocket;
        }

        public static float ComputeSignedOffsetMm(Vector3 cuePos, Vector3 idealGhostCenter, Vector3 manualAimDir)
        {
            Vector3 idealDir = idealGhostCenter - cuePos;
            idealDir.y = 0f;
            if (idealDir.sqrMagnitude < 1e-8f || manualAimDir.sqrMagnitude < 1e-8f)
                return 0f;

            idealDir.Normalize();
            manualAimDir.y = 0f;
            manualAimDir.Normalize();

            float denom = Vector3.Dot(manualAimDir, idealDir);
            if (Mathf.Abs(denom) < 1e-6f)
                return 0f;

            float t = Vector3.Dot(idealGhostCenter - cuePos, idealDir) / denom;
            Vector3 playerPoint = cuePos + manualAimDir * t;
            Vector3 right = Vector3.Cross(Vector3.up, idealDir).normalized;
            return Vector3.Dot(playerPoint - idealGhostCenter, right) * 1000f;
        }

        void ApplyModeState()
        {
            if (mode == TrainingMode.Exam && examState == ExamState.Answering)
                ApplyAnsweringOverrides();
            else
                ClearAnsweringOverrides();

            if (mode != TrainingMode.Exam || examState == ExamState.Answering)
                HideIdealAnswerMarker();

            SyncTargetPocketMarker();

            if (submitButton != null)
                submitButton.gameObject.SetActive(mode == TrainingMode.Exam && examState == ExamState.Answering);
            if (retakeButton != null)
                retakeButton.gameObject.SetActive(mode == TrainingMode.Exam && examState == ExamState.Result);
            if (aimManager != null)
                aimManager.ForceRefresh();
        }

#if UNITY_INCLUDE_TESTS
        public void ApplyModeStateForTests()
        {
            ApplyModeState();
        }
#endif

        void EnsureRuntimeHudWidgets()
        {
            if (aimHud == null)
                aimHud = FindObjectOfType<AimHudController>();
            var hud = aimHud;
            EnsureIdealAnswerMarker(hud);

            if (submitButton == null)
            {
                var existing = GameObject.Find("BtnSubmitAnswer");
                if (existing != null)
                    submitButton = existing.GetComponent<Button>();
            }
            if (submitButton == null && hud != null && hud.nudgeLeftBtn != null)
            {
                var template = hud.nudgeLeftBtn.gameObject;
                var submitGo = Instantiate(template, template.transform.parent);
                submitGo.name = "BtnSubmitAnswer";
                submitButton = submitGo.GetComponent<Button>();
                if (submitButton != null)
                    submitButton.onClick.RemoveAllListeners();
                SetButtonText(submitGo, "提交", 15f);

                if (hud.nudgeRightBtn != null)
                    submitGo.transform.SetSiblingIndex(hud.nudgeRightBtn.transform.GetSiblingIndex());
            }

            if (retakeButton == null)
            {
                var existing = GameObject.Find("BtnRetakeAnswer");
                if (existing != null)
                    retakeButton = existing.GetComponent<Button>();
            }
            if (retakeButton == null && hud != null && hud.nudgeLeftBtn != null)
            {
                var template = hud.nudgeLeftBtn.gameObject;
                var retakeGo = Instantiate(template, template.transform.parent);
                retakeGo.name = "BtnRetakeAnswer";
                retakeButton = retakeGo.GetComponent<Button>();
                if (retakeButton != null)
                    retakeButton.onClick.RemoveAllListeners();
                SetButtonText(retakeGo, "重考", 15f);

                if (submitButton != null)
                    retakeGo.transform.SetSiblingIndex(submitButton.transform.GetSiblingIndex() + 1);
            }

            if (resultLabel == null)
            {
                var existing = GameObject.Find("ExamResultLabel");
                if (existing != null)
                    resultLabel = existing.GetComponent<TMP_Text>();
            }
            if (resultLabel == null && hud != null && hud.offsetLabel != null)
            {
                var labelGo = Instantiate(hud.offsetLabel.gameObject, hud.offsetLabel.transform.parent);
                labelGo.name = "ExamResultLabel";
                resultLabel = labelGo.GetComponent<TMP_Text>();
                if (resultLabel != null)
                    resultLabel.text = "";
                labelGo.transform.SetSiblingIndex(hud.offsetLabel.transform.GetSiblingIndex() + 2);
            }
        }

        void EnsureSubmitButtonListener()
        {
            if (boundSubmitButton == submitButton)
                return;

            if (boundSubmitButton != null)
                boundSubmitButton.onClick.RemoveListener(SubmitAnswer);

            if (submitButton != null)
            {
                submitButton.onClick.AddListener(SubmitAnswer);
                boundSubmitButton = submitButton;
            }
            else
            {
                boundSubmitButton = null;
            }
        }

        void EnsureRetakeButtonListener()
        {
            if (boundRetakeButton == retakeButton)
                return;

            if (boundRetakeButton != null)
                boundRetakeButton.onClick.RemoveListener(RetakeAnswer);

            if (retakeButton != null)
            {
                retakeButton.onClick.AddListener(RetakeAnswer);
                boundRetakeButton = retakeButton;
            }
            else
            {
                boundRetakeButton = null;
            }
        }

        static void SetButtonText(GameObject buttonGo, string text, float fontSize)
        {
            var label = buttonGo.GetComponentInChildren<TMP_Text>();
            if (label == null)
                return;

            label.text = text;
            label.fontSize = fontSize;
        }

        void ApplyAnsweringOverrides()
        {
            if (visibility != null)
            {
                for (int i = 0; i < AnswerHiddenLayers.Length; i++)
                    visibility.SetRuntimeLayerVisibilityOverride(AnswerHiddenLayers[i], false);
            }

            if (aimManager != null)
            {
                if (!hasGhostSnapshot)
                {
                    savedShowGhostBall = aimManager.showGhostBall;
                    savedShowPredictionMarkers = aimManager.showPredictionMarkers;
                    hasGhostSnapshot = true;
                }
                aimManager.showGhostBall = false;
                aimManager.showPredictionMarkers = false;
                HideIdealAnswerMarker();
                HideAnswerOnlyMarkers();
            }
        }

        void ClearAnsweringOverrides()
        {
            if (visibility != null)
                visibility.ClearRuntimeLayerVisibilityOverrides();

            if (hasGhostSnapshot && aimManager != null)
            {
                aimManager.showGhostBall = savedShowGhostBall;
                aimManager.showPredictionMarkers = savedShowPredictionMarkers;
                hasGhostSnapshot = false;
            }
        }

        void HideAnswerOnlyMarkers()
        {
            if (aimManager == null)
                return;

            if (aimManager.ghostRenderer != null)
                aimManager.ghostRenderer.Hide();
            if (aimManager.endRenderer != null)
                aimManager.endRenderer.Hide();
            if (aimManager.snapshotRenderer != null)
                aimManager.snapshotRenderer.Hide();
        }

        void SyncTargetPocketMarker()
        {
            if (table == null || table.Pockets == null)
                return;

            PocketMarker targetPocket = GetExamTargetPocket();

            foreach (var pocket in table.Pockets)
            {
                if (pocket != null)
                    pocket.SetTargetMarked(pocket == targetPocket);
            }

            markedTargetPocket = targetPocket;
        }

        PocketMarker GetExamTargetPocket()
        {
            if (mode != TrainingMode.Exam || aimManager == null)
                return null;

            return aimManager.userSelectedPocket != null
                ? aimManager.userSelectedPocket
                : aimManager.currentPocket;
        }

        void ShowIdealAnswerMarker()
        {
            if (aimHud == null || !TryGetIdealGhostCenter(out Vector3 idealGhostCenter))
                return;

            aimHud.ShowIdealAnswerMarker(idealGhostCenter);
        }

        void HideIdealAnswerMarker()
        {
            if (aimHud != null)
                aimHud.HideIdealAnswerMarker();
        }

        bool TryGetIdealGhostCenter(out Vector3 idealGhostCenter)
        {
            idealGhostCenter = Vector3.zero;
            if (aimManager == null ||
                cueBall == null ||
                targetBall == null ||
                table == null ||
                aimManager.currentPocket == null)
            {
                return false;
            }

            Vector3 pottingPoint = aimManager.GetPottingPointFor(aimManager.currentPocket);
            AimResult ideal = AimSolver.Compute(cueBall.Center, targetBall.Center, pottingPoint, table.ballRadius);
            if (!ideal.solvable)
                return false;

            idealGhostCenter = ideal.ghostBallCenter;
            return true;
        }

        static void EnsureIdealAnswerMarker(AimHudController hud)
        {
            if (hud == null || hud.idealAnswerVBar != null || hud.rawImageRect == null)
                return;

            var go = new GameObject("IdealAnswerV", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(hud.rawImageRect, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(hud.hudLineWidthPx, 60f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.65f, 1f, 0f, 0.95f);
            image.raycastTarget = false;

            go.SetActive(false);
            hud.idealAnswerVBar = rt;
        }

        static string FormatEvaluation(AimAnswerEvaluation evaluation)
        {
            string potText = evaluation.pocketed ? "进" : "未进";
            string sign = evaluation.signedOffsetMm >= 0f ? "+" : "-";
            string side = evaluation.signedOffsetMm >= 0f ? "R" : "L";
            float offsetAbs = Mathf.Abs(evaluation.signedOffsetMm);
            float angleAbs = Mathf.Abs(evaluation.signedAngleErrorDegrees);
            return string.Format(CultureInfo.InvariantCulture,
                "{0} | 偏移 {1}{2:F1} mm {3} | 角差 {4:F2}°",
                potText, sign, offsetAbs, side, angleAbs);
        }
    }
}
