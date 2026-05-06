#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Core;
using PoolAimTrainer.Visualization;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Adds a blue CueBallPath LineRenderer showing the cue ball's actual simulated
    /// trajectory (including post-impact deflection). Run after Phase 4.
    ///
    /// Menu: PoolAimTrainer -> Setup CueBall Path Line
    /// </summary>
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

            var lr = CreateOrUpdateLine(vis.transform, "CueBallPath",
                new Color(0.3f, 0.6f, 1f, 0.95f), width: 0.005f);

            var renderer = vis.GetComponent<CueBallPathRenderer>();
            if (renderer == null) renderer = vis.AddComponent<CueBallPathRenderer>();
            renderer.line = lr;
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
                "CueBallPath (blue) line wired. Press Play and drag balls to see the cue ball's actual physics trajectory — it should follow the white aim line until impact, then deflect at ~90° tangent.",
                "OK");
        }

        static LineRenderer CreateOrUpdateLine(Transform parent, string name, Color color, float width)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }
            else go = t.gameObject;

            var lr = go.GetComponent<LineRenderer>();
            if (lr == null) lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = color;
            lr.endColor = color;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = color;
            lr.sharedMaterial = mat;
            lr.enabled = false;
            return lr;
        }
    }
}
#endif
