#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Capture the current Game view as a PNG and save it to docs/test-screenshots/.
    /// Used for automated verification by the AI assistant: every test run should
    /// leave a file on disk that the user can human-review.
    ///
    /// Menu: PoolAimTrainer -> Capture Game View
    ///
    /// The filename must be set via SessionState key before invoking, or a
    /// timestamped default is used.
    /// </summary>
    public static class GameViewCapture
    {
        const string OUT_DIR = "docs/test-screenshots";
        const string FILENAME_KEY = "PoolAimTrainer.Capture.Filename";

        public static void SetNextFilename(string filename)
        {
            SessionState.SetString(FILENAME_KEY, filename);
        }

        [MenuItem("PoolAimTrainer/Capture Game View")]
        public static void Capture()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", OUT_DIR));
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filename = SessionState.GetString(FILENAME_KEY, "");
            SessionState.EraseString(FILENAME_KEY);
            if (string.IsNullOrEmpty(filename))
                filename = $"capture-{System.DateTime.Now:yyyyMMdd-HHmmss}.png";
            if (!filename.EndsWith(".png")) filename += ".png";

            string fullPath = Path.Combine(dir, filename);
            ScreenCapture.CaptureScreenshot(fullPath);
            UnityEngine.Debug.Log($"[GameViewCapture] Saved screenshot: {fullPath}");
        }
    }
}
#endif
