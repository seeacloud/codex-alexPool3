using TMPro;
using UnityEngine;

namespace PoolAimTrainer.SceneObjects
{
    public class PocketMarker : MonoBehaviour
    {
        [Tooltip("袋口半径（米）")]
        public float pocketRadius = 0.06f;
        public Color normalColor = new Color(1f, 0.85f, 0.2f, 1f);
        public Color highlightColor = new Color(0.2f, 1f, 0.3f, 1f);
        [Tooltip("运行时指示器相对袋口的缩放倍数")]
        public float indicatorScaleMultiplier = 1.4f;

        [Tooltip("袋口编号（1~6），用于 UI 引用；0 表示不显示")]
        public int pocketNumber = 0;
        [Tooltip("编号标签相对球心的向上偏移（米）")]
        public float labelHeight = 0.08f;

        public static bool LabelsVisible = true;

        public static void SetLabelsVisible(bool visible)
        {
            LabelsVisible = visible;
            foreach (var pm in FindObjectsOfType<PocketMarker>())
            {
                var labelT = pm.transform.Find("Label");
                if (labelT != null) labelT.gameObject.SetActive(visible);
            }
        }

        Renderer indicatorRenderer;
        TextMeshPro labelText;

        public Vector3 Position => transform.position;

        void Start()
        {
            EnsureIndicator();
            EnsureLabel();
            SetHighlighted(false);
        }

        void LateUpdate()
        {
            if (labelText != null && Camera.main != null)
            {
                // Billboard the label so it always faces the camera.
                labelText.transform.rotation = Quaternion.LookRotation(
                    labelText.transform.position - Camera.main.transform.position, Vector3.up);
            }
        }

        void EnsureIndicator()
        {
            var existing = transform.Find("Indicator");
            GameObject indicator;
            if (existing != null)
            {
                indicator = existing.gameObject;
            }
            else
            {
                indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.name = "Indicator";
                indicator.transform.SetParent(transform, false);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = Vector3.one * pocketRadius * indicatorScaleMultiplier;
                var col = indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            indicatorRenderer = indicator.GetComponent<Renderer>();
            if (indicatorRenderer != null && indicatorRenderer.sharedMaterial != null)
            {
                var mat = indicatorRenderer.material;
                mat.color = normalColor;
            }
        }

        void EnsureLabel()
        {
            if (pocketNumber <= 0) return;
            var existing = transform.Find("Label");
            GameObject labelGO;
            if (existing != null)
            {
                labelGO = existing.gameObject;
            }
            else
            {
                labelGO = new GameObject("Label");
                labelGO.transform.SetParent(transform, false);
            }
            // Position the label above the pocket and size it to render as a small 3D tag.
            labelGO.transform.localPosition = new Vector3(0f, labelHeight, 0f);
            labelGO.transform.localScale = Vector3.one * 0.015f;
            labelGO.SetActive(LabelsVisible);
            labelText = labelGO.GetComponent<TextMeshPro>();
            if (labelText == null) labelText = labelGO.AddComponent<TextMeshPro>();
            labelText.text = pocketNumber.ToString();
            labelText.fontSize = 4f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.enableWordWrapping = false;
            var rect = labelText.rectTransform;
            rect.sizeDelta = new Vector2(4f, 2f);
        }

        public void SetHighlighted(bool on)
        {
            if (indicatorRenderer == null) EnsureIndicator();
            if (indicatorRenderer != null)
                indicatorRenderer.material.color = on ? highlightColor : normalColor;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pocketRadius);
        }
    }
}
