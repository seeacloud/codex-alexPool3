using TMPro;
using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.Visualization
{
    public class AimVsTargetArcRenderer : MonoBehaviour
    {
        const string AngleName = "∠2";

        [Tooltip("弧半径（米）")]
        public float arcRadius = 0.06f;
        [Tooltip("弧线段数")]
        public int arcSegments = 24;
        [Tooltip("弧颜色")]
        public Color arcColor = new Color(0.3f, 0.8f, 1f, 1f);

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
            arcLine = AngleMarkerStyle.CreateArc(transform, "AimVsTargetArc", arcColor);
        }

        void EnsureLabel()
        {
            if (label != null) return;
            label = AngleMarkerStyle.CreateLabel(transform, "AimVsTargetLabel", out labelRT);
        }

        public void Show(Vector3 cuePos, Vector3 targetPos, Vector3 aimDir)
        {
            if (!ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.AimVsTargetArc))
            {
                Hide();
                return;
            }

            EnsureArc();
            EnsureLabel();

            Vector3 red = targetPos - cuePos;
            red.y = 0f;
            Vector3 blue = aimDir;
            blue.y = 0f;
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
                Quaternion rot = Quaternion.AngleAxis(signedDeg * t, Vector3.up);
                Vector3 dir = rot * a;
                Vector3 p = cuePos + dir * arcRadius;
                p.y = cuePos.y;
                arcLine.SetPosition(i, p);
            }
            AngleMarkerStyle.ApplyArcStyle(arcLine, arcColor);
            arcLine.enabled = true;

            Quaternion midRot = Quaternion.AngleAxis(signedDeg * 0.5f, Vector3.up);
            Vector3 midDir = midRot * a;
            labelVertexWorld = cuePos;
            labelArcPointWorld = cuePos + midDir * arcRadius;
            labelArcPointWorld.y = cuePos.y;

            label.fontSize = AngleMarkerStyle.LabelFontSizePx;
            label.color = Color.white;
            label.text = AngleName + " " + angleDeg.ToString("0.#") + "°";
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
            AngleMarkerStyle.PlaceLabel(label, labelRT, labelVertexWorld, labelArcPointWorld, "AimVsTargetLabel");
        }
    }
}
