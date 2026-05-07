using System.Collections;
using UnityEngine;
using PoolAimTrainer.Core;
using PoolAimTrainer.GeometryCore;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;

namespace PoolAimTrainer.UI
{
    public class ShotButton : MonoBehaviour
    {
        public AimManager aimManager;
        public ShotSimulator shotSimulator;
        public BallController cueBall;
        public BallController targetBall;
        public TableController table;

        [Tooltip("动画总时长（秒）")]
        public float animDuration = 1.2f;
        [Tooltip("击打后自动复位延迟（秒），0=不自动复位")]
        public float resetDelay = 2f;

        UnityEngine.UI.Button btn;
        bool playing;
        Vector3 savedCuePos;
        Vector3 savedTargetPos;

        void Start()
        {
            btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.AddListener(OnClick);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && !playing) OnClick();
        }

        void OnClick()
        {
            if (playing) return;
            if (aimManager == null || shotSimulator == null || cueBall == null || targetBall == null || table == null) return;

            Vector3 aimDir;
            if (aimManager.hasManualAim && aimManager.manualAimDir.sqrMagnitude > 1e-6f)
            {
                aimDir = aimManager.manualAimDir.normalized;
            }
            else if (aimManager.currentPocket != null)
            {
                var r = AimSolver.Compute(cueBall.Center, targetBall.Center, aimManager.currentPocket.Position, table.ballRadius);
                if (!r.solvable) return;
                aimDir = (r.ghostBallCenter - cueBall.Center).normalized;
            }
            else return;

            var sim = shotSimulator.Run(cueBall.Center, targetBall.Center, aimDir, table.ballRadius, table);

            savedCuePos = cueBall.Center;
            savedTargetPos = targetBall.Center;

            StartCoroutine(PlayShot(sim));
        }

        IEnumerator PlayShot(SimulationResult sim)
        {
            playing = true;
            if (btn != null) btn.interactable = false;

            // Hide all aim visuals during the shot — ghost ball, aim lines, trajectory lines,
            // end-position ghost. AimManager drives these every frame, so disable it to freeze
            // Hide all aim visuals during the shot — ghost ball, aim lines, trajectory lines,
            // end-position ghost. Disable AimManager so it doesn't re-show them while balls move.
            bool aimWasEnabled = aimManager != null && aimManager.enabled;
            if (aimManager != null)
            {
                aimManager.enabled = false;
                if (aimManager.ghostRenderer != null) aimManager.ghostRenderer.Hide();
                if (aimManager.aimLineRenderer != null) aimManager.aimLineRenderer.Hide();
                if (aimManager.pathRenderer != null) aimManager.pathRenderer.Hide();
                if (aimManager.cuePathRenderer != null) aimManager.cuePathRenderer.Hide();
                if (aimManager.endRenderer != null) aimManager.endRenderer.Hide();
            }

            // Phase 1: cue ball approaches along aim line (cue → ghost).
            // Target ball stays still. Only cue ball is moving.
            Vector3 cueStart = sim.cueBallTrajectory[0];
            Vector3 ghostPos = sim.cueBallTrajectory.Length >= 2
                ? sim.cueBallTrajectory[1]
                : cueStart;
            Vector3 cueEnd = sim.cueBallTrajectory.Length >= 3
                ? sim.cueBallTrajectory[2]
                : ghostPos;

            Vector3 targetStart = sim.targetBallTrajectory[0];
            Vector3 targetEnd = sim.targetBallTrajectory[sim.targetBallTrajectory.Length - 1];

            float approachDist = Vector3.Distance(cueStart, ghostPos);
            float cueAfterDist = Vector3.Distance(ghostPos, cueEnd);
            float targetAfterDist = Vector3.Distance(targetStart, targetEnd);

            bool didHit = sim.state != TargetBallEndState.NotHit;

            // Constant linear speed across the whole animation so velocities look consistent.
            float totalDist = approachDist + Mathf.Max(cueAfterDist, targetAfterDist);
            if (totalDist < 0.001f) totalDist = 1f;
            float speed = totalDist / animDuration;

            // Phase 1: approach (cue moves alone)
            if (approachDist > 0.001f)
            {
                float t1Duration = approachDist / speed;
                float t1 = 0f;
                while (t1 < t1Duration)
                {
                    t1 += Time.deltaTime;
                    float u = Mathf.Clamp01(t1 / t1Duration);
                    cueBall.transform.position = Vector3.Lerp(cueStart, ghostPos, u);
                    yield return null;
                }
                cueBall.transform.position = ghostPos;
            }

            // Phase 2: post-collision. Both balls move simultaneously at the same speed.
            if (didHit && (cueAfterDist > 0.001f || targetAfterDist > 0.001f))
            {
                float maxAfterDist = Mathf.Max(cueAfterDist, targetAfterDist);
                float t2Duration = maxAfterDist / speed;
                float t2 = 0f;
                while (t2 < t2Duration)
                {
                    t2 += Time.deltaTime;
                    float dist = t2 * speed;
                    if (cueAfterDist > 0.001f)
                    {
                        float uCue = Mathf.Clamp01(dist / cueAfterDist);
                        cueBall.transform.position = Vector3.Lerp(ghostPos, cueEnd, uCue);
                    }
                    if (targetAfterDist > 0.001f)
                    {
                        float uTgt = Mathf.Clamp01(dist / targetAfterDist);
                        targetBall.transform.position = Vector3.Lerp(targetStart, targetEnd, uTgt);
                    }
                    yield return null;
                }
            }

            cueBall.transform.position = cueEnd;
            targetBall.transform.position = targetEnd;

            if (resetDelay > 0f)
            {
                yield return new WaitForSeconds(resetDelay);
                cueBall.MoveTo(savedCuePos);
                targetBall.MoveTo(savedTargetPos);
            }

            // Restore aim visuals
            if (aimManager != null)
            {
                aimManager.enabled = aimWasEnabled;
                aimManager.ForceRefresh();
            }

            playing = false;
            if (btn != null) btn.interactable = true;
        }

        static float PathLength(Vector3[] path)
        {
            if (path == null || path.Length < 2) return 0f;
            float len = 0f;
            for (int i = 1; i < path.Length; i++)
                len += Vector3.Distance(path[i - 1], path[i]);
            return len;
        }
    }
}
