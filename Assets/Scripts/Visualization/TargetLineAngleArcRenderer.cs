using TMPro;
using UnityEngine;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.Visualization
{
    public class TargetLineAngleArcRenderer : MonoBehaviour
    {
        const string EstimatedToTargetPathAngleName = "∠3";
        const string TargetPathToCueThroughAngleName = "∠4";

        [Tooltip("弧半径（米）")]
        public float arcRadius = 0.075f;
        [Tooltip("第二条弧的半径偏移（米），避免两条弧重叠")]
        public float secondaryArcRadiusOffset = 0.018f;
        [Tooltip("弧线段数")]
        public int arcSegments = 24;
        [Tooltip("洋红线与橙线角度颜色")]
        public Color estimatedToTargetPathColor = new Color(1f, 0f, 1f, 1f);
        [Tooltip("橙线与红线角度颜色")]
        public Color targetPathToCueThroughColor = new Color(1f, 0.6f, 0.2f, 1f);

        LineRenderer estimatedToTargetPathArc;
        LineRenderer targetPathToCueThroughArc;
        RectTransform estimatedToTargetPathLabelRT;
        RectTransform targetPathToCueThroughLabelRT;
        TextMeshProUGUI estimatedToTargetPathLabel;
        TextMeshProUGUI targetPathToCueThroughLabel;
        ArcLabelPlacement estimatedToTargetPathPlacement;
        ArcLabelPlacement targetPathToCueThroughPlacement;
        bool estimatedToTargetPathVisible;
        bool targetPathToCueThroughVisible;

        struct ArcLabelPlacement
        {
            public Vector3 vertexWorld;
            public Vector3 arcPointWorld;
        }

        void Awake()
        {
            EnsureArcs();
            EnsureLabels();
        }

        void EnsureArcs()
        {
            if (estimatedToTargetPathArc == null)
                estimatedToTargetPathArc = AngleMarkerStyle.CreateArc(
                    transform, "EstimatedToTargetPathAngleArc", estimatedToTargetPathColor);
            if (targetPathToCueThroughArc == null)
                targetPathToCueThroughArc = AngleMarkerStyle.CreateArc(
                    transform, "TargetPathToCueThroughAngleArc", targetPathToCueThroughColor);
        }

        void EnsureLabels()
        {
            if (estimatedToTargetPathLabel != null && targetPathToCueThroughLabel != null)
                return;

            estimatedToTargetPathLabel = AngleMarkerStyle.CreateLabel(
                transform,
                "EstimatedToTargetPathAngleLabel",
                out estimatedToTargetPathLabelRT);
            targetPathToCueThroughLabel = AngleMarkerStyle.CreateLabel(
                transform,
                "TargetPathToCueThroughAngleLabel",
                out targetPathToCueThroughLabelRT);
        }

        public void Show(
            Vector3 targetPos,
            Vector3 estimatedAimDir,
            bool hasEstimatedAimDir,
            Vector3 targetPathDir,
            Vector3 cueThroughTargetDir)
        {
            EnsureArcs();
            EnsureLabels();

            bool showedEstimatedToTargetPath = false;
            if (hasEstimatedAimDir && ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.EstimatedAimToTargetPathAngle))
            {
                showedEstimatedToTargetPath = ShowArc(
                    estimatedToTargetPathArc,
                    estimatedToTargetPathLabel,
                    estimatedToTargetPathLabelRT,
                    targetPos,
                    estimatedAimDir,
                    targetPathDir,
                    arcRadius,
                    EstimatedToTargetPathAngleName,
                    estimatedToTargetPathColor,
                    out estimatedToTargetPathPlacement);
            }

            if (!showedEstimatedToTargetPath)
                HideEstimatedToTargetPath();
            estimatedToTargetPathVisible = showedEstimatedToTargetPath;

            bool showedTargetPathToCueThrough = false;
            if (ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.TargetPathToCueThroughAngle))
            {
                showedTargetPathToCueThrough = ShowArc(
                    targetPathToCueThroughArc,
                    targetPathToCueThroughLabel,
                    targetPathToCueThroughLabelRT,
                    targetPos,
                    targetPathDir,
                    cueThroughTargetDir,
                    arcRadius + secondaryArcRadiusOffset,
                    TargetPathToCueThroughAngleName,
                    targetPathToCueThroughColor,
                    out targetPathToCueThroughPlacement);
            }

            if (!showedTargetPathToCueThrough)
                HideTargetPathToCueThrough();
            targetPathToCueThroughVisible = showedTargetPathToCueThrough;
        }

        bool ShowArc(
            LineRenderer line,
            TextMeshProUGUI text,
            RectTransform textRT,
            Vector3 vertex,
            Vector3 fromDir,
            Vector3 toDir,
            float radius,
            string angleName,
            Color color,
            out ArcLabelPlacement placement)
        {
            placement = default;
            Vector3 a = fromDir;
            Vector3 b = toDir;
            a.y = 0f;
            b.y = 0f;
            if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f)
                return false;

            a.Normalize();
            b.Normalize();
            if (Vector3.Dot(a, b) < 0f) b = -b;

            float angleRad = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f));
            float angleDeg = angleRad * Mathf.Rad2Deg;
            if (angleDeg < 0.1f)
                return false;

            float signedDeg = Vector3.SignedAngle(a, b, Vector3.up);
            int segs = Mathf.Max(2, arcSegments);
            line.positionCount = segs + 1;
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                Quaternion rot = Quaternion.AngleAxis(signedDeg * t, Vector3.up);
                Vector3 dir = rot * a;
                Vector3 p = vertex + dir * radius;
                p.y = vertex.y;
                line.SetPosition(i, p);
            }
            AngleMarkerStyle.ApplyArcStyle(line, color);
            line.enabled = true;

            Quaternion midRot = Quaternion.AngleAxis(signedDeg * 0.5f, Vector3.up);
            Vector3 midDir = midRot * a;
            placement.vertexWorld = vertex;
            placement.arcPointWorld = vertex + midDir * radius;
            placement.arcPointWorld.y = vertex.y;

            text.fontSize = AngleMarkerStyle.LabelFontSizePx;
            text.color = Color.white;
            text.text = angleName + " " + angleDeg.ToString("0.#") + "°";
            textRT.gameObject.SetActive(true);
            AngleMarkerStyle.PlaceLabel(text, textRT, placement.vertexWorld, placement.arcPointWorld, angleName + "Label");
            return true;
        }

        public void Hide()
        {
            HideEstimatedToTargetPath();
            HideTargetPathToCueThrough();
            estimatedToTargetPathVisible = false;
            targetPathToCueThroughVisible = false;
        }

        void HideEstimatedToTargetPath()
        {
            if (estimatedToTargetPathArc != null) estimatedToTargetPathArc.enabled = false;
            if (estimatedToTargetPathLabelRT != null) estimatedToTargetPathLabelRT.gameObject.SetActive(false);
        }

        void HideTargetPathToCueThrough()
        {
            if (targetPathToCueThroughArc != null) targetPathToCueThroughArc.enabled = false;
            if (targetPathToCueThroughLabelRT != null) targetPathToCueThroughLabelRT.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (estimatedToTargetPathVisible)
                AngleMarkerStyle.PlaceLabel(
                    estimatedToTargetPathLabel,
                    estimatedToTargetPathLabelRT,
                    estimatedToTargetPathPlacement.vertexWorld,
                    estimatedToTargetPathPlacement.arcPointWorld,
                    EstimatedToTargetPathAngleName + "Label");
            if (targetPathToCueThroughVisible)
                AngleMarkerStyle.PlaceLabel(
                    targetPathToCueThroughLabel,
                    targetPathToCueThroughLabelRT,
                    targetPathToCueThroughPlacement.vertexWorld,
                    targetPathToCueThroughPlacement.arcPointWorld,
                    TargetPathToCueThroughAngleName + "Label");
        }
    }
}
