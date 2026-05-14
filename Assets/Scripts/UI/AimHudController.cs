using System;
using System.Globalization;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
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
        [Tooltip("自动把击打有效区域等比放大到 HUD 窗口")]
        public bool fitStrikeAreaToHudWindow = true;
        [Range(0.1f, 1f)]
        [Tooltip("球径 + 十字线余量占 HUD 高度的比例，越大画面越放大")]
        public float strikeAreaViewportFill = 0.8f;

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
        public RectTransform objectBallCenterHBar;
        public RectTransform objectBallCenterVBar;
        public RectTransform objectBallClockFace;
        public TMP_Text offsetLabel;
        [Tooltip("HUD 窗口十字线和理想答案线的屏幕像素宽度")]
        public float hudLineWidthPx = 1f;
        [Tooltip("子球球心绿色十字线长度（屏幕像素）")]
        public float objectBallCenterCrossLengthPx = 20f;
        [Tooltip("是否在 HUD 子球上显示表盘刻度。")]
        public bool showObjectBallClockFace = true;
        [Tooltip("表盘半径相对 HUD 子球半径的倍数。")]
        public float objectBallClockRadiusScale = 1.08f;
        [Tooltip("表盘数字相对刻度的外扩距离（屏幕像素）。")]
        public float objectBallClockLabelOffsetPx = 12f;
        [Tooltip("表盘刻度长度（屏幕像素）。")]
        public float objectBallClockTickLengthPx = 8f;
        [Tooltip("表盘刻度颜色。")]
        public Color objectBallClockColor = new Color(1f, 1f, 1f, 0.9f);
        [Tooltip("十字竖轴的长度 = 球直径 + 这个额外值（米）")]
        public float crosshairVerticalExtraMeters = 0.010f;
        public float ballDiameter = 0.0572f;

        [Header("HUD Clean View")]
        [Tooltip("瞄准窗口是否使用纯色背景。常用于隐藏球台后，用台面绿色填充背景。")]
        public bool useSolidHudBackground = true;
        [Tooltip("瞄准窗口纯色背景颜色。")]
        public Color solidHudBackgroundColor = new Color(0.02f, 0.24f, 0.10f, 1f);
        [Tooltip("只在瞄准窗口隐藏这些场景参考线；主视图不受影响，不会隐藏 HUD 自己的绿十字/红十字 UI 标线。")]
        public bool hideSelectedReferenceLinesInHud = true;
        [Tooltip("要从瞄准窗口隐藏的参考线层。白线=CueToGhost，红线=CueThroughTarget，镜像红线=MirroredCueThroughTarget，绿线=ObjectToPocket，橙线=TargetBallPath。")]
        public ReferenceVisualLayer[] hudHiddenReferenceLayers =
        {
            ReferenceVisualLayer.CueToGhost,
            ReferenceVisualLayer.ObjectToPocket,
            ReferenceVisualLayer.TargetBallPath,
            ReferenceVisualLayer.CueThroughTarget,
            ReferenceVisualLayer.MirroredCueThroughTarget,
            ReferenceVisualLayer.ManualCuePath,
            ReferenceVisualLayer.EstimatedAimLine,
            ReferenceVisualLayer.CutAngleArc,
            ReferenceVisualLayer.AimVsTargetArc,
            ReferenceVisualLayer.EstimatedAimToTargetPathAngle,
            ReferenceVisualLayer.TargetPathToCueThroughAngle,
            ReferenceVisualLayer.ToleranceFanArea,
            ReferenceVisualLayer.ToleranceLowerTargetPath,
            ReferenceVisualLayer.ToleranceUpperTargetPath,
        };
        [Tooltip("只在瞄准窗口隐藏所有非球体的场景 Renderer。开启后会自动隐藏球台、袋口、参考线、角度弧和预测残影，只保留下方允许列表中的球体。")]
        public bool hideNonBallSceneRenderersInHud = true;
        [Tooltip("只在瞄准窗口隐藏球台 Renderer；主视图不受影响。用于不启用“隐藏非球体场景 Renderer”时的补充控制。")]
        public bool hideTableInHud = true;
        [Tooltip("球台根对象。为空时会尝试使用 AimManager.table。")]
        public GameObject[] hudTableRoots;
        [Tooltip("隐藏非球体场景 Renderer 时额外保留的物体根节点。主球和目标球会自动保留。")]
        public GameObject[] extraHudVisibleRoots;
        [Tooltip("额外指定只在瞄准窗口隐藏的 Renderer，可用于补充球台、袋口或其他视觉物件。")]
        public Renderer[] extraHudHiddenRenderers;

        [Header("Nudge & Resize")]
        public UnityEngine.UI.Button nudgeLeftBtn;
        public UnityEngine.UI.Button nudgeRightBtn;
        public UnityEngine.UI.Button resizeBtn;
        [Tooltip("每次微调偏移量（毫米）")]
        public float nudgeStepMm = 0.5f;
        [Tooltip("HUD 面板 RectTransform（用于放大/缩小）")]
        public RectTransform panelRect;
        public Vector2 sizeSmall = new Vector2(360f, 330f);
        public Vector2 sizeLarge = new Vector2(640f, 540f);
        public Vector2 sizeHidden = new Vector2(60f, 60f);
        public KeyCode resizeKey = KeyCode.H;
        int hudState; // 0=normal, 1=large, 2=hidden
        const float CompressedHudImageHeight = 190f;

        [Tooltip("HUD 内容容器（隐藏态时隐藏它，只留 resize 按钮）")]
        public GameObject hudContent;

        Vector2 lastClickNormalized = new Vector2(0.5f, 0.5f);
        bool hasClicked;
        float currentHudOrthoSize;
        RenderTexture ownedRenderTexture;
        readonly List<RendererState> hudHiddenRendererStates = new List<RendererState>();
        readonly HashSet<Renderer> hudHiddenRendererSet = new HashSet<Renderer>();
        bool isHidingRenderersForHudCamera;

        struct RendererState
        {
            public Renderer renderer;
            public bool enabled;
        }

        void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            Camera.onPostRender += OnCameraPostRender;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPostRender -= OnCameraPostRender;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RestoreHudHiddenRenderers();
        }

        void Start()
        {
            EnsureObjectBallCenterCross();
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
            if (objectBallCenterHBar != null)
                objectBallCenterHBar.sizeDelta = new Vector2(HudLengthCanvasUnits(objectBallCenterCrossLengthPx), width);
            if (objectBallCenterVBar != null)
                objectBallCenterVBar.sizeDelta = new Vector2(width, HudLengthCanvasUnits(objectBallCenterCrossLengthPx));
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
            currentHudOrthoSize = EffectiveHudOrthoSize();
            aimCamera.orthographicSize = currentHudOrthoSize;
            aimCamera.nearClipPlane = 0.001f;
            aimCamera.farClipPlane = Mathf.Max(2f, dist + 1f);
            ApplyHudCameraBackground();
            ApplyCompressedHudLayout();
            EnsureRenderTexture();

            if (!hasClicked)
                UpdateDefaultAimSelection();

            UpdateCrosshair();
            UpdateObjectBallCenterCross();
            UpdateObjectBallClockFace();
            UpdateOffsetLabel();
        }

        void OnDestroy()
        {
            RestoreHudHiddenRenderers();
            ReleaseOwnedRenderTexture();
        }

        void ApplyHudCameraBackground()
        {
            if (aimCamera == null || !useSolidHudBackground)
                return;

            aimCamera.clearFlags = CameraClearFlags.SolidColor;
            aimCamera.backgroundColor = solidHudBackgroundColor;
        }

        void OnCameraPreCull(Camera camera)
        {
            if (camera == aimCamera)
                HideRenderersForHudCamera();
        }

        void OnCameraPostRender(Camera camera)
        {
            if (camera == aimCamera)
                RestoreHudHiddenRenderers();
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == aimCamera)
                HideRenderersForHudCamera();
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == aimCamera)
                RestoreHudHiddenRenderers();
        }

        void HideRenderersForHudCamera()
        {
            if (isHidingRenderersForHudCamera)
                return;

            hudHiddenRendererStates.Clear();
            hudHiddenRendererSet.Clear();

            if (hideNonBallSceneRenderersInHud)
                CollectNonBallSceneRenderers();
            else if (hideSelectedReferenceLinesInHud)
                CollectHudReferenceLineRenderers();
            if (!hideNonBallSceneRenderersInHud && hideTableInHud)
                CollectHudTableRenderers();
            CollectExtraHudHiddenRenderers();

            for (int i = 0; i < hudHiddenRendererStates.Count; i++)
            {
                Renderer renderer = hudHiddenRendererStates[i].renderer;
                if (renderer != null)
                    renderer.enabled = false;
            }

            isHidingRenderersForHudCamera = hudHiddenRendererStates.Count > 0;
        }

        void CollectNonBallSceneRenderers()
        {
            var renderers = FindObjectsOfType<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || ShouldKeepRendererVisibleInHud(renderer))
                    continue;

                AddHudHiddenRenderer(renderer);
            }
        }

        bool ShouldKeepRendererVisibleInHud(Renderer renderer)
        {
            Transform rendererTransform = renderer.transform;
            if (cueBall != null && rendererTransform.IsChildOf(cueBall.transform))
                return true;
            if (targetBall != null && rendererTransform.IsChildOf(targetBall.transform))
                return true;
            if (aimManager != null
                && aimManager.ghostRenderer != null
                && aimManager.ghostRenderer.CurrentInstance != null
                && rendererTransform.IsChildOf(aimManager.ghostRenderer.CurrentInstance.transform))
            {
                return true;
            }
            if (aimManager != null
                && aimManager.snapshotRenderer != null
                && ShouldKeepSnapshotRendererVisibleInHud(rendererTransform))
            {
                return true;
            }

            if (extraHudVisibleRoots == null)
                return false;

            for (int i = 0; i < extraHudVisibleRoots.Length; i++)
            {
                GameObject root = extraHudVisibleRoots[i];
                if (root != null && rendererTransform.IsChildOf(root.transform))
                    return true;
            }

            return false;
        }

        bool ShouldKeepSnapshotRendererVisibleInHud(Transform rendererTransform)
        {
            var snapshotRenderer = aimManager.snapshotRenderer;
            GameObject collisionCueSnapshot = snapshotRenderer.CurrentCollisionCueSnapshot;
            if (collisionCueSnapshot != null && rendererTransform.IsChildOf(collisionCueSnapshot.transform))
                return true;

            GameObject contactMarker = snapshotRenderer.CurrentContactMarker;
            return contactMarker != null && rendererTransform.IsChildOf(contactMarker.transform);
        }

        void RestoreHudHiddenRenderers()
        {
            if (!isHidingRenderersForHudCamera && hudHiddenRendererStates.Count == 0)
                return;

            for (int i = 0; i < hudHiddenRendererStates.Count; i++)
            {
                RendererState state = hudHiddenRendererStates[i];
                if (state.renderer != null)
                    state.renderer.enabled = state.enabled;
            }

            hudHiddenRendererStates.Clear();
            hudHiddenRendererSet.Clear();
            isHidingRenderersForHudCamera = false;
        }

        void CollectHudReferenceLineRenderers()
        {
            var renderers = FindObjectsOfType<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                ReferenceVisualLayer layer;
                if (TryGetReferenceLayerForRendererName(renderer.gameObject.name, out layer)
                    && IsReferenceLayerHiddenInHud(layer))
                {
                    AddHudHiddenRenderer(renderer);
                }
            }
        }

        bool TryGetReferenceLayerForRendererName(string rendererObjectName, out ReferenceVisualLayer layer)
        {
            switch (rendererObjectName)
            {
                case "LR_CueToGhost":
                    layer = ReferenceVisualLayer.CueToGhost;
                    return true;
                case "LR_ObjToPocket":
                    layer = ReferenceVisualLayer.ObjectToPocket;
                    return true;
                case "LR_CueThroughTarget":
                    layer = ReferenceVisualLayer.CueThroughTarget;
                    return true;
                case "LR_MirroredCueThrough":
                    layer = ReferenceVisualLayer.MirroredCueThroughTarget;
                    return true;
                case "LR_TargetPath":
                    layer = ReferenceVisualLayer.TargetBallPath;
                    return true;
                case "LR_CuePath":
                    layer = ReferenceVisualLayer.ManualCuePath;
                    return true;
                case "LR_EstimatedAimLine":
                    layer = ReferenceVisualLayer.EstimatedAimLine;
                    return true;
                case "CutAngleArc":
                    layer = ReferenceVisualLayer.CutAngleArc;
                    return true;
                case "AimVsTargetArc":
                    layer = ReferenceVisualLayer.AimVsTargetArc;
                    return true;
                case "EstimatedToTargetPathAngleArc":
                    layer = ReferenceVisualLayer.EstimatedAimToTargetPathAngle;
                    return true;
                case "TargetPathToCueThroughAngleArc":
                    layer = ReferenceVisualLayer.TargetPathToCueThroughAngle;
                    return true;
                case "PottingToleranceFanMesh":
                    layer = ReferenceVisualLayer.ToleranceFanArea;
                    return true;
                case "LR_ToleranceLowerTargetPath":
                    layer = ReferenceVisualLayer.ToleranceLowerTargetPath;
                    return true;
                case "LR_ToleranceUpperTargetPath":
                    layer = ReferenceVisualLayer.ToleranceUpperTargetPath;
                    return true;
                default:
                    layer = default;
                    return false;
            }
        }

        bool IsReferenceLayerHiddenInHud(ReferenceVisualLayer layer)
        {
            if (hudHiddenReferenceLayers == null)
                return false;

            if (hudHiddenReferenceLayers.Length == 0)
                return true;

            for (int i = 0; i < hudHiddenReferenceLayers.Length; i++)
            {
                if (hudHiddenReferenceLayers[i] == layer)
                    return true;
            }

            return false;
        }

        void CollectHudTableRenderers()
        {
            if (hudTableRoots != null)
            {
                for (int i = 0; i < hudTableRoots.Length; i++)
                    AddHudHiddenRenderersFromRoot(hudTableRoots[i]);
            }

            if (aimManager != null && aimManager.table != null)
                AddHudHiddenRenderersFromRoot(aimManager.table.gameObject);
        }

        void CollectExtraHudHiddenRenderers()
        {
            if (extraHudHiddenRenderers == null)
                return;

            for (int i = 0; i < extraHudHiddenRenderers.Length; i++)
                AddHudHiddenRenderer(extraHudHiddenRenderers[i]);
        }

        void AddHudHiddenRenderersFromRoot(GameObject root)
        {
            if (root == null)
                return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                AddHudHiddenRenderer(renderers[i]);
        }

        void AddHudHiddenRenderer(Renderer renderer)
        {
            if (renderer == null || hudHiddenRendererSet.Contains(renderer))
                return;

            hudHiddenRendererSet.Add(renderer);
            hudHiddenRendererStates.Add(new RendererState
            {
                renderer = renderer,
                enabled = renderer.enabled,
            });
        }

        void OnValidate()
        {
            if (hudHiddenReferenceLayers == null || hudHiddenReferenceLayers.Length == 0)
                hudHiddenReferenceLayers = (ReferenceVisualLayer[])Enum.GetValues(typeof(ReferenceVisualLayer));
            else if (IsLegacyFullHiddenReferenceLayerSet(hudHiddenReferenceLayers))
            {
                Array.Resize(ref hudHiddenReferenceLayers, hudHiddenReferenceLayers.Length + 1);
                hudHiddenReferenceLayers[hudHiddenReferenceLayers.Length - 1] =
                    ReferenceVisualLayer.MirroredCueThroughTarget;
            }
        }

        static bool IsLegacyFullHiddenReferenceLayerSet(ReferenceVisualLayer[] layers)
        {
            Array allLayers = Enum.GetValues(typeof(ReferenceVisualLayer));
            if (layers == null || layers.Length != allLayers.Length - 1)
                return false;
            if (ContainsReferenceLayer(layers, ReferenceVisualLayer.MirroredCueThroughTarget))
                return false;

            for (int i = 0; i < allLayers.Length; i++)
            {
                ReferenceVisualLayer layer = (ReferenceVisualLayer)allLayers.GetValue(i);
                if (layer == ReferenceVisualLayer.MirroredCueThroughTarget)
                    continue;
                if (!ContainsReferenceLayer(layers, layer))
                    return false;
            }

            return true;
        }

        static bool ContainsReferenceLayer(ReferenceVisualLayer[] layers, ReferenceVisualLayer target)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == target)
                    return true;
            }

            return false;
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

        void ApplyCompressedHudLayout()
        {
            if (rawImage == null)
                return;

            var aspect = rawImage.GetComponent<AspectRatioFitter>();
            if (aspect != null && aspect.enabled)
                aspect.enabled = false;

            var layout = rawImage.GetComponent<LayoutElement>();
            if (layout != null)
                layout.preferredHeight = CompressedHudImageHeight;
        }

        float EffectiveHudOrthoSize()
        {
            float baseOrthoSize = Mathf.Max(0.001f, orthoSize);
            if (!fitStrikeAreaToHudWindow)
                return baseOrthoSize;

            float fill = Mathf.Clamp(strikeAreaViewportFill, 0.1f, 1f);
            float strikeAreaHeight = Mathf.Max(0.001f, ballDiameter + crosshairVerticalExtraMeters);
            float fitOrthoSize = strikeAreaHeight / (2f * fill);
            return Mathf.Min(baseOrthoSize, fitOrthoSize);
        }

        float CurrentHudOrthoSize()
        {
            return currentHudOrthoSize > 0f ? currentHudOrthoSize : EffectiveHudOrthoSize();
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
                float pxHeight = worldHeight / (2f * CurrentHudOrthoSize()) * r.height;
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
            float pxHeight = worldHeight / (2f * CurrentHudOrthoSize()) * r.height;

            idealAnswerVBar.gameObject.SetActive(true);
            idealAnswerVBar.sizeDelta = new Vector2(HudLineWidthCanvasUnits(), pxHeight);
            idealAnswerVBar.anchoredPosition = new Vector2(markerX, centerY);
        }

        float HudLineWidthCanvasUnits()
        {
            return HudLengthCanvasUnits(Mathf.Max(1f, hudLineWidthPx));
        }

        float HudLengthCanvasUnits(float screenPixels)
        {
            float scale = 1f;
            if (rawImage != null && rawImage.canvas != null)
                scale = Mathf.Max(0.001f, rawImage.canvas.scaleFactor);

            return Mathf.Max(1f, screenPixels) / scale;
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
            if (offsetLabel != null)
                offsetLabel.text = "dx = 0.0 mm";
        }

        public void ResetAimSelectionToDefault()
        {
            hasClicked = false;
            UpdateDefaultAimSelection();
            UpdateCrosshair();
            UpdateObjectBallCenterCross();
            UpdateObjectBallClockFace();
            UpdateOffsetLabel();
        }

        void EnsureObjectBallCenterCross()
        {
            if (rawImageRect == null)
                return;

            if (objectBallCenterHBar != null && objectBallCenterVBar != null)
                return;

            Transform existing = rawImageRect.Find("ObjectBallCenterCross");
            RectTransform marker;
            if (existing != null)
            {
                marker = existing.GetComponent<RectTransform>();
            }
            else
            {
                var markerGo = new GameObject("ObjectBallCenterCross", typeof(RectTransform));
                markerGo.transform.SetParent(rawImageRect, false);
                marker = markerGo.GetComponent<RectTransform>();
            }

            marker.anchorMin = new Vector2(0f, 0f);
            marker.anchorMax = new Vector2(0f, 0f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = Vector2.zero;

            if (objectBallCenterHBar == null)
                objectBallCenterHBar = EnsureCenterCrossBar(marker, "H");
            if (objectBallCenterVBar == null)
                objectBallCenterVBar = EnsureCenterCrossBar(marker, "V");
            EnsureObjectBallClockFace(marker);
        }

        RectTransform EnsureCenterCrossBar(RectTransform marker, string name)
        {
            Transform existing = marker.Find(name);
            RectTransform bar;
            if (existing != null)
            {
                bar = existing.GetComponent<RectTransform>();
            }
            else
            {
                var barGo = new GameObject(name, typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(marker, false);
                bar = barGo.GetComponent<RectTransform>();
            }

            bar.anchorMin = new Vector2(0.5f, 0.5f);
            bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.anchoredPosition = Vector2.zero;
            var image = bar.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.1f, 1f, 0.1f, 1f);
                image.raycastTarget = false;
            }
            return bar;
        }

        void EnsureObjectBallClockFace(RectTransform marker)
        {
            if (objectBallClockFace == null)
            {
                Transform existing = marker.Find("ObjectBallClockFace");
                if (existing != null)
                    objectBallClockFace = existing.GetComponent<RectTransform>();
            }

            if (objectBallClockFace == null)
            {
                var clockGo = new GameObject("ObjectBallClockFace", typeof(RectTransform));
                clockGo.transform.SetParent(marker, false);
                objectBallClockFace = clockGo.GetComponent<RectTransform>();
            }

            objectBallClockFace.anchorMin = new Vector2(0.5f, 0.5f);
            objectBallClockFace.anchorMax = new Vector2(0.5f, 0.5f);
            objectBallClockFace.pivot = new Vector2(0.5f, 0.5f);
            objectBallClockFace.anchoredPosition = Vector2.zero;
            objectBallClockFace.sizeDelta = Vector2.zero;

            for (int hour = 1; hour <= 12; hour++)
            {
                EnsureClockTick(objectBallClockFace, hour);
                EnsureClockLabel(objectBallClockFace, hour);
            }
        }

        RectTransform EnsureClockTick(RectTransform clock, int hour)
        {
            string name = "Tick" + hour;
            Transform existing = clock.Find(name);
            RectTransform tick;
            if (existing != null)
            {
                tick = existing.GetComponent<RectTransform>();
            }
            else
            {
                var tickGo = new GameObject(name, typeof(RectTransform), typeof(Image));
                tickGo.transform.SetParent(clock, false);
                tick = tickGo.GetComponent<RectTransform>();
            }

            var image = tick.GetComponent<Image>();
            if (image != null)
            {
                image.color = objectBallClockColor;
                image.raycastTarget = false;
            }

            return tick;
        }

        RectTransform EnsureClockLabel(RectTransform clock, int hour)
        {
            string name = "Label" + hour;
            Transform existing = clock.Find(name);
            RectTransform labelRt;
            TextMeshProUGUI label;
            if (existing != null)
            {
                labelRt = existing.GetComponent<RectTransform>();
                label = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var labelGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(clock, false);
                labelRt = labelGo.GetComponent<RectTransform>();
                label = labelGo.GetComponent<TextMeshProUGUI>();
            }

            if (label != null)
            {
                label.text = hour.ToString(CultureInfo.InvariantCulture);
                label.fontSize = 11f;
                label.color = objectBallClockColor;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            return labelRt;
        }

        void UpdateObjectBallCenterCross()
        {
            if (rawImageRect == null)
                return;

            EnsureObjectBallCenterCross();
            if (objectBallCenterHBar == null || objectBallCenterVBar == null)
                return;

            Rect r = rawImageRect.rect;
            if (r.width <= 0f || r.height <= 0f)
                return;

            RectTransform marker = objectBallCenterHBar.parent as RectTransform;
            if (marker == null)
                return;

            marker.gameObject.SetActive(true);
            marker.anchoredPosition = new Vector2(r.width * 0.5f, r.height * 0.5f);

            float lineWidth = HudLineWidthCanvasUnits();
            float length = HudLengthCanvasUnits(objectBallCenterCrossLengthPx);
            objectBallCenterHBar.sizeDelta = new Vector2(length, lineWidth);
            objectBallCenterVBar.sizeDelta = new Vector2(lineWidth, length);
        }

        void UpdateObjectBallClockFace()
        {
            if (rawImageRect == null)
                return;

            EnsureObjectBallCenterCross();
            if (objectBallClockFace == null)
                return;

            objectBallClockFace.gameObject.SetActive(showObjectBallClockFace);
            if (!showObjectBallClockFace)
                return;

            Rect r = rawImageRect.rect;
            if (r.width <= 0f || r.height <= 0f)
                return;

            float ballDiameterPx = ballDiameter / (2f * CurrentHudOrthoSize()) * r.height;
            float radius = ballDiameterPx * 0.5f * Mathf.Max(0.1f, objectBallClockRadiusScale);
            float labelRadius = radius + HudLengthCanvasUnits(objectBallClockLabelOffsetPx);
            float tickLength = HudLengthCanvasUnits(objectBallClockTickLengthPx);
            float lineWidth = HudLineWidthCanvasUnits();

            for (int hour = 1; hour <= 12; hour++)
            {
                float angle = (90f - hour * 30f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                RectTransform tick = EnsureClockTick(objectBallClockFace, hour);
                tick.gameObject.SetActive(true);
                tick.anchorMin = new Vector2(0.5f, 0.5f);
                tick.anchorMax = new Vector2(0.5f, 0.5f);
                tick.pivot = new Vector2(0.5f, 0.5f);
                tick.sizeDelta = new Vector2(lineWidth, tickLength);
                tick.anchoredPosition = dir * radius;
                tick.localRotation = Quaternion.Euler(0f, 0f, -hour * 30f);

                RectTransform label = EnsureClockLabel(objectBallClockFace, hour);
                label.gameObject.SetActive(true);
                label.anchorMin = new Vector2(0.5f, 0.5f);
                label.anchorMax = new Vector2(0.5f, 0.5f);
                label.pivot = new Vector2(0.5f, 0.5f);
                label.sizeDelta = new Vector2(22f, 16f);
                label.anchoredPosition = dir * labelRadius;
            }
        }

        void UpdateDefaultAimSelection()
        {
            if (aimManager == null || aimCamera == null || rawImageRect == null || cueBall == null || targetBall == null)
            {
                lastClickNormalized = new Vector2(0.5f, 0.5f);
                return;
            }

            if (aimManager.currentPocket == null || aimManager.table == null)
            {
                lastClickNormalized = new Vector2(0.5f, 0.5f);
                return;
            }

            Vector3 pottingPoint = aimManager.GetPottingPointFor(aimManager.currentPocket);
            var ideal = PoolAimTrainer.GeometryCore.AimSolver.Compute(
                cueBall.Center, targetBall.Center, pottingPoint, aimManager.table.ballRadius);
            if (!ideal.solvable)
            {
                lastClickNormalized = new Vector2(0.5f, 0.5f);
                return;
            }

            Vector3 viewport = aimCamera.WorldToViewportPoint(ideal.ghostBallCenter);
            lastClickNormalized = new Vector2(Mathf.Clamp01(viewport.x), 0.5f);
        }

        void UpdateOffsetLabel()
        {
            if (offsetLabel == null) return;
            float aspect = 1f;
            if (aimCamera != null && aimCamera.pixelHeight > 0)
                aspect = (float)aimCamera.pixelWidth / aimCamera.pixelHeight;
            float dxWorldM = (lastClickNormalized.x - 0.5f) * 2f * CurrentHudOrthoSize() * aspect;
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
            float deltaU = deltaWorld / (2f * CurrentHudOrthoSize() * aspect);
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
