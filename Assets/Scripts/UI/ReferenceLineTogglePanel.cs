using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PoolAimTrainer.Core;

namespace PoolAimTrainer.UI
{
    public class ReferenceLineTogglePanel : MonoBehaviour
    {
        const string PanelObjectName = "ReferenceLineTogglePanel";
        const string RightPanelObjectName = "RightPanel";
        const string RightPanelToggleButtonName = "RightPanelToggleButton";
        const string LegacyGhostButtonName = "BtnGhost";
        const string PuzzleSectionObjectName = "PuzzleSection";
        const string SectionTitleObjectName = "Title";
        const float RightPanelWidth = 330f;
        const float HudImagePreferredHeight = 190f;
        const float RightPanelHorizontalPadding = 16f;
        const float HudInset = 32f;
        const float ToggleGridSpacing = 6f;

        public ReferenceLineVisibility visibility;
        public AimManager aimManager;
        public Vector2 anchoredPosition = new Vector2(12f, -12f);

        readonly Dictionary<ReferenceVisualLayer, Toggle> layerToggles =
            new Dictionary<ReferenceVisualLayer, Toggle>();

        Toggle masterToggle;
        RectTransform panel;

        void Start()
        {
            WireDefaults();
            BuildPanel();
            RefreshToggles();
        }

        void OnDestroy()
        {
            if (visibility != null)
                visibility.Changed -= OnVisibilityChanged;
        }

        public void WireDefaults()
        {
            if (visibility == null)
            {
                visibility = ReferenceLineVisibility.Active
                    ?? FindObjectOfType<ReferenceLineVisibility>();
            }
            if (visibility != null)
            {
                visibility.ActivateAsCurrent();
                visibility.Changed -= OnVisibilityChanged;
                visibility.Changed += OnVisibilityChanged;
            }

            if (aimManager == null)
                aimManager = FindObjectOfType<AimManager>();
        }

