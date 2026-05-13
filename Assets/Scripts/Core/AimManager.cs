using UnityEngine;
using PoolAimTrainer.GeometryCore;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.Visualization;
using PoolAimTrainer.UI;

namespace PoolAimTrainer.Core
{
    public class AimManager : MonoBehaviour
    {
        public BallController cueBall;
        public BallController targetBall;
        public TableController table;
        public GhostBallRenderer ghostRenderer;
        public AimLineRenderer aimLineRenderer;
        public HintPanel hintPanel;

        [Header("E1: Target ball final-position ghost")]
        public ShotSimulator shotSimulator;
        public TargetBallEndRenderer endRenderer;
        public TargetBallPathRenderer pathRenderer;
        public CueBallPathRenderer cuePathRenderer;
        public TrajectorySnapshotRenderer snapshotRenderer;
        public PottingToleranceFanRenderer toleranceFanRenderer;
        public CueThroughTargetLineRenderer cueThroughTargetRenderer;
        public EstimatedAimLineRenderer estimatedAimLineRenderer;
        public CutAngleArcRenderer cutAngleArcRenderer;
        public AimVsTargetArcRenderer aimVsTargetArcRenderer;
        public TargetLineAngleArcRenderer targetLineAngleArcRenderer;
        public ReferenceLineVisibility referenceLineVisibility;
        public ReferenceLineTogglePanel referenceLineTogglePanel;
        [Tooltip("两次隐藏场景仿真之间最小间隔（秒）")]
        public float simThrottleSeconds = 0.05f;

        [Tooltip("当前选中的袋口（自动选择或手动点选）")]
        public PocketMarker currentPocket;

        [Tooltip("是否显示 ghost ball 本身；不影响瞄准线、切角弧或容差扇形")]
        public bool showGhostBall = true;

        [Tooltip("是否显示模拟预测球、碰撞残影和接触点；考试答题时会临时关闭")]
        public bool showPredictionMarkers = true;

        [Tooltip("用户手动选择的袋口；非 null 时优先使用它而非自动推荐")]
        public PocketMarker userSelectedPocket;

        [Tooltip("用户手动设置的击打方向（来自 HUD 点击）；非零时覆盖自动瞄准方向")]
        public Vector3 manualAimDir;
        public bool hasManualAim;

        const float DEFAULT_JAW_GAP = 0.08f;

        public void SetManualAimDir(Vector3 dir)
        {
            manualAimDir = dir.normalized;
            hasManualAim = true;
            simDirty = true;
        }

        public void ClearManualAim()
        {
            hasManualAim = false;
            manualAimDir = Vector3.zero;
            simDirty = true;
        }

        Vector3 lastCue, lastTarget, lastPocket;
        PocketMarker lastForcedPocketSource;
        PocketMarker lastHighlighted;
        bool simDirty;
        float lastSimTime;

        public void SetUserPocket(PocketMarker pocket)
        {
            userSelectedPocket = pocket;
        }

        public void ForceRefresh()
        {
            lastCue = Vector3.positiveInfinity;
            lastTarget = Vector3.positiveInfinity;
            lastPocket = Vector3.positiveInfinity;
            simDirty = true;
        }

        public void ClearUserPocket()
        {
            userSelectedPocket = null;
        }

        void Awake()
        {
            AutoWireCueThroughTarget();
            AutoWireReferenceLineControls();
        }

        void OnDestroy()
        {
            if (referenceLineVisibility != null)
                referenceLineVisibility.Changed -= OnReferenceLineVisibilityChanged;
        }

