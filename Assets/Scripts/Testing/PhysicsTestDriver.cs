#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Testing
{
    /// <summary>
    /// Drives automated scenario playback in Play mode: places the cue/target
    /// balls, waits a few frames so the aim pipeline updates, captures a PNG of
    /// the Game view to disk, then advances. When the list is exhausted, writes
    /// a manifest and exits Play mode.
    ///
    /// Output:
    ///   &lt;project-parent&gt;/docs/test-screenshots/&lt;runId&gt;/&lt;scenarioName&gt;.png
    ///   &lt;project-parent&gt;/docs/test-screenshots/&lt;runId&gt;/manifest.md
    ///
    /// Lives in the Runtime assembly (behind UNITY_EDITOR so it's stripped from
    /// builds). Editor-assembly MonoBehaviours don't round-trip through the
    /// serialize/deserialize that Play mode entry triggers.
    /// </summary>
    public class PhysicsTestDriver : MonoBehaviour
    {
        [System.Serializable]
        public class Scenario
        {
            public string name;
            public Vector3 cuePos;
            public Vector3 targetPos;
            public Vector3 manualAimDir;
            public int pocketNumber;
            public string note;
        }

        public AimManager aimManager;
        public TableController table;
        public List<Scenario> scenarios = new List<Scenario>();

        public bool autoRun = false;
        public int settleFrames = 4;
        public float postCaptureDelay = 0.5f;

        const string OUT_REL = "docs/test-screenshots";
        public string runId = "";

        void Start()
        {
            if (!autoRun) return;
            if (aimManager == null || table == null)
            {
                Debug.LogError("[PhysicsTestDriver] aimManager or table not wired");
                EditorApplication.isPlaying = false;
                return;
            }
            if (string.IsNullOrEmpty(runId))
                runId = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");

            aimManager.showGhostBall = true;

            // UI (right-side panel, HUD, etc.) covers the right half of the
            // table — exactly where most test scenarios put the target ball.
            // Hide it for the duration of the run so screenshots actually show
            // the physics.
            foreach (var canvas in FindObjectsOfType<Canvas>())
                canvas.enabled = false;

            StartCoroutine(RunAll());
        }

        IEnumerator RunAll()
        {
            string outDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", OUT_REL, runId));
            Directory.CreateDirectory(outDir);
            Debug.Log($"[PhysicsTestDriver] run started, scenarios={scenarios.Count}, outDir={outDir}");

            var manifest = new System.Text.StringBuilder();
            manifest.AppendLine($"# PhysicsTestDriver run: {runId}");
            manifest.AppendLine($"- date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            manifest.AppendLine($"- scenario count: {scenarios.Count}");
            manifest.AppendLine();

            for (int i = 0; i < scenarios.Count; i++)
                yield return RunOne(scenarios[i], outDir, manifest);

            File.WriteAllText(Path.Combine(outDir, "manifest.md"), manifest.ToString());
            Debug.Log($"[PhysicsTestDriver] run finished, manifest at {outDir}/manifest.md");
            yield return new WaitForSeconds(0.3f);
            EditorApplication.isPlaying = false;
        }

        IEnumerator RunOne(Scenario s, string outDir, System.Text.StringBuilder manifest)
        {
            Debug.Log($"[PhysicsTestDriver] scenario: {s.name}");

            aimManager.cueBall.MoveTo(s.cuePos);
            aimManager.targetBall.MoveTo(s.targetPos);

            PocketMarker pm = null;
            if (s.pocketNumber >= 1)
            {
                foreach (var p in table.Pockets)
                    if (p.pocketNumber == s.pocketNumber) { pm = p; break; }
            }
            if (pm != null) aimManager.SetUserPocket(pm);
            else aimManager.ClearUserPocket();

            if (s.manualAimDir.sqrMagnitude > 1e-6f)
                aimManager.SetManualAimDir(s.manualAimDir);
            else
                aimManager.ClearManualAim();

            aimManager.ForceRefresh();

            for (int f = 0; f < settleFrames; f++)
                yield return null;

            string fname = SanitizeFilename(s.name) + ".png";
            string fullPath = Path.Combine(outDir, fname);
            ScreenCapture.CaptureScreenshot(fullPath);
            yield return new WaitForSeconds(postCaptureDelay);

            float deadline = Time.realtimeSinceStartup + 2f;
            while (!File.Exists(fullPath) && Time.realtimeSinceStartup < deadline)
                yield return null;

            bool ok = File.Exists(fullPath);
            long size = ok ? new FileInfo(fullPath).Length : 0;

            manifest.AppendLine($"## {s.name}");
            manifest.AppendLine($"- file: `{fname}`");
            manifest.AppendLine($"- cue: {Fmt(s.cuePos)}");
            manifest.AppendLine($"- target: {Fmt(s.targetPos)}");
            manifest.AppendLine($"- pocket#: {s.pocketNumber} ({(pm != null ? pm.name : "auto")})");
            if (s.manualAimDir.sqrMagnitude > 1e-6f)
                manifest.AppendLine($"- manualAimDir: {Fmt(s.manualAimDir)}");
            manifest.AppendLine($"- note: {s.note}");
            manifest.AppendLine($"- captured: {ok} ({size} bytes)");
            manifest.AppendLine();
            Debug.Log($"[PhysicsTestDriver]   {s.name} -> captured={ok} bytes={size} path={fullPath}");
        }

        static string Fmt(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";

        static string SanitizeFilename(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "scenario";
            foreach (char c in Path.GetInvalidFileNameChars())
                raw = raw.Replace(c, '_');
            return raw;
        }
    }
}
#endif
