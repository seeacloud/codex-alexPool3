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
    /// Attach PhysicsLineToggle to _GameManager and wire both trajectory path renderers.
    /// Press V at runtime to show/hide the orange + blue prediction lines.
    /// Menu: PoolAimTrainer -> Attach Physics Line Toggle (V)
    /// </summary>
    public static class AttachPhysicsLineToggle
    {
        [MenuItem("PoolAimTrainer/Attach Physics Line Toggle (V)")]
        public static void Attach()
        {
            var gm = GameObject.Find("_GameManager");
            var vis = GameObject.Find("_Visualization");
            if (gm == null || vis == null)
            {
                EditorUtility.DisplayDialog("Missing",
                    "_GameManager or _Visualization not found. Run Session C + Phase 4 first.",
                    "OK");
                return;
            }

            var toggle = gm.GetComponent<PhysicsLineToggle>();
            if (toggle == null) toggle = gm.AddComponent<PhysicsLineToggle>();
            toggle.targetPath = vis.GetComponent<TargetBallPathRenderer>();
            toggle.cuePath = vis.GetComponent<CueBallPathRenderer>();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "PhysicsLineToggle attached. Press V at runtime to toggle orange+blue lines.",
                "OK");
        }
    }
}
#endif