        void AutoWireCueThroughTarget()
        {
            if (cueThroughTargetRenderer != null && cutAngleArcRenderer != null
                && aimVsTargetArcRenderer != null && snapshotRenderer != null
                && toleranceFanRenderer != null && estimatedAimLineRenderer != null
                && targetLineAngleArcRenderer != null) return;
            GameObject host = GameObject.Find("_Visualization");
            if (host == null) host = gameObject;
            if (cueThroughTargetRenderer == null)
            {
                cueThroughTargetRenderer = host.GetComponent<CueThroughTargetLineRenderer>();
                if (cueThroughTargetRenderer == null)
                    cueThroughTargetRenderer = host.AddComponent<CueThroughTargetLineRenderer>();
            }
            if (cutAngleArcRenderer == null)
            {
                cutAngleArcRenderer = host.GetComponent<CutAngleArcRenderer>();
                if (cutAngleArcRenderer == null)
                    cutAngleArcRenderer = host.AddComponent<CutAngleArcRenderer>();
            }
            if (aimVsTargetArcRenderer == null)
            {
                aimVsTargetArcRenderer = host.GetComponent<AimVsTargetArcRenderer>();
                if (aimVsTargetArcRenderer == null)
                    aimVsTargetArcRenderer = host.AddComponent<AimVsTargetArcRenderer>();
            }
            if (snapshotRenderer == null)
            {
                snapshotRenderer = host.GetComponent<TrajectorySnapshotRenderer>();
                if (snapshotRenderer == null)
                    snapshotRenderer = host.AddComponent<TrajectorySnapshotRenderer>();
            }
            if (toleranceFanRenderer == null)
            {
                toleranceFanRenderer = host.GetComponent<PottingToleranceFanRenderer>();
                if (toleranceFanRenderer == null)
                    toleranceFanRenderer = host.AddComponent<PottingToleranceFanRenderer>();
            }
            if (estimatedAimLineRenderer == null)
            {
                estimatedAimLineRenderer = host.GetComponent<EstimatedAimLineRenderer>();
                if (estimatedAimLineRenderer == null)
                    estimatedAimLineRenderer = host.AddComponent<EstimatedAimLineRenderer>();
            }
            if (targetLineAngleArcRenderer == null)
            {
                targetLineAngleArcRenderer = host.GetComponent<TargetLineAngleArcRenderer>();
                if (targetLineAngleArcRenderer == null)
                    targetLineAngleArcRenderer = host.AddComponent<TargetLineAngleArcRenderer>();
            }
        }

        void AutoWireReferenceLineControls()
        {
            if (referenceLineVisibility == null)
            {
                referenceLineVisibility = FindObjectOfType<ReferenceLineVisibility>();
                if (referenceLineVisibility == null)
                    referenceLineVisibility = gameObject.AddComponent<ReferenceLineVisibility>();
            }
            referenceLineVisibility.ActivateAsCurrent();
            referenceLineVisibility.Changed -= OnReferenceLineVisibilityChanged;
            referenceLineVisibility.Changed += OnReferenceLineVisibilityChanged;

            if (referenceLineTogglePanel == null)
            {
                referenceLineTogglePanel = FindObjectOfType<ReferenceLineTogglePanel>();
                if (referenceLineTogglePanel == null)
                    referenceLineTogglePanel = gameObject.AddComponent<ReferenceLineTogglePanel>();
            }
            referenceLineTogglePanel.visibility = referenceLineVisibility;
            referenceLineTogglePanel.aimManager = this;
            referenceLineTogglePanel.WireDefaults();
        }

        void OnReferenceLineVisibilityChanged()
        {
            ForceRefresh();
        }

        void Update()
        {
            if (cueBall == null || targetBall == null || table == null) return;
            if (table.Pockets.Count == 0) return;

            PocketMarker desired = userSelectedPocket != null
                ? userSelectedPocket
                : (SelectBestSolvablePocket() ?? SelectClosestPocket());

            bool pocketChanged = currentPocket != desired;
            currentPocket = desired;

            if (ChangedSinceLast() || pocketChanged)
            {
                UpdatePocketHighlight();
                Recompute();
                UpdateCueThroughTarget();
                RememberPositions();
                simDirty = true;
            }

            if (simDirty && Time.unscaledTime - lastSimTime > simThrottleSeconds)
            {
                simDirty = false;
                lastSimTime = Time.unscaledTime;
                TriggerSim();
            }
        }

