using UnityEngine;
using PoolAimTrainer.GeometryCore;
using PoolAimTrainer.SceneObjects;
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

        [Tooltip("当前选中的袋口（MVP 阶段由外部/脚本指定）")]
        public PocketMarker currentPocket;

        Vector3 lastCue, lastTarget, lastPocket;

        void Update()
        {
            if (cueBall == null || targetBall == null || table == null) return;
            if (table.Pockets.Count == 0) return;

            if (ChangedSinceLast())
            {
                currentPocket = SelectBestSolvablePocket() ?? SelectClosestPocket();
                Recompute();
                RememberPositions();
            }
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