        public void BuildPanel()
        {
            if (visibility == null || panel != null)
                return;

            HideLegacyGhostButton();

            Canvas canvas = FindOrCreateCanvas();
            Transform parent = FindRightPanel(canvas);
            bool embeddedInRightPanel = parent != null;
            if (!embeddedInRightPanel)
                parent = canvas.transform;
            else
                ConfigureRightPanel(parent, canvas);

            var panelGo = new GameObject(PanelObjectName, typeof(RectTransform), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(parent, false);
            panel = panelGo.GetComponent<RectTransform>();

            if (embeddedInRightPanel)
            {
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = Vector2.zero;
            }
            else
            {
                panel.anchorMin = new Vector2(0f, 1f);
                panel.anchorMax = new Vector2(0f, 1f);
                panel.pivot = new Vector2(0f, 1f);
                panel.anchoredPosition = anchoredPosition;
                panel.sizeDelta = new Vector2(220f, 0f);

                var bg = panelGo.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.42f);
                bg.raycastTarget = true;
            }

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = embeddedInRightPanel
                ? new RectOffset(0, 0, 0, 0)
                : new RectOffset(8, 8, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateHeader(panelGo.transform, "参考线");
            CreateGhostToggle(panelGo.transform, embeddedInRightPanel ? 120f : 180f);
            masterToggle = CreateToggle(panelGo.transform, "全部参考线", visibility.masterVisible);
            masterToggle.onValueChanged.AddListener(visibility.SetMasterVisible);

            Transform layerParent = panelGo.transform;
            if (embeddedInRightPanel)
                layerParent = CreateLayerGrid(panelGo.transform);

            foreach (var layer in visibility.Layers)
            {
                Toggle toggle = CreateToggle(
                    layerParent,
                    layer.label,
                    layer.visible,
                    embeddedInRightPanel ? 120f : 180f);
                ReferenceVisualLayer captured = layer.layer;
                toggle.onValueChanged.AddListener(value => visibility.SetLayerVisible(captured, value));
                layerToggles[captured] = toggle;
            }

            if (embeddedInRightPanel)
            {
                EnsureCollapsibleSection(parent.Find(PuzzleSectionObjectName), "出题");
                EnsureCollapsibleSection(panel, "参考线");
            }
        }

        void CreateGhostToggle(Transform parent, float labelWidth)
        {
            if (aimManager == null)
                aimManager = FindObjectOfType<AimManager>();
            if (aimManager == null)
                return;

            Toggle ghostToggle = CreateToggle(parent, "Ghost Ball", aimManager.showGhostBall, labelWidth);
            var ghost = ghostToggle.gameObject.AddComponent<GhostToggleButton>();
            ghost.aimManager = aimManager;
        }

        static void HideLegacyGhostButton()
        {
            var legacy = GameObject.Find(LegacyGhostButtonName);
            if (legacy == null)
                return;

            var ghostToggle = legacy.GetComponent<GhostToggleButton>();
            if (ghostToggle != null)
                ghostToggle.enabled = false;
            legacy.SetActive(false);
        }

        static Transform CreateLayerGrid(Transform parent)
        {
            var gridGo = new GameObject("LayersGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridGo.transform.SetParent(parent, false);

            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(ToggleGridSpacing, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;
            float cellWidth = (RightPanelWidth - RightPanelHorizontalPadding - ToggleGridSpacing) * 0.5f;
            grid.cellSize = new Vector2(cellWidth, 22f);

            var layout = gridGo.GetComponent<LayoutElement>();
            layout.preferredHeight = 22f * 7f + 4f * 6f;
            return gridGo.transform;
        }

        static void EnsureCollapsibleSection(Transform section, string fallbackTitle)
        {
            if (section == null)
                return;

            Transform title = section.Find(SectionTitleObjectName);
            if (title == null)
            {
                CreateHeader(section, fallbackTitle);
                title = section.Find(SectionTitleObjectName);
                if (title == null)
                    return;
                title.SetAsFirstSibling();
            }

            var label = title.GetComponent<TextMeshProUGUI>();
            if (label == null)
                return;

            string titleText = NormalizeCollapsibleTitle(label.text);
            if (string.IsNullOrWhiteSpace(titleText))
                titleText = fallbackTitle;

            label.raycastTarget = true;

            var button = title.GetComponent<Button>() ?? title.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                bool shouldCollapse = HasVisibleSectionContent(section, title);
                SetSectionCollapsed(section, title, titleText, shouldCollapse);
            });

            SetSectionCollapsed(section, title, titleText, false);
        }

        static bool HasVisibleSectionContent(Transform section, Transform title)
        {
            foreach (Transform child in section)
            {
                if (child != title && child.gameObject.activeSelf)
                    return true;
            }
            return false;
        }

        static void SetSectionCollapsed(Transform section, Transform title, string titleText, bool collapsed)
        {
            foreach (Transform child in section)
            {
                if (child != title)
                    child.gameObject.SetActive(!collapsed);
            }

            var label = title.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = (collapsed ? "> " : "v ") + titleText;
        }

        static string NormalizeCollapsibleTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Trim();
            if (text.StartsWith("v "))
                return text.Substring(2);
            if (text.StartsWith("> "))
                return text.Substring(2);
            if (text.StartsWith("▼ "))
                return text.Substring(2);
            if (text.StartsWith("▶ "))
                return text.Substring(2);
            return text;
        }

        static void ConfigureRightPanel(Transform rightPanel, Canvas canvas)
        {
            var rt = rightPanel.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(RightPanelWidth, rt.sizeDelta.y);

            var layout = rightPanel.GetComponent<LayoutElement>() ?? rightPanel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = RightPanelWidth;

            var hudImage = rightPanel.Find("HudSection/RawImage");
            if (hudImage != null)
            {
                var hudLayout = hudImage.GetComponent<LayoutElement>() ?? hudImage.gameObject.AddComponent<LayoutElement>();
                hudLayout.preferredHeight = HudImagePreferredHeight;
            }

            EnsureCollapseButton(rightPanel, canvas);
        }

