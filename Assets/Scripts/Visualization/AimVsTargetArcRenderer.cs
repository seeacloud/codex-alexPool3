using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Draws an arc at the cue ball vertex marking the acute angle between
    /// (cue→target) red line and the user's manual aim blue line.
    /// Label: screen-space UI, fixed 14px white thin font, hugging the arc
    /// outer edge by a few pixels.
    /// Hidden when no manual aim is set.
    /// </summary>
    public class AimVsTargetArcRenderer : MonoBehaviour
    {
        [Tooltip("弧半径（米）")]
        public float arcRadius = 0.06f;
        [Tooltip("弧线段数")]
        public int arcSegments = 24;
        [Tooltip("弧线宽度（米）")]
        public float arcLineWidth = 0.0008f;
        [Tooltip("弧颜色")]
        public Color arcColor = new Color(0.3f, 0.8f, 1f, 1f);

        [Header("Label (screen-space, fixed pixel size)")]
        [Tooltip("标签字号（像素）")]
        public float labelFontSizePx = 14f;
        [Tooltip("标签沿角平分线离弧多远（像素）")]
        public float labelScreenGapPx = 4f;

        LineRenderer arcLine;
        Canvas overlayCanvas;
        RectTransform labelRT;
        TextMeshProUGUI label;
        Vector3 labelArcPointWorld;
        Vector3 labelVertexWorld;
        bool labelVisible;

        void Awake()
        {
            EnsureArc();
            EnsureLabel();
        }

        void EnsureArc()
        {
            if (arcLine != null) return;
            var go = new GameObject("AimVsTargetArc");
            go.transform.SetParent(transform, false);
            arcLine = go.AddComponent<LineRenderer>();
            arcLine.useWorldSpace = true;
            arcLine.numCornerVertices = 0;
            arcLine.numCapVertices = 0;
            arcLine.alignment = LineAlignment.View;
            arcLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arcLine.receiveShadows = false;
            arcLine.startWidth = arcLineWidth;
            arcLine.endWidth = arcLineWidth;
            Color c = arcColor; c.a = 1f;
            arcLine.startColor = c;
            arcLine.endColor = c;
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = c;
            arcLine.sharedMaterial = mat;
            arcLine.enabled = false;
        }

        void EnsureLabel()
        {
            if (label != null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "AimVsTargetLabel") Object.Destroy(child.gameObject);
            }

            overlayCanvas = FindOrCreateOverlayCanvas();

            var go = new GameObject("AimVsTargetLabel", typeof(RectTransform));
            go.transform.SetParent(overlayCanvas.transform, false);
            labelRT = go.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(0f, 0f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);
            labelRT.sizeDelta = new Vector2(60f, 20f);

            label = go.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = labelFontSizePx;
            label.fontWeight = FontWeight.Thin;
            label.color = Color.white;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            go.SetActive(false);
        }

        static Canvas FindOrCreateOverlayCanvas()
        {
            var existing = GameObject.Find("CutAngleLabelCanvas");
            if (existing != null)
            {
                var c = existing.GetComponent<Canvas>();
                if (c != null) return c;
            }
            var go = new GameObject("CutAngleLabelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var gr = go.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
            return canvas;
        }

        public void Show(Vector3 cuePos, Vector3 targetPos, Vector3 aimDir)
        {
            EnsureArc();
            EnsureLabel();

            // Two rays from cue ball:
            //   red:  cue → target
            //   blue: cue + aimDir (user's manual aim direction)
            Vector3 red = targetPos - cuePos; red.y = 0f;
            Vector3 blue = aimDir; blue.y = 0f;
            if (red.sqrMagnitude < 1e-6f || blue.sqrMagnitude < 1e-6f)
            {
                Hide();
                return;
            }
            red.Normalize();
            blue.Normalize();

            Vector3 a = red;
            Vector3 b = blue;
            if (Vector3.Dot(a, b) < 0f) b = -b;

            float angleRad = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f));
            float angleDeg = angleRad * Mathf.Rad2Deg;
            if (angleDeg < 0.1f)
            {
                Hide();
                return;
            }

            float signedDeg = Vector3.SignedAngle(a, b, Vector3.up);
            int segs = Mathf.Max(2, arcSegments);
            arcLine.positionCount = segs + 1;
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float deg = signedDeg * t;
                Quaternion rot = Quaternion.AngleAxis(deg, Vector3.up);
                Vector3 dir = rot * a;
                Vector3 p = cuePos + dir * arcRadius;
                p.y = cuePos.y;
                arcLine.SetPosition(i, p);
            }
            arcLine.startWidth = arcLineWidth;
            arcLine.endWidth = arcLineWidth;
            arcLine.enabled = true;

            Quaternion midRot = Quaternion.AngleAxis(signedDeg * 0.5f, Vector3.up);
            Vector3 midDir = midRot * a;
            labelVertexWorld = cuePos;
            labelArcPointWorld = cuePos + midDir * arcRadius;
            labelArcPointWorld.y = cuePos.y;

            label.fontSize = labelFontSizePx;
            label.color = Color.white;
            label.text = angleDeg.ToString("0.#") + "°";
            labelRT.gameObject.SetActive(true);
            labelVisible = true;

            UpdateLabelScreenPos();
        }

        public void Hide()
        {
            if (arcLine != null) arcLine.enabled = false;
            if (labelRT != null) labelRT.gameObject.SetActive(false);
            labelVisible = false;
        }

        void LateUpdate()
        {
            if (labelVisible) UpdateLabelScreenPos();
        }

        void UpdateLabelScreenPos()
        {
            if (labelRT == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 sVertex = cam.WorldToScreenPoint(labelVertexWorld);
            Vector3 sArc = cam.WorldToScreenPoint(labelArcPointWorld);
            if (sVertex.z < 0f || sArc.z < 0f)
            {
                labelRT.gameObject.SetActive(false);
                return;
            }
            if (!labelRT.gameObject.activeSelf) labelRT.gameObject.SetActive(true);

            Vector2 dir = new Vector2(sArc.x - sVertex.x, sArc.y - sVertex.y);
            if (dir.sqrMagnitude < 0.25f)
            {
                Vector3 bisectorWorld = labelArcPointWorld - labelVertexWorld;
                Vector3 far = cam.WorldToScreenPoint(labelVertexWorld + bisectorWorld * 10f);
                dir = new Vector2(far.x - sVertex.x, far.y - sVertex.y);
            }
            if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
            else dir.Normalize();

            label.ForceMeshUpdate();
            Vector2 size = label.GetRenderedValues(true);
            float halfExtent = Mathf.Abs(dir.x) * size.x * 0.5f + Mathf.Abs(dir.y) * size.y * 0.5f;

            Vector2 anchor = new Vector2(sArc.x, sArc.y) + dir * (labelScreenGapPx + halfExtent);
            labelRT.anchoredPosition = anchor;
            labelRT.sizeDelta = new Vector2(size.x + 4f, size.y + 2f);
        }
    }
}