        void TriggerSim()
        {
            if (shotSimulator == null || endRenderer == null) return;

            Vector3 aimDir;
            if (hasManualAim && manualAimDir.sqrMagnitude > 1e-6f)
            {
                aimDir = manualAimDir.normalized;
            }
            else if (currentPocket != null)
            {
                Vector3 pottingPoint = GetPottingPointFor(currentPocket);
                if (!TryComputeDefaultAimDirection(
                    cueBall.Center, targetBall.Center, pottingPoint, table.ballRadius, out aimDir))
                {
                    endRenderer.Hide();
                    if (pathRenderer != null) pathRenderer.Hide();
                    if (cuePathRenderer != null) cuePathRenderer.Hide();
                    if (snapshotRenderer != null) snapshotRenderer.Hide();
                    if (estimatedAimLineRenderer != null) estimatedAimLineRenderer.Hide();
                    if (targetLineAngleArcRenderer != null) targetLineAngleArcRenderer.Hide();
                    return;
                }
            }
            else
            {
                endRenderer.Hide();
                if (pathRenderer != null) pathRenderer.Hide();
                if (cuePathRenderer != null) cuePathRenderer.Hide();
                if (snapshotRenderer != null) snapshotRenderer.Hide();
                if (estimatedAimLineRenderer != null) estimatedAimLineRenderer.Hide();
                if (targetLineAngleArcRenderer != null) targetLineAngleArcRenderer.Hide();
                return;
            }

            var sim = shotSimulator.Run(cueBall.Center, targetBall.Center, aimDir, table.ballRadius, table);
            if (showPredictionMarkers)
                endRenderer.Show(sim);
            else
                endRenderer.Hide();
            if (pathRenderer != null)
            {
                if (sim.state == TargetBallEndState.NotHit) pathRenderer.Hide();
                else pathRenderer.Show(sim.targetBallTrajectory);
            }

            if (snapshotRenderer != null && showPredictionMarkers)
                snapshotRenderer.Show(sim, table.ballRadius);
            else if (snapshotRenderer != null)
                snapshotRenderer.Hide();

            // Current aim direction: user-selected aim if present, otherwise the default
            // ghost-ball center aim. This keeps every mode from starting with empty aim visuals.
            bool hasManualAimDir = hasManualAim && manualAimDir.sqrMagnitude > 1e-6f;
            bool hasVisibleAim = aimDir.sqrMagnitude > 1e-6f;
            Vector3 visibleAimDir = hasManualAimDir ? manualAimDir.normalized : aimDir.normalized;
            if (hasVisibleAim)
            {
                if (cuePathRenderer != null)
                {
                    Vector3 end = ClipRayAtTable(cueBall.Center, visibleAimDir, table);
                    cuePathRenderer.Show(new[] { cueBall.Center, end });
                }

                if (estimatedAimLineRenderer != null)
                    estimatedAimLineRenderer.Show(
                        cueBall.Center, targetBall.Center, visibleAimDir, table.ballRadius, table);
            }
            else
            {
                if (cuePathRenderer != null) cuePathRenderer.Hide();
                if (estimatedAimLineRenderer != null) estimatedAimLineRenderer.Hide();
            }

            UpdateTargetLineAngles(sim, hasVisibleAim ? visibleAimDir : Vector3.zero, hasVisibleAim);

            // Arc + label for angle between current aim and cue→target.
            if (aimVsTargetArcRenderer != null)
            {
                if (hasVisibleAim)
                {
                    aimVsTargetArcRenderer.Show(cueBall.Center, targetBall.Center, visibleAimDir);
                }
                else
                {
                    aimVsTargetArcRenderer.Hide();
                }
            }
        }

