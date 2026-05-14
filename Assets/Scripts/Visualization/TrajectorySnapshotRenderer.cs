using System.Collections.Generic;
using UnityEngine;
using PoolAimTrainer.Trajectory;

namespace PoolAimTrainer.Visualization
{
    public class TrajectorySnapshotRenderer : MonoBehaviour
    {
        const string MARKER_MATERIAL_NAME = "TrajectorySnapshotMaterial";

        [Tooltip("是否显示路径快照")]
        public bool showSnapshots = true;
        [Tooltip("主球路径上的中间快照数量，不包含碰撞关键帧")]
        public int cueSnapshotCount = 4;
        [Tooltip("子球路径上的中间快照数量")]
        public int targetSnapshotCount = 5;
        [Tooltip("普通快照球相对真实球直径的缩放")]
        public float snapshotScaleMultiplier = 0.9f;
        [Tooltip("碰撞接触点小标记相对真实球直径的缩放")]
        public float contactMarkerScaleMultiplier = 0.05f;
        [Tooltip("快照之间、快照与真实球/碰撞点之间至少间隔多少个球半径")]
        public float minSnapshotSpacingRadii = 1.7f;

        public GameObject snapshotPrefab;
        public GameObject contactMarkerPrefab;
        public Color cueColor = new Color(0.22f, 0.93f, 0.95f, 0.18f);
        public Color targetColor = new Color(1f, 0.6f, 0.2f, 0.20f);
        public Color collisionCueColor = new Color(1f, 1f, 1f, 0.38f);
        public Color contactColor = new Color(1f, 0.12f, 0.12f, 0.95f);

        readonly List<GameObject> cueSnapshots = new List<GameObject>();
        readonly List<GameObject> targetSnapshots = new List<GameObject>();
        GameObject collisionCueSnapshot;
        GameObject contactMarker;

        public GameObject CurrentCollisionCueSnapshot => collisionCueSnapshot;
        public GameObject CurrentContactMarker => contactMarker;

        public void Show(SimulationResult result, float ballRadius)
        {
            if (!showSnapshots)
            {
                Hide();
                return;
            }

            float ballScale = Mathf.Max(0.001f, ballRadius * 2f * snapshotScaleMultiplier);
            HideLayer(cueSnapshots);
            HideLayer(targetSnapshots);
            HideChildrenWithPrefix("CueSnapshot");
            HideChildrenWithPrefix("TargetSnapshot");

            if (result.hasBallCollision)
            {
                collisionCueSnapshot = ShowSingle(collisionCueSnapshot, result.collisionCueBallCenter,
                    ballScale * 1.05f, collisionCueColor, snapshotPrefab, "CollisionCueSnapshot");
                contactMarker = ShowSingle(contactMarker, result.collisionContactPoint,
                    Mathf.Max(0.003f, ballRadius * 2f * contactMarkerScaleMultiplier),
                    contactColor, contactMarkerPrefab != null ? contactMarkerPrefab : snapshotPrefab,
                    "CollisionContactPoint");
            }
            else
            {
                HideSingle(collisionCueSnapshot);
                HideSingle(contactMarker);
            }
        }

        public void Hide()
        {
            HideLayer(cueSnapshots);
            HideLayer(targetSnapshots);
            HideSingle(collisionCueSnapshot);
            HideSingle(contactMarker);
        }

        void ShowLayer(List<GameObject> layer, Vector3[] points, float scale, Color color,
            GameObject prefab, string namePrefix)
        {
            int count = points != null ? points.Length : 0;
            EnsureLayerSize(layer, count, prefab, namePrefix);
            for (int i = 0; i < layer.Count; i++)
            {
                bool active = i < count;
                layer[i].SetActive(active);
                if (!active) continue;
                layer[i].transform.position = points[i];
                layer[i].transform.localScale = Vector3.one * scale;
                ApplyColor(layer[i], color);
            }
        }

        void EnsureLayerSize(List<GameObject> layer, int count, GameObject prefab, string namePrefix)
        {
            while (layer.Count < count)
                layer.Add(CreateMarker(prefab, namePrefix + "_" + layer.Count));
        }

        GameObject ShowSingle(GameObject marker, Vector3 position, float scale, Color color,
            GameObject prefab, string name)
        {
            if (marker == null)
                marker = CreateMarker(prefab, name);
            marker.SetActive(true);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * scale;
            ApplyColor(marker, color);
            return marker;
        }

        GameObject CreateMarker(GameObject prefab, string markerName)
        {
            GameObject marker = prefab != null
                ? Instantiate(prefab, transform)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = markerName;
            marker.transform.SetParent(transform, true);
            var collider = marker.GetComponent<Collider>();
            DestroyRuntimeOrImmediate(collider);
            return marker;
        }

        static void HideLayer(List<GameObject> layer)
        {
            for (int i = 0; i < layer.Count; i++)
                if (layer[i] != null) layer[i].SetActive(false);
        }

        void HideChildrenWithPrefix(string namePrefix)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.StartsWith(namePrefix))
                    child.gameObject.SetActive(false);
            }
        }

        static void HideSingle(GameObject marker)
        {
            if (marker != null) marker.SetActive(false);
        }

        static void ApplyColor(GameObject marker, Color color)
        {
            var renderer = marker.GetComponent<Renderer>();
            if (renderer == null) return;
            Material mat = GetMarkerMaterial(renderer);
            ConfigureTransparentMaterial(mat);
            mat.color = color;
        }

        static Material GetMarkerMaterial(Renderer renderer)
        {
            Material mat = renderer.sharedMaterial;
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = MARKER_MATERIAL_NAME };
                renderer.sharedMaterial = mat;
                return mat;
            }

            if (!mat.name.StartsWith(MARKER_MATERIAL_NAME))
            {
                mat = new Material(mat) { name = MARKER_MATERIAL_NAME };
                renderer.sharedMaterial = mat;
            }
            return mat;
        }

        static void DestroyRuntimeOrImmediate(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        static void ConfigureTransparentMaterial(Material mat)
        {
            if (mat == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) mat.shader = shader;

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
