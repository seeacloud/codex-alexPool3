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
    /// Adds a TargetBallPath LineRenderer (orange, physics trajectory of the object ball)
    /// and links it into AimManager.pathRenderer. Run after Phase 4.
    ///
    /// Menu: PoolAimTrainer -> Setup TargetBall Path Line
    /// </summary>
    public static class SetupTargetBallPath
    {
        [MenuItem("PoolAimTrainer/Setup TargetBall Path Line")]
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

            var lr = CreateOrUpdateLine(vis.transform, "TargetBallPath",
                new Color(1f, 0.55f, 0.15f, 0.95f), width: 0.005f);

            var pathRenderer = vis.GetComponent<TargetBallPathRenderer>();
            if (pathRenderer == null) pathRenderer = vis.AddComponent<TargetBallPathRenderer>();
            pathRenderer.line = lr;
            pathRenderer.decimation = 2;

            var aim = gm.GetComponent<AimManager>();
            if (aim != null)
            {
                aim.pathRenderer = pathRenderer;
                EditorUtility.SetDirty(aim);
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "TargetBallPath (orange) line wired. Press Play and drag balls to see the target ball's physical trajectory in real time.",
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
