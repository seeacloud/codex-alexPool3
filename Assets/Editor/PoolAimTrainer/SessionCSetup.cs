#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.UI;
using PoolAimTrainer.Visualization;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Builds the visualization layer: ghost ball prefab, aim line renderers, UI canvas
    /// with TMP hint panel, and the central AimManager with all fields wired.
    ///
    /// Menu: PoolAimTrainer -> Setup Session C Visualization
    ///
    /// Requires Sessions A and B to have been run first.
    /// Note: Chinese characters in hint panel will appear as squares until Phase 6
    /// Task 6.2 creates a Chinese TMP font asset.
    /// </summary>
    public static class SessionCSetup
    {
        const string GhostPrefabPath = "Assets/Prefabs/GhostBallAim.prefab";
        const string GhostMatPath = "Assets/Prefabs/GhostAimMat.mat";
        const float BallDiameter = 0.0572f;

        [MenuItem("PoolAimTrainer/Setup Session C Visualization")]
        public static void Setup()
        {
            var table = GameObject.Find("Pool-Table");
            var cue = GameObject.Find("CueBall");
            var target = GameObject.Find("TargetBall");
            if (table == null || cue == null || target == null)
            {
                EditorUtility.DisplayDialog("Scene not ready",
                    "Missing Pool-Table / CueBall / TargetBall. Run Sessions A + B first.", "OK");
                return;
            }
            var tc = table.GetComponent<TableController>();
            var cueBC = cue.GetComponent<BallController>();
            var targetBC = target.GetComponent<BallController>();

            if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");

            var ghostMat = AssetDatabase.LoadAssetAtPath<Material>(GhostMatPath);
            if (ghostMat == null) ghostMat = CreateTransparentURPMaterial(new Color(1f, 1f, 1f, 0.4f), GhostMatPath);

            var ghostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GhostPrefabPath);
            if (ghostPrefab == null) ghostPrefab = CreateGhostBallPrefab(ghostMat);

            var vis = GameObject.Find("_Visualization");
            if (vis == null) vis = new GameObject("_Visualization");
            if (vis.GetComponent<GhostBallRenderer>() == null)
                vis.AddComponent<GhostBallRenderer>().ghostPrefab = ghostPrefab;

            var aimLineRenderer = vis.GetComponent<AimLineRenderer>();
            if (aimLineRenderer == null) aimLineRenderer = vis.AddComponent<AimLineRenderer>();

            var canvas = SetupUICanvas();
            var hintPanel = SetupHintPanel(canvas);

            var gm = GameObject.Find("_GameManager");
            if (gm == null) gm = new GameObject("_GameManager");
            var aim = gm.GetComponent<AimManager>();
            if (aim == null) aim = gm.AddComponent<AimManager>();
            aim.cueBall = cueBC;
            aim.targetBall = targetBC;
            aim.table = tc;
            aim.ghostRenderer = vis.GetComponent<GhostBallRenderer>();
            aim.aimLineRenderer = aimLineRenderer;
            aim.hintPanel = hintPanel;

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                "Visualization layer ready: GhostBallRenderer, AimLineRenderer, HintPanel, AimManager all wired. Press Play.",
                "OK");
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

        static GameObject CreateGhostBallPrefab(Material mat)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "GhostBallAim";
            temp.transform.localScale = Vector3.one * BallDiameter;
            var rend = temp.GetComponent<Renderer>();
            rend.sharedMaterial = mat;
            Object.DestroyImmediate(temp.GetComponent<SphereCollider>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, GhostPrefabPath);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        static Canvas SetupUICanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null) return existing;
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            return canvas;
        }

        static HintPanel SetupHintPanel(Canvas canvas)
        {
            var existing = canvas.GetComponentInChildren<HintPanel>();
            if (existing != null) return existing;

            var textGO = new GameObject("HintText");
            textGO.transform.SetParent(canvas.transform, false);
            var rt = textGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(20f, 20f);
            rt.sizeDelta = new Vector2(900f, 160f);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 36f;
            tmp.color = Color.white;
            tmp.text = "拖动主球或目标球试试";

            var hp = canvas.gameObject.GetComponent<HintPanel>();
            if (hp == null) hp = canvas.gameObject.AddComponent<HintPanel>();
            hp.text = tmp;
            return hp;
        }
    }
}
#endif
