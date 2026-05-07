#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Interaction;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Wires DragInput.cameraOrbit so that left-click-drag on empty table area pans the camera.
    /// Menu: PoolAimTrainer -> Wire Camera Pan
    /// </summary>
    public static class WireCameraPan
    {
        [MenuItem("PoolAimTrainer/Wire Camera Pan")]
        public static void Wire()
        {
            var im = GameObject.Find("_InputManager");
            if (im == null) { EditorUtility.DisplayDialog("Missing", "_InputManager not found.", "OK"); return; }
            var drag = im.GetComponent<DragInput>();
            if (drag == null) { EditorUtility.DisplayDialog("Missing", "DragInput not found.", "OK"); return; }
            var cam = Camera.main;
            if (cam == null) { EditorUtility.DisplayDialog("Missing", "Main Camera not found.", "OK"); return; }
            var orbit = cam.GetComponent<CameraOrbit>();
            if (orbit == null) { EditorUtility.DisplayDialog("Missing", "CameraOrbit not found on Main Camera.", "OK"); return; }
            drag.cameraOrbit = orbit;
            EditorUtility.SetDirty(drag);
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Done", "DragInput.cameraOrbit wired. Left-drag on empty table area now pans the camera.", "OK");
        }
    }
}
#endif