        static void EnsureCollapseButton(Transform rightPanel, Canvas canvas)
        {
            Transform existing = canvas.transform.Find(RightPanelToggleButtonName);
            if (existing != null)
            {
                WireCollapseButton(existing.gameObject, rightPanel);
                return;
            }

            var buttonGo = new GameObject(RightPanelToggleButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvas.transform, false);
            var rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-10f, -10f);
            rt.sizeDelta = new Vector2(32f, 28f);

            var bg = buttonGo.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = "<";
            label.fontSize = 18f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            WireCollapseButton(buttonGo, rightPanel);
        }

        static void WireCollapseButton(GameObject buttonGo, Transform rightPanel)
        {
            var button = buttonGo.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                bool nextVisible = !rightPanel.gameObject.activeSelf;
                rightPanel.gameObject.SetActive(nextVisible);
                SetCollapseButtonLabel(buttonGo, nextVisible);
            });
            SetCollapseButtonLabel(buttonGo, rightPanel.gameObject.activeSelf);
        }

        static void SetCollapseButtonLabel(GameObject buttonGo, bool panelVisible)
        {
            var label = buttonGo.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = panelVisible ? "<" : ">";
        }

        static Transform FindRightPanel(Canvas canvas)
        {
            var panelGo = GameObject.Find(RightPanelObjectName);
            if (panelGo == null)
                return null;

            var rightPanel = panelGo.GetComponent<RectTransform>();
            if (rightPanel == null)
                return null;

            var ownerCanvas = rightPanel.GetComponentInParent<Canvas>();
            return ownerCanvas == canvas ? rightPanel : null;
        }

        void OnVisibilityChanged()
        {
            RefreshToggles();
            if (aimManager != null)
                aimManager.ForceRefresh();
        }

        void RefreshToggles()
        {
            if (visibility == null)
                return;

            if (masterToggle != null)
                masterToggle.SetIsOnWithoutNotify(visibility.masterVisible);

            foreach (var layer in visibility.Layers)
            {
                if (layerToggles.TryGetValue(layer.layer, out Toggle toggle) && toggle != null)
                    toggle.SetIsOnWithoutNotify(layer.visible);
            }
        }

        static Canvas FindOrCreateCanvas()
        {
            var existing = GameObject.Find("Canvas");
            if (existing != null)
            {
                var canvas = existing.GetComponent<Canvas>();
                if (canvas != null)
                    return canvas;
            }

            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var created = go.GetComponent<Canvas>();
            created.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return created;
        }

        static void CreateHeader(Transform parent, string text)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 22f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 14f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
        }

        static Toggle CreateToggle(Transform parent, string labelText, bool isOn, float labelWidth = 180f)
        {
            var go = new GameObject(labelText, typeof(RectTransform), typeof(Toggle), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 22f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(go.transform, false);
            var boxRT = box.GetComponent<RectTransform>();
            boxRT.sizeDelta = new Vector2(14f, 14f);
            var boxImage = box.GetComponent<Image>();
            boxImage.color = new Color(1f, 1f, 1f, 0.22f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            var checkRT = check.GetComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.22f, 0.22f);
            checkRT.anchorMax = new Vector2(0.78f, 0.78f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;
            var checkImage = check.GetComponent<Image>();
            checkImage.color = new Color(0.2f, 0.95f, 1f, 1f);

            var text = new GameObject("Label", typeof(RectTransform));
            text.transform.SetParent(go.transform, false);
            var textRT = text.GetComponent<RectTransform>();
            textRT.sizeDelta = new Vector2(labelWidth, 20f);
            var label = text.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 12f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = isOn;
            return toggle;
        }
    }
}
