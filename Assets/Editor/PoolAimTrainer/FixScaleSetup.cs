#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Diagnoses the scale mismatch between the imported Pool-Table FBX and our expected
    /// real-world dimensions (2.24m x 1.12m playing surface, 2R = 0.0572m ball diameter).
    ///
    /// Menu: PoolAimTrainer -> Fix Scale To Real World
    ///
    /// Strategy: measure Pool-Table's combined Renderer bounds. Compute the factor needed
    /// to bring its X extent close to 2*HalfLength. Apply that factor to Pool-Table's
    /// localScale. Then reposition pockets and balls (they live outside Pool-Table in
    /// world space) to the correct real-world coordinates.
    /// </summary>
    public static class FixScaleSetup
    {
        const float HalfLength = 1.12f;
        const float HalfWidth = 0.56f;
        const float BallRadius = 0.0286f;
        const float BallDiameter = BallRadius * 2f;

        [MenuItem("PoolAimTrainer/Fix Scale To Real World")]
        public static void Fix()
        {
            var table = GameObject.Find("Pool-Table");
            if (table == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Pool-Table missing" + ": " + "Run 'Setup Session A Scene' first.");
                return;
            }

            Bounds? b = ComputeWorldBounds(table);
            if (b == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "No renderers" + ": " + "Pool-Table has no Renderer components; cannot measure bounds.");
                return;
            }
            var bounds = b.Value;

            float measuredLength = bounds.size.x;
            float targetLength = HalfLength * 2f;
            float factor = targetLength / measuredLength;

            Undo.RecordObject(table.transform, "Fix Pool-Table Scale");
            table.transform.localScale = table.transform.localScale * factor;
            table.transform.position = Vector3.zero;

            foreach (Transform child in table.transform)
            {
                if (child.GetComponent<PocketMarker>() != null)
                {
                    Undo.RecordObject(child, "Fix Pocket Position");
                    Vector3 p = child.localPosition;
                    p.y = BallRadius;
                    child.localPosition = p;
                }
            }

            var cue = GameObject.Find("CueBall");
            if (cue != null)
            {
                Undo.RecordObject(cue.transform, "Fix CueBall");
                cue.transform.position = new Vector3(-0.5f, BallRadius, 0f);
                cue.transform.localScale = Vector3.one * BallDiameter;
            }

            var target = GameObject.Find("TargetBall");
            if (target != null)
            {
                Undo.RecordObject(target.transform, "Fix TargetBall");
                target.transform.position = new Vector3(0.5f, BallRadius, 0f);
                target.transform.localScale = Vector3.one * BallDiameter;
            }

            if (Camera.main != null)
            {
                var camOrbit = Camera.main.GetComponent<PoolAimTrainer.Interaction.CameraOrbit>();
                if (camOrbit != null)
                {
                    camOrbit.distance = 2.5f;
                    camOrbit.pitchDegrees = 55f;
                    camOrbit.yawDegrees = 0f;
                    camOrbit.minDistance = 0.8f;
                    camOrbit.maxDistance = 4f;
                }
                else
                {
                    var cam = Camera.main.transform;
                    cam.position = new Vector3(0f, 1.6f, -1.6f);
                    cam.rotation = Quaternion.Euler(45f, 0f, 0f);
                }
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.Debug.Log("[Editor] " + "Fixed" + ": " + $"Measured table X = {measuredLength:F3}, target = {targetLength:F3}, applied factor = {factor:F4}.\nBalls at expected positions. Press Play to verify proportions.");
        }

        static Bounds? ComputeWorldBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return null;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }
    }
}
#endif
