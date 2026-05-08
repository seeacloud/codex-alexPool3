#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Places every PocketMarker at its intended WORLD position, bypassing any parent scale.
    /// Run this after FixScaleSetup if pockets ended up squished toward the table center.
    ///
    /// Menu: PoolAimTrainer -> Fix Pocket Positions
    /// </summary>
    public static class FixPocketPositions
    {
        const float HalfLength = 1.12f;
        const float HalfWidth = 0.56f;
        const float BallRadius = 0.0286f;

        [MenuItem("PoolAimTrainer/Fix Pocket Positions")]
        public static void Fix()
        {
            // Known world coordinates for the six pockets (American 8-ft table).
            var targets = new (string name, Vector3 worldPos)[]
            {
                ("Pocket_TL", new Vector3(-HalfLength, BallRadius,  HalfWidth)),
                ("Pocket_TR", new Vector3( HalfLength, BallRadius,  HalfWidth)),
                ("Pocket_BL", new Vector3(-HalfLength, BallRadius, -HalfWidth)),
                ("Pocket_BR", new Vector3( HalfLength, BallRadius, -HalfWidth)),
                ("Pocket_TM", new Vector3(         0f, BallRadius,  HalfWidth)),
                ("Pocket_BM", new Vector3(         0f, BallRadius, -HalfWidth)),
            };

            int fixedCount = 0;
            foreach (var t in targets)
            {
                var go = GameObject.Find(t.name);
                if (go == null) continue;
                Undo.RecordObject(go.transform, "Fix Pocket Position");
                // Setting world position (transform.position) ignores parent scale.
                go.transform.position = t.worldPos;
                // Neutralise any inherited non-uniform scale on the pocket itself so its
                // runtime indicator sphere is round.
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = new Vector3(
                    1f / SafeParentScale(go.transform).x,
                    1f / SafeParentScale(go.transform).y,
                    1f / SafeParentScale(go.transform).z);
                fixedCount++;
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.Debug.Log("[Editor] " + "Done" + ": " + $"Fixed {fixedCount} pockets to real-world corner/mid positions.");
        }

        static Vector3 SafeParentScale(Transform t)
        {
            if (t.parent == null) return Vector3.one;
            var s = t.parent.lossyScale;
            if (Mathf.Abs(s.x) < 1e-6f) s.x = 1f;
            if (Mathf.Abs(s.y) < 1e-6f) s.y = 1f;
            if (Mathf.Abs(s.z) < 1e-6f) s.z = 1f;
            return s;
        }
    }
}
#endif
