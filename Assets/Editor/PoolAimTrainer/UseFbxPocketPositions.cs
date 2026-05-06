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
    /// Reads the actual pocket mesh positions from the PoolSet FBX hierarchy (children of
    /// Pool-Table named "Pocket", "Pocket.001"..."Pocket.005") and uses them as the
    /// authoritative positions for our PocketMarker GameObjects (Pocket_TL, Pocket_TR,
    /// Pocket_BL, Pocket_BR, Pocket_TM, Pocket_BM). Also updates TableController's
    /// playfieldHalf* so that the ball clamp matches the real table bounds.
    ///
    /// Menu: PoolAimTrainer -> Use FBX Pocket Positions
    /// </summary>
    public static class UseFbxPocketPositions
    {
        [MenuItem("PoolAimTrainer/Use FBX Pocket Positions")]
        public static void Apply()
        {
            var table = GameObject.Find("Pool-Table");
            if (table == null)
            {
                EditorUtility.DisplayDialog("Missing",
                    "Pool-Table not found in scene.", "OK");
                return;
            }

            var fbx = CollectFbxPockets(table.transform);
            if (fbx.Count < 6)
            {
                EditorUtility.DisplayDialog("Not enough FBX pockets",
                    $"Expected 6 FBX pocket children under Pool-Table, found {fbx.Count}.",
                    "OK");
                return;
            }

            var mapping = ClassifyByQuadrant(fbx);

            int applied = 0;
            applied += ApplyPosition("Pocket_TL", mapping["TL"]);
            applied += ApplyPosition("Pocket_TR", mapping["TR"]);
            applied += ApplyPosition("Pocket_BL", mapping["BL"]);
            applied += ApplyPosition("Pocket_BR", mapping["BR"]);
            applied += ApplyPosition("Pocket_TM", mapping["TM"]);
            applied += ApplyPosition("Pocket_BM", mapping["BM"]);

            // Update TableController half-extents based on actual pocket positions.
            var tc = table.GetComponent<TableController>();
            if (tc != null)
            {
                float maxX = 0f, maxZ = 0f;
                foreach (var p in fbx)
                {
                    var w = p.position;
                    if (Mathf.Abs(w.x) > maxX) maxX = Mathf.Abs(w.x);
                    if (Mathf.Abs(w.z) > maxZ) maxZ = Mathf.Abs(w.z);
                }
                Undo.RecordObject(tc, "Update TableController bounds");
                tc.playfieldHalfLength = maxX;
                tc.playfieldHalfWidth = maxZ;
                EditorUtility.SetDirty(tc);
            }

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                $"Applied {applied}/6 pocket positions from FBX. " +
                (tc != null ? $"TableController bounds: halfLength={tc.playfieldHalfLength:F3}, halfWidth={tc.playfieldHalfWidth:F3}." : ""),
                "OK");
        }

        static List<Transform> CollectFbxPockets(Transform tableRoot)
        {
            var list = new List<Transform>();
            foreach (Transform child in tableRoot)
            {
                // Our markers are named Pocket_TL etc. FBX children are Pocket, Pocket.001...
                if (child.name.StartsWith("Pocket") && !child.name.Contains("_"))
                    list.Add(child);
            }
            return list;
        }

        static Dictionary<string, Transform> ClassifyByQuadrant(List<Transform> pockets)
        {
            // Sort by X then Z to reason about layout.
            // Classification rule (American 8-ft table, camera origin at table center):
            //   Two corners on the +Z (top) side: most negative X -> TL, most positive X -> TR
            //   Two corners on the -Z (bottom) side: most negative X -> BL, most positive X -> BR
            //   Two mid pockets at X close to 0: +Z -> TM, -Z -> BM
            // We pick the two with smallest |X| as mid pockets; the remaining four are corners.
            pockets.Sort((a, b) => Mathf.Abs(a.position.x).CompareTo(Mathf.Abs(b.position.x)));
            var midPockets = new List<Transform>(2) { pockets[0], pockets[1] };
            var cornerPockets = new List<Transform>(4) { pockets[2], pockets[3], pockets[4], pockets[5] };

            midPockets.Sort((a, b) => b.position.z.CompareTo(a.position.z));
            var tm = midPockets[0];
            var bm = midPockets[1];

            var topCorners = new List<Transform>();
            var bottomCorners = new List<Transform>();
            foreach (var c in cornerPockets)
            {
                if (c.position.z >= 0f) topCorners.Add(c);
                else bottomCorners.Add(c);
            }
            topCorners.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            bottomCorners.Sort((a, b) => a.position.x.CompareTo(b.position.x));

            var map = new Dictionary<string, Transform>();
            if (topCorners.Count == 2)
            {
                map["TL"] = topCorners[0];
                map["TR"] = topCorners[1];
            }
            if (bottomCorners.Count == 2)
            {
                map["BL"] = bottomCorners[0];
                map["BR"] = bottomCorners[1];
            }
            map["TM"] = tm;
            map["BM"] = bm;
            return map;
        }

        static int ApplyPosition(string markerName, Transform fbxSource)
        {
            if (fbxSource == null) return 0;
            var go = GameObject.Find(markerName);
            if (go == null) return 0;
            Undo.RecordObject(go.transform, "Apply FBX pocket position");
            // Use fbx mesh world position; keep y at ball radius so the indicator sits on the surface.
            var p = fbxSource.position;
            p.y = 0.0286f;
            go.transform.position = p;
            return 1;
        }
    }
}
#endif