        void UpdateTargetLineAngles(SimulationResult sim, Vector3 manualDir, bool hasVisibleManualAim)
        {
            if (targetLineAngleArcRenderer == null)
                return;

            if (!sim.hasBallCollision || sim.targetBallTrajectory == null || sim.targetBallTrajectory.Length < 2)
            {
                targetLineAngleArcRenderer.Hide();
                return;
            }

            Vector3 targetPathDir = sim.targetBallTrajectory[sim.targetBallTrajectory.Length - 1] - targetBall.Center;
            targetPathDir.y = 0f;
            Vector3 cueThroughTargetDir = targetBall.Center - cueBall.Center;
            cueThroughTargetDir.y = 0f;
            Vector3 estimatedAimDir = Vector3.zero;
            bool hasEstimatedAimDir = false;
            if (hasVisibleManualAim)
            {
                hasEstimatedAimDir = EstimatedAimLineRenderer.TryComputeLine(
                    cueBall.Center, targetBall.Center, manualDir, table.ballRadius, table,
                    out var hitPoint, out _);
                if (hasEstimatedAimDir)
                {
                    estimatedAimDir = targetBall.Center - hitPoint;
                    estimatedAimDir.y = 0f;
                }
            }

            targetLineAngleArcRenderer.Show(
                targetBall.Center,
                estimatedAimDir,
                hasEstimatedAimDir,
                targetPathDir,
                cueThroughTargetDir);
        }

        void UpdateCueThroughTarget()
        {
            AutoWireCueThroughTarget();
            if (cueThroughTargetRenderer == null) return;
            Vector3 cue = cueBall.Center;
            Vector3 tgt = targetBall.Center;
            Vector3 dir = tgt - cue;
            if (dir.sqrMagnitude < 1e-6f)
            {
                cueThroughTargetRenderer.Hide();
                return;
            }
            dir = dir.normalized;
            Vector3 railHit = ClipRayAtTable(tgt, dir, table);
            cueThroughTargetRenderer.Show(cue, tgt, railHit);
        }

        static Vector3 ClipRayAtTable(Vector3 start, Vector3 dir, TableController table)
        {
            float w = table.playfieldHalfLength;
            float h = table.playfieldHalfWidth;
            float tBest = float.MaxValue;
            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = (dir.x > 0f ? (w - start.x) : (-w - start.x)) / dir.x;
                if (tx > 0f && tx < tBest) tBest = tx;
            }
            if (Mathf.Abs(dir.z) > 1e-6f)
            {
                float tz = (dir.z > 0f ? (h - start.z) : (-h - start.z)) / dir.z;
                if (tz > 0f && tz < tBest) tBest = tz;
            }
            if (tBest >= float.MaxValue) tBest = 2f;
            Vector3 end = start + dir * tBest;
            end.y = table.ballRadius;
            return end;
        }

        public static bool TryComputeDefaultAimDirection(
            Vector3 cueCenter,
            Vector3 targetCenter,
            Vector3 pottingPoint,
            float ballRadius,
            out Vector3 aimDir)
        {
            aimDir = Vector3.zero;
            AimResult result = AimSolver.Compute(cueCenter, targetCenter, pottingPoint, ballRadius);
            if (!result.solvable)
                return false;

            aimDir = result.ghostBallCenter - cueCenter;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude < 1e-8f)
                return false;

