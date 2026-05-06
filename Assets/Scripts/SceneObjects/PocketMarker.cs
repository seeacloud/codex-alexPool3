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

        Renderer indicatorRenderer;

        public Vector3 Position => transform.position;

        void Start()
        {
            EnsureIndicator();
            SetHighlighted(false);
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
                // Remove collider: we detect clicks via table-plane raycast in DragInput.
                var col = indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            indicatorRenderer = indicator.GetComponent<Renderer>();
            if (indicatorRenderer != null && indicatorRenderer.sharedMaterial != null)
            {
                // Use a per-instance material clone so highlight tint doesn't leak across pockets.
                var mat = indicatorRenderer.material;
                mat.color = normalColor;
            }
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
