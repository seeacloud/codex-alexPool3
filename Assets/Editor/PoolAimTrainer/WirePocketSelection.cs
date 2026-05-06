#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Core;
using PoolAimTrainer.Interaction;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Wires AimManager reference into DragInput so that clicking a pocket in the Game view
    /// sets AimManager.userSelectedPocket.
    ///
    /// Menu: PoolAimTrainer -> Wire Pocket Selection
    /// </summary>
    public static class WirePocketSelection
    {
        [MenuItem("PoolAimTrainer/Wire Pocket Selection")]
        public static void Wire()
        {
            var im = GameObject.Find("_InputManager");
            var gm = GameObject.Find("_GameManager");
            if (im == null || gm == null)
            {
                EditorUtility.DisplayDialog("Missing object",
                    "_InputManager or _GameManager not found. Run Sessions B and C first.", "OK");
                return;
            }
            var drag = im.GetComponent<DragInput>();
            var aim = gm.GetComponent<AimManager>();
            if (drag == null || aim == null)
            {
                EditorUtility.DisplayDialog("Missing component",
                    "DragInput or AimManager missing. Run Sessions B and C first.", "OK");
                return;
            }
            drag.aimManager = aim;
            EditorUtility.SetDirty(drag);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "DragInput.aimManager linked. Play, then click a pocket's yellow indicator to lock it as the target.",
                "OK");
        }
    }
}
#endif
