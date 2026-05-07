using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.UI
{
    /// <summary>
    /// HUD that renders the target ball as viewed from the cue ball's position.
    /// Clicking on the HUD image converts the click into a 3D aim point and tells
    /// AimManager to use that aim direction (overrides auto-ghost until cleared).
    /// Shows a crosshair at the current strike point and a readout of the horizontal
    /// offset from the target ball's center in millimetres.
    /// </summary>
    public class AimHudController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler
    {
        public Camera aimCamera;
        public RawImage rawImage;
        public RectTransform rawImageRect;
        public BallController cueBall;
        public BallController targetBall;
        public AimManager aimManager;

        [Tooltip("HUD 正交相机的视野大小（世界单位，半高）。orthoSize*2 = 窗口垂直方向覆盖的世界距离")]
        public float orthoSize = 0.08f;

        [Tooltip("HUD 相机 near plane 距离主球的偏移（米）")]
        public float nearOffset = 0.001f;

        [Header("UI widgets")]
        public RectTransform crosshair;
        public TMP_Text offsetLabel;

        Vector2 lastClickNormalized = new Vector2(0.5f, 0.5f);
        bool hasClicked;

        void Start()
        {
            if (crosshair != null) crosshair.gameObject.SetActive(false);
            if (offsetLabel != null) offsetLabel.text = "Δ = 0.0 mm";
        }

        void LateUpdate()
        {
            if (aimCamera == null || cueBall == null || targetBall == null) return;
            Vector3 dir = targetBall.Center - cueBall.Center;
            float dist = dir.magnitude;
            if (dist < 1e-4f) return;
            Vector3 forward = dir / dist;
            aimCamera.transform.position = cueBall.Center + forward * nearOffset;
            aimCamera.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            aimCamera.orthographic = true;
            aimCamera.orthographicSize = orthoSize;
            aimCamera.nearClipPlane = 0.001f;
            aimCamera.farClipPlane = Mathf.Max(2f, dist + 1f);

            if (hasClicked)
            {
                UpdateCrosshair();
                UpdateOffsetLabel();
            }
        }

        void UpdateCrosshair()
        {
            if (crosshair == null || rawImageRect == null) return;
            Rect r = rawImageRect.rect;
            Vector2 local = new Vector2(
                r.xMin + lastClickNormalized.x * r.width,
                r.yMin + lastClickNormalized.y * r.height);
            crosshair.gameObject.SetActive(true);
            crosshair.anchoredPosition = local;
        }

        void UpdateOffsetLabel()
        {
            if (offsetLabel == null) return;
            // Horizontal offset in world metres: (u - 0.5) * 2 * orthoSize * aspect
            float aspect = 1f;
            if (aimCamera != null && aimCamera.pixelHeight > 0)
                aspect = (float)aimCamera.pixelWidth / aimCamera.pixelHeight;
            float dxWorld = (lastClickNormalized.x - 0.5f) * 2f * orthoSize * aspect;
            float dyWorld = (lastClickNormalized.y - 0.5f) * 2f * orthoSize;
            float dxMm = dxWorld * 1000f;
            float dyMm = dyWorld * 1000f;
            string side = dxMm >= 0f ? "R" : "L";
            string vert = dyMm >= 0f ? "↑" : "↓";
            offsetLabel.text = string.Format(CultureInfo.InvariantCulture,
                "Δx = {0:+0.0;-0.0} mm {1}   Δy = {2:+0.0;-0.0} mm {3}",
                dxMm, side, dyMm, vert);
        }

        public void OnPointerClick(PointerEventData e) { ProcessClick(e); }
        public void OnPointerDown(PointerEventData e) { ProcessClick(e); }
        public void OnDrag(PointerEventData e) { ProcessClick(e); }

        void ProcessClick(PointerEventData e)
        {
            if (aimCamera == null || rawImageRect == null || cueBall == null) return;
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rawImageRect, e.position, e.pressEventCamera, out localPoint))
                return;

            Rect rect = rawImageRect.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float u = (localPoint.x - rect.xMin) / rect.width;
            float v = (localPoint.y - rect.yMin) / rect.height;
            lastClickNormalized = new Vector2(u, v);
            hasClicked = true;

            float zDepth = Mathf.Max(nearOffset + 0.01f,
                Vector3.Distance(cueBall.Center, targetBall != null ? targetBall.Center : aimCamera.transform.position + aimCamera.transform.forward));
            Vector3 worldPoint = aimCamera.ViewportToWorldPoint(new Vector3(u, v, zDepth));
            Vector3 aimDir = (worldPoint - cueBall.Center).normalized;

            if (aimManager != null) aimManager.SetManualAimDir(aimDir);
        }
    }
}
