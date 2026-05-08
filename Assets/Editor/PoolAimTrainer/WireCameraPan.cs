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
            if (im == null) { UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "_InputManager not found."); return; }
            var drag = im.GetComponent<DragInput>();
            if (drag == null) { UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "DragInput not found."); return; }
            var cam = Camera.main;
            if (cam == null) { UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "Main Camera not found."); return; }
            var orbit = cam.GetComponent<CameraOrbit>();
            if (orbit == null) { UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "CameraOrbit not found on Main Camera."); return; }
            drag.cameraOrbit = orbit;
            EditorUtility.SetDirty(drag);
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            UnityEngine.Debug.Log("[Editor] " + "Done" + ": " + "DragInput.cameraOrbit wired. Left-drag on empty table area now pans the camera.");
        }
    }
}
#endif