            aimDir.Normalize();
            return true;
        }



        void UpdatePocketHighlight()
        {
            if (lastHighlighted != null && lastHighlighted != currentPocket)
                lastHighlighted.SetHighlighted(false);
            if (currentPocket != null)
                currentPocket.SetHighlighted(true);
            lastHighlighted = currentPocket;
        }

        PocketMarker SelectBestSolvablePocket()
        {
            PocketMarker best = null;
            float bestDist = float.MaxValue;
            foreach (var p in table.Pockets)
            {
                Vector3 pottingPoint = GetPottingPointFor(p);
                var r = AimSolver.Compute(cueBall.Center, targetBall.Center, pottingPoint, table.ballRadius);
                if (!r.solvable) continue;
                float d = Vector3.Distance(targetBall.Center, pottingPoint);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        PocketMarker SelectClosestPocket()
        {
            PocketMarker best = null;
            float bestDist = float.MaxValue;
            foreach (var p in table.Pockets)
            {
                float d = Vector3.Distance(targetBall.Center, p.Position);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        bool ChangedSinceLast()
        {
            return cueBall.Center != lastCue
                || targetBall.Center != lastTarget
                || (currentPocket != null && GetPottingPointFor(currentPocket) != lastPocket);
        }

        void RememberPositions()
        {
            lastCue = cueBall.Center;
            lastTarget = targetBall.Center;
            if (currentPocket != null) lastPocket = GetPottingPointFor(currentPocket);
        }

        void Recompute()
        {
            if (currentPocket == null)
            {
                if (ghostRenderer != null) ghostRenderer.Hide();
                if (aimLineRenderer != null) aimLineRenderer.Hide();
                if (cutAngleArcRenderer != null) cutAngleArcRenderer.Hide();
                if (toleranceFanRenderer != null) toleranceFanRenderer.Hide();
                if (estimatedAimLineRenderer != null) estimatedAimLineRenderer.Hide();
                if (targetLineAngleArcRenderer != null) targetLineAngleArcRenderer.Hide();
                if (hintPanel != null) hintPanel.Clear();
                return;
            }
            Vector3 pottingPoint = GetPottingPointFor(currentPocket);
            var r = AimSolver.Compute(
                cueBall.Center, targetBall.Center, pottingPoint, table.ballRadius);
            if (!r.solvable)
            {
                if (ghostRenderer != null) ghostRenderer.Hide();
                if (aimLineRenderer != null) aimLineRenderer.Hide();
                if (cutAngleArcRenderer != null) cutAngleArcRenderer.Hide();
                if (toleranceFanRenderer != null) toleranceFanRenderer.Hide();
                if (estimatedAimLineRenderer != null) estimatedAimLineRenderer.Hide();
                if (targetLineAngleArcRenderer != null) targetLineAngleArcRenderer.Hide();
                if (hintPanel != null) hintPanel.Clear();
                return;
            }
            if (ghostRenderer != null)
            {
                if (showGhostBall) ghostRenderer.Show(r.ghostBallCenter);
                else ghostRenderer.Hide();
            }
            if (aimLineRenderer != null)
                aimLineRenderer.Show(r.aimLineStart, r.aimLineEnd, r.objectBallToPocketStart, r.objectBallToPocketEnd);
            if (cutAngleArcRenderer != null)
                cutAngleArcRenderer.Show(cueBall.Center, targetBall.Center, pottingPoint, table.ballRadius);
            if (toleranceFanRenderer != null)
            {
                float jawGap = shotSimulator != null ? shotSimulator.jawGap : DEFAULT_JAW_GAP;
                toleranceFanRenderer.Show(
                    cueBall.Center, targetBall.Center, currentPocket,
                    table.ballRadius, table, jawGap);
            }
            float offsetM = ComputeOffsetCm(r, table.ballRadius);
            var side = DetermineAimSide(r);
            if (hintPanel != null) hintPanel.Clear();
        }

        public Vector3 GetPottingPointFor(PocketMarker pocket)
        {
            if (pocket == null) return Vector3.zero;
            if (table == null || table.Pockets == null || targetBall == null) return pocket.Position;

            int pocketIndex = table.Pockets.IndexOf(pocket);
            if (pocketIndex < 0) return pocket.Position;

            float jawGap = shotSimulator != null ? shotSimulator.jawGap : DEFAULT_JAW_GAP;
            Vector3 point = TableGeometry.GetPottingPoint(table, pocketIndex, targetBall.Center, jawGap);
            point.y = table.ballRadius;
            return point;
        }

        float ComputeOffsetCm(AimResult r, float ballRadius)
        {
            return 2f * ballRadius * Mathf.Sin(r.cutAngleDegrees * Mathf.Deg2Rad);
        }

        AimSide DetermineAimSide(AimResult r)
        {
            Vector3 objToPocket = (r.objectBallToPocketEnd - r.objectBallToPocketStart).normalized;
            Vector3 objToGhost = (r.ghostBallCenter - r.objectBallToPocketStart).normalized;
            Vector3 cross = Vector3.Cross(objToPocket, objToGhost);
            if (cross.y > 0.01f) return AimSide.Right;
            if (cross.y < -0.01f) return AimSide.Left;
            return AimSide.Center;
        }
    }
}
