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
        [Tooltip("两次隐藏场景仿真之间最小间隔（秒）")]
        public float simThrottleSeconds = 0.05f;

        [Tooltip("当前选中的袋口（自动选择或手动点选）")]
        public PocketMarker currentPocket;

        [Tooltip("用户手动选择的袋口；非 null 时优先使用它而非自动推荐")]
        public PocketMarker userSelectedPocket;

        [Tooltip("用户手动设置的击打方向（来自 HUD 点击）；非零时覆盖自动瞄准方向")]
        public Vector3 manualAimDir;
        public bool hasManualAim;

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

        public void ClearUserPocket()
        {
            userSelectedPocket = null;
        }

        void Update()
        {
            if (cueBall == null || targetBall == null || table == null) return;
            if (table.Pockets.Count == 0) return;

            Update_HandleManualAimReset();

            PocketMarker desired = userSelectedPocket != null
                ? userSelectedPocket
                : (SelectBestSolvablePocket() ?? SelectClosestPocket());

            bool pocketChanged = currentPocket != desired;
            currentPocket = desired;

            if (ChangedSinceLast() || pocketChanged)
            {
                UpdatePocketHighlight();
                Recompute();
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
                var r = AimSolver.Compute(cueBall.Center, targetBall.Center, currentPocket.Position, table.ballRadius);
                if (!r.solvable)
                {
                    endRenderer.Hide();
                    if (pathRenderer != null) pathRenderer.Hide();
                    if (cuePathRenderer != null) cuePathRenderer.Hide();
                    return;
                }
                aimDir = (r.ghostBallCenter - cueBall.Center).normalized;
            }
            else
            {
                endRenderer.Hide();
                if (pathRenderer != null) pathRenderer.Hide();
                if (cuePathRenderer != null) cuePathRenderer.Hide();
                return;
            }

            var sim = shotSimulator.Run(cueBall.Center, targetBall.Center, aimDir, table.ballRadius, table);
            endRenderer.Show(sim);
            if (pathRenderer != null)
            {
                if (sim.state == TargetBallEndState.NotHit) pathRenderer.Hide();
                else pathRenderer.Show(sim.targetBallTrajectory);
            }

            // Blue line = user's actual aim direction (straight ray from cue along manualAimDir),
            // shown only when manual aim is active. Clipped at table rails so it doesn't extend
            // forever. Independent of physics deflection — it just visualises "where I'm aiming".
            if (cuePathRenderer != null)
            {
                if (hasManualAim && manualAimDir.sqrMagnitude > 1e-6f)
                {
                    Vector3 dir = manualAimDir.normalized;
                    Vector3 end = ClipRayAtTable(cueBall.Center, dir, table);
                    cuePathRenderer.Show(new[] { cueBall.Center, end });
                }
                else
                {
                    cuePathRenderer.Hide();
                }
            }
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

        void Update_HandleManualAimReset()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                ClearManualAim();
            }
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
                var r = AimSolver.Compute(cueBall.Center, targetBall.Center, p.Position, table.ballRadius);
                if (!r.solvable) continue;
                float d = Vector3.Distance(targetBall.Center, p.Position);
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
                || (currentPocket != null && currentPocket.Position != lastPocket);
        }

        void RememberPositions()
        {
            lastCue = cueBall.Center;
            lastTarget = targetBall.Center;
            if (currentPocket != null) lastPocket = currentPocket.Position;
        }

        void Recompute()
        {
            if (currentPocket == null)
            {
                if (ghostRenderer != null) ghostRenderer.Hide();
                if (aimLineRenderer != null) aimLineRenderer.Hide();
                if (hintPanel != null) hintPanel.Show("没有可用袋口");
                return;
            }
            var r = AimSolver.Compute(
                cueBall.Center, targetBall.Center, currentPocket.Position, table.ballRadius);
            if (!r.solvable)
            {
                if (ghostRenderer != null) ghostRenderer.Hide();
                if (aimLineRenderer != null) aimLineRenderer.Hide();
                if (hintPanel != null) hintPanel.Show(r.hintText);
                return;
            }
            if (ghostRenderer != null) ghostRenderer.Show(r.ghostBallCenter);
            if (aimLineRenderer != null)
                aimLineRenderer.Show(r.aimLineStart, r.aimLineEnd, r.objectBallToPocketStart, r.objectBallToPocketEnd);
            float offsetM = ComputeOffsetCm(r, table.ballRadius);
            var side = DetermineAimSide(r);
            if (hintPanel != null)
                hintPanel.Show(HintGenerator.Generate(r.cutAngleDegrees, offsetM * 100f, side));
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
