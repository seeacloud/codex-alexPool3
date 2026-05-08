#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Reads the actual pocket mesh geometry from the PoolSet FBX hierarchy and uses the
    /// Renderer bounds CENTER (not transform.position, which is the mesh pivot) to place
    /// our PocketMarker GameObjects directly over the hole openings. Also assigns numbers
    /// 1..6 clockwise from top-left (TL=1, TM=2, TR=3, BR=4, BM=5, BL=6).
    ///
    /// Menu: PoolAimTrainer -> Use FBX Pocket Positions
    /// </summary>
    public static class UseFbxPocketPositions
    {
        const float BallRadius = 0.0286f;

        [MenuItem("PoolAimTrainer/Use FBX Pocket Positions")]
        public static void Apply()
        {
            var table = GameObject.Find("Pool-Table");
            if (table == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "Pool-Table not found in scene.");
                return;
            }

            var fbx = CollectFbxPockets(table.transform);
            if (fbx.Count < 6)
            {
                UnityEngine.Debug.Log("[Editor] " + "Not enough FBX pockets" + ": " + $"Expected 6 FBX pocket children under Pool-Table, found {fbx.Count}.");
                return;
            }

            // Resolve each FBX pocket to its true world-space CENTER using Renderer bounds.
            var centers = new List<(Transform fbx, Vector3 center)>();
            foreach (var p in fbx)
            {
                var rend = p.GetComponentInChildren<Renderer>();
                Vector3 center = rend != null ? rend.bounds.center : p.position;
                centers.Add((p, center));
            }

            var mapping = ClassifyByQuadrant(centers);

            // 1=TL, 2=TM, 3=TR, 4=BR, 5=BM, 6=BL (clockwise from top-left)
            int applied = 0;
            applied += ApplyPosition("Pocket_TL", mapping["TL"], pocketNumber: 1);
            applied += ApplyPosition("Pocket_TM", mapping["TM"], pocketNumber: 2);
            applied += ApplyPosition("Pocket_TR", mapping["TR"], pocketNumber: 3);
            applied += ApplyPosition("Pocket_BR", mapping["BR"], pocketNumber: 4);
            applied += ApplyPosition("Pocket_BM", mapping["BM"], pocketNumber: 5);
            applied += ApplyPosition("Pocket_BL", mapping["BL"], pocketNumber: 6);

            // Update TableController bounds from actual pocket spread.
            var tc = table.GetComponent<TableController>();
            if (tc != null)
            {
                float maxX = 0f, maxZ = 0f;
                foreach (var item in centers)
                {
                    if (Mathf.Abs(item.center.x) > maxX) maxX = Mathf.Abs(item.center.x);
                    if (Mathf.Abs(item.center.z) > maxZ) maxZ = Mathf.Abs(item.center.z);
                }
                Undo.RecordObject(tc, "Update TableController bounds");
                tc.playfieldHalfLength = maxX;
                tc.playfieldHalfWidth = maxZ;
                EditorUtility.SetDirty(tc);
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.Debug.Log("[Editor] Done: " +
                $"Applied {applied}/6 pockets numbered 1..6. " +
                (tc != null ? $"Bounds: halfLength={tc.playfieldHalfLength:F3}, halfWidth={tc.playfieldHalfWidth:F3}." : ""));
        }

        static List<Transform> CollectFbxPockets(Transform tableRoot)
        {
            var list = new List<Transform>();
            foreach (Transform child in tableRoot)
            {
                if (child.name.StartsWith("Pocket") && !child.name.Contains("_"))
                    list.Add(child);
            }
            return list;
        }

        static Dictionary<string, Vector3> ClassifyByQuadrant(List<(Transform fbx, Vector3 center)> centers)
        {
            // Pick the two smallest-|X| as mid pockets, the rest are corners.
            centers.Sort((a, b) => Mathf.Abs(a.center.x).CompareTo(Mathf.Abs(b.center.x)));
            var midCenters = new List<Vector3> { centers[0].center, centers[1].center };
            var cornerCenters = new List<Vector3>
            {
                centers[2].center, centers[3].center, centers[4].center, centers[5].center
            };

            midCenters.Sort((a, b) => b.z.CompareTo(a.z));
            var tm = midCenters[0];
            var bm = midCenters[1];

            var top = new List<Vector3>();
            var bottom = new List<Vector3>();
            foreach (var c in cornerCenters)
            {
                if (c.z >= 0f) top.Add(c);
                else bottom.Add(c);
            }
            top.Sort((a, b) => a.x.CompareTo(b.x));
            bottom.Sort((a, b) => a.x.CompareTo(b.x));

            var map = new Dictionary<string, Vector3>();
            if (top.Count == 2) { map["TL"] = top[0]; map["TR"] = top[1]; }
            if (bottom.Count == 2) { map["BL"] = bottom[0]; map["BR"] = bottom[1]; }
            map["TM"] = tm;
            map["BM"] = bm;
            return map;
        }

        static int ApplyPosition(string markerName, Vector3 worldPos, int pocketNumber)
        {
            var go = GameObject.Find(markerName);
            if (go == null) return 0;
            Undo.RecordObject(go.transform, "Apply FBX pocket position");
            worldPos.y = BallRadius;
            go.transform.position = worldPos;
            var pm = go.GetComponent<PocketMarker>();
            if (pm != null)
            {
                Undo.RecordObject(pm, "Set Pocket Number");
                pm.pocketNumber = pocketNumber;
                EditorUtility.SetDirty(pm);
            }
            return 1;
        }
    }
}
#endif
