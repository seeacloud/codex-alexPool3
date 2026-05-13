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

        [Header("Render Texture Quality")]
        [Tooltip("按 HUD 显示尺寸放大渲染纹理，2 表示用两倍像素重建，减少球边缘锯齿")]
        public float renderTextureScale = 2f;
        [Tooltip("HUD RenderTexture 的多重采样等级")]
        public int renderTextureAntiAliasing = 4;
        [Tooltip("HUD RenderTexture 的最小边长")]
        public int minRenderTextureSize = 256;
        [Tooltip("HUD RenderTexture 的最大边长，避免窗口放大后占用过多显存")]
        public int maxRenderTextureSize = 2048;

        [Header("UI widgets")]
        public RectTransform crosshairHBar;
        public RectTransform crosshairVBar;
        public RectTransform idealAnswerVBar;
        public TMP_Text offsetLabel;
        [Tooltip("HUD 窗口十字线和理想答案线的屏幕像素宽度")]
        public float hudLineWidthPx = 1f;
        [Tooltip("十字竖轴的长度 = 球直径 + 这个额外值（米）")]
        public float crosshairVerticalExtraMeters = 0.010f;
        public float ballDiameter = 0.0572f;

        [Header("Nudge & Resize")]
        public UnityEngine.UI.Button nudgeLeftBtn;
        public UnityEngine.UI.Button nudgeRightBtn;
        public UnityEngine.UI.Button resizeBtn;
        [Tooltip("每次微调偏移量（毫米）")]
        public float nudgeStepMm = 0.5f;
        [Tooltip("HUD 面板 RectTransform（用于放大/缩小）")]
        public RectTransform panelRect;
        public Vector2 sizeSmall = new Vector2(360f, 440f);
        public Vector2 sizeLarge = new Vector2(640f, 720f);
        public Vector2 sizeHidden = new Vector2(60f, 60f);
        public KeyCode resizeKey = KeyCode.H;
        int hudState; // 0=normal, 1=large, 2=hidden

        [Tooltip("HUD 内容容器（隐藏态时隐藏它，只留 resize 按钮）")]
        public GameObject hudContent;

        Vector2 lastClickNormalized = new Vector2(0.5f, 0.5f);
        bool hasClicked;
        RenderTexture ownedRenderTexture;

        void Start()
        {
            ApplyHudLineWidthToAssignedBars();
            if (crosshairHBar != null) crosshairHBar.gameObject.SetActive(false);
            if (crosshairVBar != null) crosshairVBar.gameObject.SetActive(false);
            if (idealAnswerVBar != null) idealAnswerVBar.gameObject.SetActive(false);
            if (offsetLabel != null) offsetLabel.text = "dx = 0.0 mm";
            if (nudgeLeftBtn != null) nudgeLeftBtn.onClick.AddListener(() => NudgeAim(-nudgeStepMm));
            if (nudgeRightBtn != null) nudgeRightBtn.onClick.AddListener(() => NudgeAim(+nudgeStepMm));
            if (resizeBtn != null) resizeBtn.onClick.AddListener(ToggleSize);
        }

        void ApplyHudLineWidthToAssignedBars()
        {
            float width = HudLineWidthCanvasUnits();
            if (crosshairHBar != null)
                crosshairHBar.sizeDelta = new Vector2(crosshairHBar.sizeDelta.x, width);
            if (crosshairVBar != null)
                crosshairVBar.sizeDelta = new Vector2(width, crosshairVBar.sizeDelta.y);
            if (idealAnswerVBar != null)
                idealAnswerVBar.sizeDelta = new Vector2(width, idealAnswerVBar.sizeDelta.y);
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
            EnsureRenderTexture();

            if (hasClicked)
            {
                UpdateCrosshair();
                UpdateOffsetLabel();
            }
        }

        void OnDestroy()
        {
            ReleaseOwnedRenderTexture();
        }

        void EnsureRenderTexture()
        {
            if (aimCamera == null || rawImage == null || rawImageRect == null) return;

            Rect rect = rawImageRect.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float canvasScale = rawImage.canvas != null ? rawImage.canvas.scaleFactor : 1f;
            float scale = Mathf.Max(1f, renderTextureScale);
            int width = Mathf.Clamp(Mathf.CeilToInt(rect.width * canvasScale * scale), minRenderTextureSize, maxRenderTextureSize);
            int height = Mathf.Clamp(Mathf.CeilToInt(rect.height * canvasScale * scale), minRenderTextureSize, maxRenderTextureSize);
            int aa = Mathf.Clamp(NextSupportedAntiAliasing(renderTextureAntiAliasing), 1, 8);

            RenderTexture current = aimCamera.targetTexture;
            if (current != null &&
                current.width == width &&
                current.height == height &&
                current.antiAliasing == aa)
            {
                if (rawImage.texture != current)
                    rawImage.texture = current;
                return;
            }

            ReleaseOwnedRenderTexture();

            ownedRenderTexture = new RenderTexture(width, height, 24)
            {
                name = "AimHudRT_Runtime",
                antiAliasing = aa,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false
            };
            ownedRenderTexture.Create();
            aimCamera.targetTexture = ownedRenderTexture;
            rawImage.texture = ownedRenderTexture;
        }

        static int NextSupportedAntiAliasing(int requested)
        {
            if (requested >= 8) return 8;
            if (requested >= 4) return 4;
            if (requested >= 2) return 2;
            return 1;
        }

        void ReleaseOwnedRenderTexture()
        {
            if (ownedRenderTexture == null) return;

            if (aimCamera != null && aimCamera.targetTexture == ownedRenderTexture)
                aimCamera.targetTexture = null;
            if (rawImage != null && rawImage.texture == ownedRenderTexture)
                rawImage.texture = null;

            ownedRenderTexture.Release();
            if (Application.isPlaying)
                Destroy(ownedRenderTexture);
            else
                DestroyImmediate(ownedRenderTexture);
            ownedRenderTexture = null;
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
                crosshairHBar.sizeDelta = new Vector2(crosshairHBar.sizeDelta.x, HudLineWidthCanvasUnits());
                crosshairHBar.anchoredPosition = new Vector2(0f, centerY);
            }
            if (crosshairVBar != null)
            {
                crosshairVBar.gameObject.SetActive(true);
                float worldHeight = ballDiameter + crosshairVerticalExtraMeters;
                float pxHeight = worldHeight / (2f * orthoSize) * r.height;
                crosshairVBar.sizeDelta = new Vector2(HudLineWidthCanvasUnits(), pxHeight);
                crosshairVBar.anchoredPosition = new Vector2(clickX, centerY);
            }
        }

        public void ShowIdealAnswerMarker(Vector3 worldPoint)
        {
            if (idealAnswerVBar == null || aimCamera == null || rawImageRect == null) return;

            Rect r = rawImageRect.rect;
            if (r.width <= 0f || r.height <= 0f) return;

            Vector3 viewport = aimCamera.WorldToViewportPoint(worldPoint);
            if (viewport.z < 0f)
            {
                HideIdealAnswerMarker();
                return;
            }

            float centerY = r.height * 0.5f;
            float markerX = Mathf.Clamp01(viewport.x) * r.width;
            float worldHeight = ballDiameter + crosshairVerticalExtraMeters;
            float pxHeight = worldHeight / (2f * orthoSize) * r.height;

            idealAnswerVBar.gameObject.SetActive(true);
            idealAnswerVBar.sizeDelta = new Vector2(HudLineWidthCanvasUnits(), pxHeight);
            idealAnswerVBar.anchoredPosition = new Vector2(markerX, centerY);
        }

        float HudLineWidthCanvasUnits()
        {
            float scale = 1f;
            if (rawImage != null && rawImage.canvas != null)
                scale = Mathf.Max(0.001f, rawImage.canvas.scaleFactor);

            return Mathf.Max(1f, hudLineWidthPx) / scale;
        }

        public void HideIdealAnswerMarker()
        {
            if (idealAnswerVBar != null)
                idealAnswerVBar.gameObject.SetActive(false);
        }

        public void ClearAimSelection()
        {
            hasClicked = false;
            lastClickNormalized = new Vector2(0.5f, 0.5f);
            if (crosshairHBar != null)
                crosshairHBar.gameObject.SetActive(false);
            if (crosshairVBar != null)
                crosshairVBar.gameObject.SetActive(false);
            if (offsetLabel != null)
                offsetLabel.text = "dx = 0.0 mm";
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

        public void ToggleSize()
        {
            if (panelRect == null) return;
            hudState = (hudState + 1) % 3;
            switch (hudState)
            {
                case 0: panelRect.sizeDelta = sizeSmall; if (hudContent != null) hudContent.SetActive(true); break;
                case 1: panelRect.sizeDelta = sizeLarge; if (hudContent != null) hudContent.SetActive(true); break;
                case 2: panelRect.sizeDelta = sizeHidden; if (hudContent != null) hudContent.SetActive(false); break;
            }
        }
    }
}
