using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Visualization
{
    public static class AngleMarkerStyle
    {
        public const float ArcLineWidth = 0.0016f;
        public const float LabelFontSizePx = 14f;
        public const float LabelScreenGapPx = 4f;
        const string OverlayCanvasName = "CutAngleLabelCanvas";

        public static LineRenderer CreateArc(Transform parent, string name, Color color)
        {
            DestroyChildByName(parent, name);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            ApplyArcStyle(line, color);
            line.enabled = false;
            return line;
        }

        public static void ApplyArcStyle(LineRenderer line, Color color)
        {
            if (line == null) return;

            Color c = color;
            c.a = 1f;
            line.startWidth = ArcLineWidth;
            line.endWidth = ArcLineWidth;
            line.widthMultiplier = 1f;
            line.startColor = c;
            line.endColor = c;
            if (line.sharedMaterial == null)
                line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            if (line.sharedMaterial != null)
                line.sharedMaterial.color = c;
        }

        public static Canvas FindOrCreateOverlayCanvas()
        {
            var existing = GameObject.Find(OverlayCanvasName);
            if (existing != null)
            {
                var c = existing.GetComponent<Canvas>();
                if (c != null) return c;
            }

            var go = new GameObject(OverlayCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var gr = go.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
            return canvas;
        }

        public static TextMeshProUGUI CreateLabel(
            Transform owner,
            string name,
            out RectTransform rt)
        {
            DestroyChildByName(owner, name);

            Canvas canvas = FindOrCreateOverlayCanvas();
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(60f, 20f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = LabelFontSizePx;
            text.fontWeight = FontWeight.Thin;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            go.SetActive(false);
            return text;
        }

        static void DestroyChildByName(Transform parent, string name)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        public static void PlaceLabel(
            TextMeshProUGUI text,
            RectTransform textRT,
            Vector3 vertexWorld,
            Vector3 arcPointWorld,
            string layoutKey)
        {
            if (text == null || textRT == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 sVertex = cam.WorldToScreenPoint(vertexWorld);
            Vector3 sArc = cam.WorldToScreenPoint(arcPointWorld);
            if (sVertex.z < 0f || sArc.z < 0f)
            {
                textRT.gameObject.SetActive(false);
                return;
            }
            if (!textRT.gameObject.activeSelf) textRT.gameObject.SetActive(true);

            Vector2 dir = new Vector2(sArc.x - sVertex.x, sArc.y - sVertex.y);
            if (dir.sqrMagnitude < 0.25f)
            {
                Vector3 bisectorWorld = arcPointWorld - vertexWorld;
                Vector3 far = cam.WorldToScreenPoint(vertexWorld + bisectorWorld * 10f);
                dir = new Vector2(far.x - sVertex.x, far.y - sVertex.y);
            }
            if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
            else dir.Normalize();

            text.ForceMeshUpdate();
            Vector2 size = text.GetRenderedValues(true);
            float halfExtent = Mathf.Abs(dir.x) * size.x * 0.5f + Mathf.Abs(dir.y) * size.y * 0.5f;

            Vector2 preferred = new Vector2(sArc.x, sArc.y) + dir * (LabelScreenGapPx + halfExtent);
            Vector2 anchor = AngleLabelLayout.ReserveForCurrentFrame(layoutKey, preferred, size, dir);
            textRT.anchoredPosition = anchor;
            textRT.sizeDelta = new Vector2(size.x + 4f, size.y + 2f);
        }
    }
}
