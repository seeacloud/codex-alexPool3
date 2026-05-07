#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Core;
using PoolAimTrainer.Visualization;

namespace PoolAimTrainer.EditorTools
{
    public static class SetupCueBallPath
    {
        [MenuItem("PoolAimTrainer/Setup CueBall Path Line")]
        public static void Setup()
        {
            var vis = GameObject.Find("_Visualization");
            var gm = GameObject.Find("_GameManager");
            if (vis == null || gm == null)
            {
                EditorUtility.DisplayDialog("Missing",
                    "_Visualization or _GameManager not found. Run Session C + Phase 4 first.",
                    "OK");
                return;
            }

            var renderer = vis.GetComponent<CueBallPathRenderer>();
            if (renderer == null) renderer = vis.AddComponent<CueBallPathRenderer>();
            renderer.decimation = 2;

            var aim = gm.GetComponent<AimManager>();
            if (aim != null)
            {
                aim.cuePathRenderer = renderer;
                EditorUtility.SetDirty(aim);
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "CueBallPath wired (GL.LINES rendering). Press Play and drag balls.",
                "OK");
        }
    }
}
#endif
