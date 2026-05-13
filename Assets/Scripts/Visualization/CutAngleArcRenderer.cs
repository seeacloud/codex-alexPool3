using TMPro;
using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.Visualization
{
    public class CutAngleArcRenderer : MonoBehaviour
    {
        const string AngleName = "∠1";

        [Tooltip("弧半径（米）")]
        public float arcRadius = 0.06f;
        [Tooltip("弧线段数（越多越平滑）")]
        public int arcSegments = 24;
        [Tooltip("弧颜色")]
        public Color arcColor = new Color(1f, 0.85f, 0.2f, 1f);

        LineRenderer arcLine;
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
            arcLine = AngleMarkerStyle.CreateArc(transform, "CutAngleArc", arcColor);
        }

        void EnsureLabel()
        {
            if (label != null) return;
            label = AngleMarkerStyle.CreateLabel(transform, "CutAngleLabel", out labelRT);
        }

        public void Show(Vector3 cuePos, Vector3 targetPos, Vector3 pocketPos)
        {
            Show(cuePos, targetPos, pocketPos, 0f);
        }

        public void Show(Vector3 cuePos, Vector3 targetPos, Vector3 pocketPos, float radius)
        {
            if (!ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.CutAngleArc))
            {
                Hide();
                return;
            }

            EnsureArc();
            EnsureLabel();

            if (!TryGetRedGreenDirections(cuePos, targetPos, pocketPos, out Vector3 green, out Vector3 red))
            {
                Hide();
                return;
            }

            Vector3 a = green;
            Vector3 b = red;
            if (Vector3.Dot(a, b) < 0f) b = -b;

            float angleDeg = ComputeRedGreenAngleDegrees(cuePos, targetPos, pocketPos);
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
                Quaternion rot = Quaternion.AngleAxis(signedDeg * t, Vector3.up);
                Vector3 dir = rot * a;
                Vector3 p = targetPos + dir * arcRadius;
                p.y = targetPos.y;
                arcLine.SetPosition(i, p);
            }
            AngleMarkerStyle.ApplyArcStyle(arcLine, arcColor);
            arcLine.enabled = true;

            Quaternion midRot = Quaternion.AngleAxis(signedDeg * 0.5f, Vector3.up);
            Vector3 midDir = midRot * a;
            labelVertexWorld = targetPos;
            labelArcPointWorld = targetPos + midDir * arcRadius;
            labelArcPointWorld.y = targetPos.y;

            label.fontSize = AngleMarkerStyle.LabelFontSizePx;
            label.color = Color.white;
            label.text = AngleName + " " + angleDeg.ToString("0.#") + "°";
            labelRT.gameObject.SetActive(true);
            labelVisible = true;

            UpdateLabelScreenPos();
        }

        public static float ComputeRedGreenAngleDegrees(Vector3 cuePos, Vector3 targetPos, Vector3 pocketPos)
        {
            if (!TryGetRedGreenDirections(cuePos, targetPos, pocketPos, out Vector3 green, out Vector3 red))
                return 0f;

            if (Vector3.Dot(green, red) < 0f)
                red = -red;

            float dot = Mathf.Clamp(Vector3.Dot(green, red), -1f, 1f);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }

        static bool TryGetRedGreenDirections(
            Vector3 cuePos,
            Vector3 targetPos,
            Vector3 pocketPos,
            out Vector3 green,
            out Vector3 red)
        {
            green = pocketPos - targetPos;
            green.y = 0f;
            red = targetPos - cuePos;
            red.y = 0f;
            if (green.sqrMagnitude < 1e-6f || red.sqrMagnitude < 1e-6f)
                return false;

            green.Normalize();
            red.Normalize();
            return true;
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
            AngleMarkerStyle.PlaceLabel(label, labelRT, labelVertexWorld, labelArcPointWorld, "CutAngleLabel");
        }
    }
}
