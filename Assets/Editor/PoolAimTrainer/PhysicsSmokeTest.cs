#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Testing;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// One-click physics smoke test. Installs PhysicsTestDriver into the open
    /// scene with a fixed list of scenarios, then toggles Play mode on. The
    /// driver itself toggles Play mode off when it's done.
    ///
    /// Menu: PoolAimTrainer -> Run Physics Smoke Test
    ///
    /// Screenshots land in:
    ///   &lt;project-parent&gt;/docs/test-screenshots/&lt;yyyyMMdd-HHmmss&gt;/
    /// </summary>
    public static class PhysicsSmokeTest
    {
        const string DRIVER_NAME = "_PhysicsTestDriver";

        [MenuItem("PoolAimTrainer/Run Physics Smoke Test")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PhysicsSmokeTest] already in Play mode; aborting");
                return;
            }

            var aim = Object.FindObjectOfType<AimManager>();
            var table = Object.FindObjectOfType<TableController>();
            if (aim == null || table == null)
            {
                Debug.LogError("[PhysicsSmokeTest] scene missing AimManager or TableController");
                return;
            }

            // Remove any leftover driver from a previous run.
            var existing = GameObject.Find(DRIVER_NAME);
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject(DRIVER_NAME);
            var driver = go.AddComponent<PhysicsTestDriver>();
            driver.aimManager = aim;
            driver.table = table;
            driver.autoRun = true;
            driver.runId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");

            // Scenarios built around the 8ft table layout (playfield extents
            // roughly ±1.12 in x, ±0.56 in z; ball radius 0.0286).
            // Pocket numbers follow the existing PocketMarker.pocketNumber
            // assignments in the scene (TR=3 / TM=2 / TL=1 etc — if the numbers
            // differ, scenarios fall back to auto-selection).
            float R = table.ballRadius;

            driver.scenarios.Add(new PhysicsTestDriver.Scenario {
                name = "01_direct_pocket_TR",
                cuePos = new Vector3(0.0f, R, -0.2f),
                targetPos = new Vector3(0.8f, R, 0.35f),
                pocketNumber = 0,
                note = "Direct shot toward top-right pocket; ghost should appear at pocket center, orange path present."
            });

            driver.scenarios.Add(new PhysicsTestDriver.Scenario {
                name = "02_rail_skim_no_pocket",
                cuePos = new Vector3(0.4f, R, -0.3f),
                targetPos = new Vector3(0.97f, R, 0.0f),
                manualAimDir = new Vector3(0.1f, 0f, 1f).normalized,
                pocketNumber = 0,
                note = "Target near right rail, aimed parallel-ish along +Z; must stop at rail, NOT pocket."
            });

            driver.scenarios.Add(new PhysicsTestDriver.Scenario {
                name = "03_ghost_near_cushion",
                cuePos = new Vector3(-0.3f, R, -0.1f),
                targetPos = new Vector3(0.85f, R, 0.45f),
                pocketNumber = 0,
                note = "Target very close to corner pocket; verifying green ghost sits at pocket center (not embedded in cushion)."
            });

            driver.scenarios.Add(new PhysicsTestDriver.Scenario {
                name = "04_long_cross_table",
                cuePos = new Vector3(-0.9f, R, -0.3f),
                targetPos = new Vector3(0.5f, R, 0.2f),
                pocketNumber = 0,
                note = "Long cross-table shot; sanity check that trajectory + ghost render at distance."
            });

            EditorSceneManager.MarkSceneDirty(go.scene);

            Debug.Log($"[PhysicsSmokeTest] driver installed, runId={driver.runId}, entering Play mode");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
