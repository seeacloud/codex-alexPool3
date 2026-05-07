#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PoolAimTrainer.EditorTools
{
    public static class RemoveToolbarButtons
    {
        [MenuItem("PoolAimTrainer/Remove H G L V Buttons")]
        public static void Remove()
        {
            string[] names = { "BtnH", "BtnG", "BtnL", "BtnV" };
            int removed = 0;
            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                    removed++;
                }
            }

            if (removed > 0)
            {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            EditorUtility.DisplayDialog("Done", $"Removed {removed} toolbar buttons.", "OK");
        }
    }
}
#endif
