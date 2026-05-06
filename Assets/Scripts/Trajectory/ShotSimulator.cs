using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Trajectory
{
    /// <summary>
    /// Runs a fast, synchronous billiards simulation in a hidden physics scene, then
    /// reports the object ball's final state (in pocket / against rail / on table /
    /// still moving / not hit).
    /// </summary>
    public class ShotSimulator : MonoBehaviour
    {
        public HiddenSceneManager hiddenScene;

        [Tooltip("默认击球速度 m/s（作用于主球）")]
        public float defaultImpulse = 3f;

        [Tooltip("仿真固定步长（秒）")]
        public float simStep = 0.02f;

        [Tooltip("最大仿真时长（秒）")]
        public float maxSimSeconds = 3f;

        [Tooltip("球速低于此值视为已停下 m/s")]
        public float stopThreshold = 0.05f;

        [Tooltip("目标球进入此半径则视为入袋 m")]
        public float pocketCatchRadius = 0.07f;

        GameObject hiddenCue, hiddenTarget;
        TableController boundTable;

        public SimulationResult Run(
            Vector3 cuePos, Vector3 targetPos, Vector3 aimDirection,
            float ballRadius, TableController table)
        {
            if (hiddenScene == null) return new SimulationResult { state = TargetBallEndState.NotHit };
            hiddenScene.EnsureCreated();
            EnsureHiddenObjects(ballRadius, table);

            hiddenCue.transform.position = cuePos;
            hiddenTarget.transform.position = targetPos;
            var rbCue = hiddenCue.GetComponent<Rigidbody>();
            var rbTgt = hiddenTarget.GetComponent<Rigidbody>();
            rbCue.velocity = aimDirection.normalized * defaultImpulse;
            rbCue.angularVelocity = Vector3.zero;
            rbTgt.velocity = Vector3.zero;
            rbTgt.angularVelocity = Vector3.zero;

            var traj = new List<Vector3>(256);
            float elapsed = 0f;
            bool targetEverMoved = false;
            var result = new SimulationResult { state = TargetBallEndState.OnTable };

            while (elapsed < maxSimSeconds)
            {
                hiddenScene.Step(simStep);
                elapsed += simStep;
                traj.Add(hiddenTarget.transform.position);

                if (rbTgt.velocity.magnitude > stopThreshold) targetEverMoved = true;

                if (targetEverMoved)
                {
                    foreach (var p in table.Pockets)
                    {
                        if (Vector3.Distance(hiddenTarget.transform.position, p.Position) < pocketCatchRadius)
                        {
                            result.state = TargetBallEndState.InPocket;
                            result.pocketHitPos = p.Position;
                            result.targetBallEndPos = p.Position;
                            result.targetBallTrajectory = traj.ToArray();
                            return result;
                        }
                    }
                }

                if (targetEverMoved
                    && rbCue.velocity.magnitude < stopThreshold
                    && rbTgt.velocity.magnitude < stopThreshold)
                {
                    break;
                }
            }

            result.targetBallEndPos = hiddenTarget.transform.position;
            result.targetBallTrajectory = traj.ToArray();

            if (!targetEverMoved) { result.state = TargetBallEndState.NotHit; return result; }

            if (rbTgt.velocity.magnitude > stopThreshold && elapsed >= maxSimSeconds - simStep)
                result.state = TargetBallEndState.StillMoving;
            else if (IsAgainstRail(result.targetBallEndPos, table, ballRadius))
                result.state = TargetBallEndState.AgainstRail;
            else
                result.state = TargetBallEndState.OnTable;

            return result;
        }

        bool IsAgainstRail(Vector3 pos, TableController table, float ballRadius)
        {
            float eps = 0.02f;
            return Mathf.Abs(Mathf.Abs(pos.x) - (table.playfieldHalfLength - ballRadius)) < eps
                || Mathf.Abs(Mathf.Abs(pos.z) - (table.playfieldHalfWidth - ballRadius)) < eps;
        }

        void EnsureHiddenObjects(float ballRadius, TableController table)
        {
            if (hiddenCue != null && boundTable == table) return;
            boundTable = table;

            hiddenCue = CreateHiddenBall("HSim_Cue", ballRadius);
            hiddenTarget = CreateHiddenBall("HSim_Target", ballRadius);
            SceneManager.MoveGameObjectToScene(hiddenCue, hiddenScene.Scene);
            SceneManager.MoveGameObjectToScene(hiddenTarget, hiddenScene.Scene);
            CreateHiddenTableColliders(table);
        }

        GameObject CreateHiddenBall(string name, float radius)
        {
            var go = new GameObject(name);
            go.transform.localScale = Vector3.one * (radius * 2f);
            var sc = go.AddComponent<SphereCollider>();
            sc.radius = 0.5f;
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.drag = 0.6f;
            rb.angularDrag = 0.4f;
            return go;
        }

        void CreateHiddenTableColliders(TableController table)
        {
            float w = table.playfieldHalfLength;
            float h = table.playfieldHalfWidth;
            float railHeight = table.ballRadius * 2f;
            CreateWall("HSim_Wall_Left", new Vector3(-w, 0f, 0f), new Vector3(0.05f, railHeight, h * 2f));
            CreateWall("HSim_Wall_Right", new Vector3(w, 0f, 0f), new Vector3(0.05f, railHeight, h * 2f));
            CreateWall("HSim_Wall_Top", new Vector3(0f, 0f, h), new Vector3(w * 2f, railHeight, 0.05f));
            CreateWall("HSim_Wall_Bottom", new Vector3(0f, 0f, -h), new Vector3(w * 2f, railHeight, 0.05f));
        }

        void CreateWall(string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            SceneManager.MoveGameObjectToScene(go, hiddenScene.Scene);
        }
    }
}
