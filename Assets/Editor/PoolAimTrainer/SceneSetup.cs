#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// One-click scene builder for Session A of MVP:
    /// creates MainScene with Pool-Table, 6 pockets, CueBall, TargetBall.
    /// Menu: PoolAimTrainer -> Setup Session A Scene
    /// </summary>
    public static class SceneSetup
    {
        const string ScenePath = "Assets/Scenes/MainScene.unity";
        const string TableFbxPath = "Assets/PoolSet/Mesh/Pool-Table.fbx";
        const string CueBallMatPath = "Assets/PoolSet/Materials/Balls/Cue Ball.mat";
        const string TargetBallMatPath = "Assets/PoolSet/Materials/Balls/pool-ball 1.mat";

        const float BallRadius = 0.0286f;
        const float BallDiameter = BallRadius * 2f;
        const float HalfLength = 1.12f;
        const float HalfWidth = 0.56f;

        [MenuItem("PoolAimTrainer/Setup Session A Scene")]
        public static void SetupSceneA()
        {
            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var tableFbx = AssetDatabase.LoadAssetAtPath<GameObject>(TableFbxPath);
            if (tableFbx == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Missing asset" + ": " + $"Pool-Table FBX not found at {TableFbxPath}. Aborting.");
                return;
            }

            var table = (GameObject)PrefabUtility.InstantiatePrefab(tableFbx);
            table.name = "Pool-Table";
            table.transform.position = Vector3.zero;
            var tc = table.AddComponent<TableController>();
            tc.playfieldHalfLength = HalfLength - BallRadius;
            tc.playfieldHalfWidth = HalfWidth - BallRadius;
            tc.ballRadius = BallRadius;
            if (TableMeshBoundsEstimator.TryEstimateBumperNoseBounds(
                table.transform,
                out float halfLength,
                out float halfWidth))
            {
                tc.playfieldHalfLength = halfLength;
                tc.playfieldHalfWidth = halfWidth;
            }

            CreatePocket("Pocket_TL", table.transform, new Vector3(-HalfLength, BallRadius,  HalfWidth));
            CreatePocket("Pocket_TR", table.transform, new Vector3( HalfLength, BallRadius,  HalfWidth));
            CreatePocket("Pocket_BL", table.transform, new Vector3(-HalfLength, BallRadius, -HalfWidth));
            CreatePocket("Pocket_BR", table.transform, new Vector3( HalfLength, BallRadius, -HalfWidth));
            CreatePocket("Pocket_TM", table.transform, new Vector3(         0f, BallRadius,  HalfWidth));
            CreatePocket("Pocket_BM", table.transform, new Vector3(         0f, BallRadius, -HalfWidth));

            var cue = CreateBall("CueBall", new Vector3(-0.5f, BallRadius, 0f),
                                 CueBallMatPath, BallKind.CueBall, tc);
            var target = CreateBall("TargetBall", new Vector3(0.5f, BallRadius, 0f),
                                    TargetBallMatPath, BallKind.TargetBall, tc);
            target.AddComponent<SelectableBall>();

            if (Camera.main != null)
            {
                var cam = Camera.main.transform;
                cam.position = new Vector3(0f, 1.6f, -1.6f);
                cam.rotation = Quaternion.Euler(45f, 0f, 0f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            UnityEngine.Debug.Log("[Editor] " + "Done" + ": " + "MainScene created at Assets/Scenes/MainScene.unity with table, 6 pockets, CueBall, TargetBall.");
        }

        static void CreatePocket(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.AddComponent<PocketMarker>();
        }

        static GameObject CreateBall(string name, Vector3 pos, string matPath,
                                     BallKind kind, TableController table)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = name;
            ball.transform.position = pos;
            ball.transform.localScale = Vector3.one * BallDiameter;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
                ball.GetComponent<Renderer>().sharedMaterial = mat;

            var bc = ball.AddComponent<BallController>();
            bc.kind = kind;
            bc.table = table;
            return ball;
        }
    }
}
#endif
