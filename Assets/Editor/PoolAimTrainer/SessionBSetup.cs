#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Interaction;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Attaches DragInput (on new _InputManager empty GO) and CameraOrbit (on Main Camera)
    /// to the currently active scene. Expects Session A to have been run first
    /// (needs a GameObject named "Pool-Table" to serve as camera pivot).
    /// Menu: PoolAimTrainer -> Setup Session B Input &amp; Camera
    /// </summary>
    public static class SessionBSetup
    {
        [MenuItem("PoolAimTrainer/Setup Session B Input && Camera")]
        public static void Setup()
        {
            var table = GameObject.Find("Pool-Table");
            if (table == null)
            {
                EditorUtility.DisplayDialog("Missing Pool-Table",
                    "Run 'Setup Session A Scene' first to create Pool-Table in the scene.",
                    "OK");
                return;
            }

            var inputManager = GameObject.Find("_InputManager");
            if (inputManager == null)
            {
                inputManager = new GameObject("_InputManager");
                Undo.RegisterCreatedObjectUndo(inputManager, "Create _InputManager");
            }
            if (inputManager.GetComponent<DragInput>() == null)
            {
                var drag = inputManager.AddComponent<DragInput>();
                drag.cam = Camera.main;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("No Main Camera",
                    "No Camera with tag 'MainCamera' found in the scene. Aborting.",
                    "OK");
                return;
            }
            var orbit = cam.GetComponent<CameraOrbit>();
            if (orbit == null) orbit = cam.gameObject.AddComponent<CameraOrbit>();
            orbit.pivot = table.transform;

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            EditorUtility.DisplayDialog("Done",
                "DragInput attached to _InputManager and CameraOrbit attached to Main Camera (pivot = Pool-Table). Press Play to test.",
                "OK");
        }
    }
}
#endif
