using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Draws an arc at the target ball vertex marking the acute angle between
    /// (target→pocket) green line and (cue→target) red line. The degree label
    /// is rendered as a screen-space UI element (fixed pixel font, never scales
    /// with zoom), billboarded at the projected bisector point.
    /// </summary>
    public class CutAngleArcRenderer : MonoBehaviour
    {
        [Tooltip("弧半径（米）")]
        public float arcRadius = 0.06f;
        [Tooltip("弧线段数（越多越平滑）")]
        public int arcSegments = 24;
        [Tooltip("弧线宽度（米）")]
        public float arcLineWidth = 0.0008f;
        [Tooltip("弧颜色")]
        public Color arcColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Header("Label (screen-space, fixed pixel size)")]
        [Tooltip("标签字号（像素）")]
        public float labelFontSizePx = 14f;
        [Tooltip("标签沿角平分线离弧多远（像素）- 屏幕空间")]
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
            var go = new GameObject("CutAngleArc");
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

            // Clean up any stale child label objects from previous versions of this
            // component (e.g. the old world-space 3D TextMeshPro) so only the new
            // screen-space UI label exists.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "CutAngleLabel") Object.Destroy(child.gameObject);
            }

            overlayCanvas = FindOrCreateOverlayCanvas();

            var go = new GameObject("CutAngleLabel", typeof(RectTransform));
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
            // Dedicated Canvas for the angle label (ConstantPixelSize so it never
            // scales with window size). DO NOT reuse the main UI canvas — that one
            // uses ScaleWithScreenSize which would mess up our pixel coords.
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
            if (gr != null) gr.enabled = false; // no interaction needed
            return canvas;
        }

        public void Show(Vector3 cuePos, Vector3 targetPos, Vector3 pocketPos)
        {
            EnsureArc();
            EnsureLabel();

            Vector3 green = pocketPos - targetPos; green.y = 0f;
            Vector3 red = targetPos - cuePos;      red.y = 0f;
            if (green.sqrMagnitude < 1e-6f || red.sqrMagnitude < 1e-6f)
            {
                Hide();
                return;
            }
            green.Normalize();
            red.Normalize();

            // Acute angle: flip one ray if the two directions point opposite sides.
            Vector3 a = green;
            Vector3 b = red;
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
                Vector3 p = targetPos + dir * arcRadius;
                p.y = targetPos.y;
                arcLine.SetPosition(i, p);
            }
            arcLine.startWidth = arcLineWidth;
            arcLine.endWidth = arcLineWidth;
            arcLine.enabled = true;

            // Two 3D anchors:
            //   vertexWorld = target ball center (angle vertex)
            //   arcPointWorld = arc midpoint along the bisector, at arc radius
            // At render time we project both to screen, compute screen-space
            // bisector direction, then push the label a fixed pixel amount
            // beyond the arc point (plus its own half-extent) so it never
            // overlaps the arc or the ball.
            Quaternion midRot = Quaternion.AngleAxis(signedDeg * 0.5f, Vector3.up);
            Vector3 midDir = midRot * a;
            labelVertexWorld = targetPos;
            labelArcPointWorld = targetPos + midDir * arcRadius;
            labelArcPointWorld.y = targetPos.y;

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

            // Screen-space bisector direction (from vertex outwards through the arc point).
            // May degenerate when the camera is looking along the arc normal — fall back
            // to the screen-projected bisector world vector for a stable direction.
            Vector2 dir = new Vector2(sArc.x - sVertex.x, sArc.y - sVertex.y);
            if (dir.sqrMagnitude < 0.25f) // < 0.5px
            {
                Vector3 bisectorWorld = labelArcPointWorld - labelVertexWorld;
                Vector3 far = cam.WorldToScreenPoint(labelVertexWorld + bisectorWorld * 10f);
                dir = new Vector2(far.x - sVertex.x, far.y - sVertex.y);
            }
            if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
            else dir.Normalize();

            // Half-extent along `dir`: project the rendered text bounds onto dir.
            label.ForceMeshUpdate();
            Vector2 size = label.GetRenderedValues(true);
            float halfExtent = Mathf.Abs(dir.x) * size.x * 0.5f + Mathf.Abs(dir.y) * size.y * 0.5f;

            Vector2 anchor = new Vector2(sArc.x, sArc.y) + dir * (labelScreenGapPx + halfExtent);
            labelRT.anchoredPosition = anchor;
            labelRT.sizeDelta = new Vector2(size.x + 4f, size.y + 2f);
        }
    }
}
