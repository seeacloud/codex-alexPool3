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
    /// HUD window showing the target ball from the cue ball's viewpoint. Clicking
    /// inside it sets AimManager's manual aim direction (blue line). A crosshair
    /// marks the current strike point: horizontal bar spans the full HUD width at
    /// the click's Y, vertical bar is short (ball diameter + a margin) centered on
    /// the click. A label below the HUD reads out the horizontal offset in mm.
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
        public RectTransform crosshairHBar;
        public RectTransform crosshairVBar;
        public TMP_Text offsetLabel;
        [Tooltip("十字竖轴的长度 = 球直径 + 这个额外值（米）")]
        public float crosshairVerticalExtraMeters = 0.010f;
        public float ballDiameter = 0.0572f;

        [Header("Nudge & Resize")]
        public UnityEngine.UI.Button nudgeLeftBtn;
        public UnityEngine.UI.Button nudgeRightBtn;
        [Tooltip("每次微调偏移量（毫米）")]
        public float nudgeStepMm = 0.5f;
        [Tooltip("HUD 面板 RectTransform（用于放大/缩小）")]
        public RectTransform panelRect;
        public Vector2 sizeSmall = new Vector2(320f, 340f);
        public Vector2 sizeLarge = new Vector2(640f, 660f);
        public KeyCode resizeKey = KeyCode.H;
        bool isLarge;

        Vector2 lastClickNormalized = new Vector2(0.5f, 0.5f);
        bool hasClicked;

        void Start()
        {
            if (crosshairHBar != null) crosshairHBar.gameObject.SetActive(false);
            if (crosshairVBar != null) crosshairVBar.gameObject.SetActive(false);
            if (offsetLabel != null) offsetLabel.text = "dx = 0.0 mm";
            if (nudgeLeftBtn != null) nudgeLeftBtn.onClick.AddListener(() => NudgeAim(-nudgeStepMm));
            if (nudgeRightBtn != null) nudgeRightBtn.onClick.AddListener(() => NudgeAim(+nudgeStepMm));
        }

        void LateUpdate()
        {
            if (aimCamera == null || cueBall == null || targetBall == null) return;

            if (Input.GetKeyDown(resizeKey)) ToggleSize();

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
            if (rawImageRect == null) return;
            Rect r = rawImageRect.rect;
            if (r.width <= 0f || r.height <= 0f) return;

            // Horizontal axis is locked to target ball center (v = 0.5) = HUD vertical middle.
            float centerY = r.height * 0.5f;
            float clickX = lastClickNormalized.x * r.width;

            if (crosshairHBar != null)
            {
                crosshairHBar.gameObject.SetActive(true);
                crosshairHBar.anchoredPosition = new Vector2(0f, centerY);
            }
            if (crosshairVBar != null)
            {
                crosshairVBar.gameObject.SetActive(true);
                float worldHeight = ballDiameter + crosshairVerticalExtraMeters;
                float pxHeight = worldHeight / (2f * orthoSize) * r.height;
                crosshairVBar.sizeDelta = new Vector2(crosshairVBar.sizeDelta.x, pxHeight);
                crosshairVBar.anchoredPosition = new Vector2(clickX, centerY);
            }
        }

        void UpdateOffsetLabel()
        {
            if (offsetLabel == null) return;
            float aspect = 1f;
            if (aimCamera != null && aimCamera.pixelHeight > 0)
                aspect = (float)aimCamera.pixelWidth / aimCamera.pixelHeight;
            float dxWorldM = (lastClickNormalized.x - 0.5f) * 2f * orthoSize * aspect;
            float dxMm = dxWorldM * 1000f;
            string sign = dxMm >= 0f ? "+" : "-";
            float absMm = Mathf.Abs(dxMm);
            string side = dxMm >= 0f ? "R" : "L";
            offsetLabel.text = string.Format(CultureInfo.InvariantCulture,
                "dx = {0}{1:F1} mm  {2}", sign, absMm, side);
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
            // Lock vertical component to target ball center (v = 0.5) so aim is constrained
            // to the horizontal plane through the target ball.
            float v = 0.5f;
            lastClickNormalized = new Vector2(u, v);
            hasClicked = true;

            float zDepth = Mathf.Max(nearOffset + 0.01f,
                Vector3.Distance(cueBall.Center, targetBall != null ? targetBall.Center : aimCamera.transform.position + aimCamera.transform.forward));
            Vector3 worldPoint = aimCamera.ViewportToWorldPoint(new Vector3(u, v, zDepth));
            Vector3 aimDir = (worldPoint - cueBall.Center).normalized;

            if (aimManager != null) aimManager.SetManualAimDir(aimDir);
        }

        void NudgeAim(float deltaMm)
        {
            float aspect = 1f;
            if (aimCamera != null && aimCamera.pixelHeight > 0)
                aspect = (float)aimCamera.pixelWidth / aimCamera.pixelHeight;
            float deltaWorld = deltaMm * 0.001f;
            float deltaU = deltaWorld / (2f * orthoSize * aspect);
            lastClickNormalized.x = Mathf.Clamp01(lastClickNormalized.x + deltaU);
            hasClicked = true;
            RecomputeAimFromNormalized();
        }

        void RecomputeAimFromNormalized()
        {
            if (aimCamera == null || cueBall == null || targetBall == null) return;
            float u = lastClickNormalized.x;
            float v = lastClickNormalized.y;
            float zDepth = Mathf.Max(nearOffset + 0.01f,
                Vector3.Distance(cueBall.Center, targetBall.Center));
            Vector3 worldPoint = aimCamera.ViewportToWorldPoint(new Vector3(u, v, zDepth));
            Vector3 aimDir = (worldPoint - cueBall.Center).normalized;
            if (aimManager != null) aimManager.SetManualAimDir(aimDir);
        }

        void ToggleSize()
        {
            if (panelRect == null) return;
            isLarge = !isLarge;
            panelRect.sizeDelta = isLarge ? sizeLarge : sizeSmall;
        }
    }
}
