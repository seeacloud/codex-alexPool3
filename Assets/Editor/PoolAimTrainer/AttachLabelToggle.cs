#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Attaches PocketLabelToggle to _GameManager. Press L at runtime to toggle labels.
    /// Menu: PoolAimTrainer -> Attach Label Toggle
    /// </summary>
    public static class AttachLabelToggle
    {
        [MenuItem("PoolAimTrainer/Attach Label Toggle (press L to show/hide)")]
        public static void Attach()
        {
            var gm = GameObject.Find("_GameManager");
            if (gm == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "_GameManager not found. Run Session C first.");
                return;
            }
            if (gm.GetComponent<PocketLabelToggle>() == null)
                gm.AddComponent<PocketLabelToggle>();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.Debug.Log("[Editor] " + "Done" + ": " + "PocketLabelToggle attached. Press L at runtime to toggle pocket number labels.");
        }
    }
}
#endif
