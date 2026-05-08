#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PoolAimTrainer.Core;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.Visualization;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Phase 4 (E1) setup:
    ///  - Creates GhostBallEnd.prefab (green transparent sphere) if missing.
    ///  - Adds HiddenSceneManager + ShotSimulator to _GameManager.
    ///  - Adds TargetBallEndRenderer to _Visualization and wires the green prefab.
    ///  - Links the new components into AimManager's new fields.
    ///
    /// Menu: PoolAimTrainer -> Setup Phase 4 TargetBall End Ghost
    /// </summary>
    public static class Phase4Setup
    {
        const string GhostEndPrefabPath = "Assets/Prefabs/GhostBallEnd.prefab";
        const string GhostEndMatPath = "Assets/Prefabs/GhostEndMat.mat";
        const float BallDiameter = 0.0572f;

        [MenuItem("PoolAimTrainer/Setup Phase 4 TargetBall End Ghost")]
        public static void Setup()
        {
            var gm = GameObject.Find("_GameManager");
            var vis = GameObject.Find("_Visualization");
            if (gm == null || vis == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Missing" + ": " + "_GameManager or _Visualization not found. Run Session C first.");
                return;
            }
            var aim = gm.GetComponent<AimManager>();
            if (aim == null)
            {
                UnityEngine.Debug.Log("[Editor] " + "Missing AimManager" + ": " + "AimManager not found on _GameManager.");
                return;
            }

            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");

            var endMat = AssetDatabase.LoadAssetAtPath<Material>(GhostEndMatPath);
            if (endMat == null) endMat = CreateTransparentURPMaterial(new Color(0.2f, 1f, 0.2f, 0.45f), GhostEndMatPath);

            var endPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GhostEndPrefabPath);
            if (endPrefab == null) endPrefab = CreateGhostBallEndPrefab(endMat);

            var sim = gm.GetComponent<ShotSimulator>();
            if (sim == null) sim = gm.AddComponent<ShotSimulator>();

            var endRenderer = vis.GetComponent<TargetBallEndRenderer>();
            if (endRenderer == null) endRenderer = vis.AddComponent<TargetBallEndRenderer>();
            endRenderer.ghostPrefab = endPrefab;

            aim.shotSimulator = sim;
            aim.endRenderer = endRenderer;
            EditorUtility.SetDirty(aim);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            UnityEngine.Debug.Log("[Editor] " + "Done" + ": " + "Phase 4 ready: HiddenSceneManager + ShotSimulator + TargetBallEndRenderer wired. Press Play and drag balls to see the green final-position ghost.");
        }

        static Material CreateTransparentURPMaterial(Color color, string path)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            AssetDatabase.CreateAsset(m, path);
            AssetDatabase.SaveAssets();
            return m;
        }

        static GameObject CreateGhostBallEndPrefab(Material mat)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "GhostBallEnd";
            temp.transform.localScale = Vector3.one * BallDiameter;
            temp.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(temp.GetComponent<SphereCollider>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, GhostEndPrefabPath);
            Object.DestroyImmediate(temp);
            return prefab;
        }
    }
}
#endif
