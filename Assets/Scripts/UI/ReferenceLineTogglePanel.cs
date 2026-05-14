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
        const float RightPanelContentWidth = 330f;
        const float RightPanelWidth = 350f;
        const float HudImagePreferredHeight = 190f;
        const float RightPanelHorizontalPadding = 16f;
        const float HudInset = 32f;
        const float ToggleGridSpacing = 6f;
        static readonly Color PuzzleCardColor = new Color(0.11f, 0.11f, 0.11f, 0.9f);
        static readonly Color PuzzleCardOutlineColor = new Color(0.32f, 0.32f, 0.32f, 0.55f);
        static readonly Color SectionDividerColor = new Color(1f, 1f, 1f, 0.08f);
        static Sprite rightPanelRoundedSprite;
        static Sprite puzzleCardRoundedSprite;
        static Sprite arrowUpSprite;
        static Sprite arrowDownSprite;

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

            Canvas canvas = GetComponentInParent<Canvas>() ?? FindOrCreateCanvas();
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
                EnsurePuzzleSectionMockup(parent.Find(PuzzleSectionObjectName));
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
            float cellWidth = (RightPanelContentWidth - RightPanelHorizontalPadding - ToggleGridSpacing) * 0.5f;
            grid.cellSize = new Vector2(cellWidth, 22f);

            var layout = gridGo.GetComponent<LayoutElement>();
            layout.preferredHeight = 22f * 7f + 4f * 6f;
            return gridGo.transform;
        }

        static void EnsurePuzzleSectionMockup(Transform section)
        {
            if (section == null)
                return;

            var background = section.GetComponent<Image>() ?? section.gameObject.AddComponent<Image>();
            background.color = PuzzleCardColor;
            background.sprite = GetPuzzleCardRoundedSprite();
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            var outline = section.GetComponent<Outline>() ?? section.gameObject.AddComponent<Outline>();
            outline.effectColor = PuzzleCardOutlineColor;
            outline.effectDistance = new Vector2(1f, -1f);

            var layout = section.GetComponent<VerticalLayoutGroup>() ?? section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Transform title = section.Find(SectionTitleObjectName);
            if (title == null)
            {
                CreateHeader(section, "出题");
                title = section.Find(SectionTitleObjectName);
                if (title == null)
                    return;
                title.SetAsFirstSibling();
            }

            var label = title.GetComponent<TextMeshProUGUI>();
            if (label == null)
                return;

            label.text = "出题";
            label.fontSize = 15f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = true;

            var titleLayout = title.GetComponent<LayoutElement>() ?? title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 24f;

            var button = title.GetComponent<Button>() ?? title.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.RemoveAllListeners();

            var icon = EnsureTitleCollapseIcon(title);
            Transform divider = EnsureTitleDivider(section, title);
            button.onClick.AddListener(() =>
            {
                bool shouldCollapse = HasVisiblePuzzleContent(section, title, divider);
                SetPuzzleSectionCollapsed(section, title, divider, shouldCollapse);
            });

            SetPuzzleSectionCollapsed(section, title, divider, false);
        }

        static Image EnsureTitleCollapseIcon(Transform title)
        {
            Transform existing = title.Find("CollapseIcon");
            if (existing != null)
            {
                var oldText = existing.GetComponent<TextMeshProUGUI>();
                if (oldText != null)
                    DestroyRuntimeOrImmediate(oldText);
                return existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }

            var go = new GameObject("CollapseIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(title, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(22f, 0f);

            var icon = go.GetComponent<Image>();
            icon.color = Color.white;
            icon.raycastTarget = false;
            return icon;
        }

        static Transform EnsureTitleDivider(Transform section, Transform title)
        {
            Transform divider = section.Find("TitleDivider");
            if (divider == null)
            {
                var dividerGo = new GameObject("TitleDivider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                dividerGo.transform.SetParent(section, false);
                divider = dividerGo.transform;
            }

            divider.SetSiblingIndex(title.GetSiblingIndex() + 1);
            var image = divider.GetComponent<Image>();
            image.color = SectionDividerColor;
            image.raycastTarget = false;

            var layout = divider.GetComponent<LayoutElement>();
            layout.preferredHeight = 1f;
            layout.flexibleHeight = 0f;
            return divider;
        }

        static bool HasVisiblePuzzleContent(Transform section, Transform title, Transform divider)
        {
            foreach (Transform child in section)
            {
                if (child != title && child != divider && child.gameObject.activeSelf)
                    return true;
            }
            return false;
        }

        static void SetPuzzleSectionCollapsed(Transform section, Transform title, Transform divider, bool collapsed)
        {
            foreach (Transform child in section)
            {
                if (child == title || child == divider)
                    continue;

                var layout = child.GetComponent<LayoutElement>();
                bool hiddenByLayout = layout != null && layout.ignoreLayout;
                child.gameObject.SetActive(!collapsed && !hiddenByLayout);
            }

            var label = title.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = "出题";

            var icon = title.Find("CollapseIcon")?.GetComponent<Image>();
            if (icon != null)
                icon.sprite = collapsed ? GetCollapseArrowDownSprite() : GetCollapseArrowUpSprite();
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

            var background = rightPanel.GetComponent<Image>() ?? rightPanel.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            background.sprite = GetRightPanelRoundedSprite();
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            var group = rightPanel.GetComponent<VerticalLayoutGroup>() ?? rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 10, 10);
            group.spacing = 8f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            var hudImage = rightPanel.Find("HudSection/RawImage");
            if (hudImage != null)
            {
                var hudLayout = hudImage.GetComponent<LayoutElement>() ?? hudImage.gameObject.AddComponent<LayoutElement>();
                hudLayout.preferredHeight = HudImagePreferredHeight;
            }

            EnsureCollapseButton(rightPanel, canvas);
        }

        static Sprite GetRightPanelRoundedSprite()
        {
            if (rightPanelRoundedSprite != null)
                return rightPanelRoundedSprite;

            const int size = 32;
            const float radius = 8f;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = "RightPanelRoundedBackground",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(radius - px, 0f, px - (size - radius));
                    float dy = Mathf.Max(radius - py, 0f, py - (size - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);

            rightPanelRoundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            rightPanelRoundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return rightPanelRoundedSprite;
        }

        static Sprite GetPuzzleCardRoundedSprite()
        {
            if (puzzleCardRoundedSprite != null)
                return puzzleCardRoundedSprite;

            const int size = 32;
            const float radius = 7f;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = "PuzzleSectionRoundedBackground",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(radius - px, 0f, px - (size - radius));
                    float dy = Mathf.Max(radius - py, 0f, py - (size - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);

            puzzleCardRoundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            puzzleCardRoundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return puzzleCardRoundedSprite;
        }

        static Sprite GetCollapseArrowUpSprite()
        {
            if (arrowUpSprite == null)
                arrowUpSprite = CreateCollapseArrowSprite("CollapseArrowUp", true);
            return arrowUpSprite;
        }

        static Sprite GetCollapseArrowDownSprite()
        {
            if (arrowDownSprite == null)
                arrowDownSprite = CreateCollapseArrowSprite("CollapseArrowDown", false);
            return arrowDownSprite;
        }

        static Sprite CreateCollapseArrowSprite(string name, bool up)
        {
            const int size = 32;
            const float lineWidth = 3.2f;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, clear);
            }

            Vector2 left = up ? new Vector2(7f, 12f) : new Vector2(7f, 20f);
            Vector2 center = up ? new Vector2(16f, 21f) : new Vector2(16f, 11f);
            Vector2 right = up ? new Vector2(25f, 12f) : new Vector2(25f, 20f);

            DrawLine(texture, left, center, lineWidth, Color.white);
            DrawLine(texture, center, right, lineWidth, Color.white);
            texture.Apply(false, true);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static void DrawLine(Texture2D texture, Vector2 a, Vector2 b, float width, Color color)
        {
            int w = texture.width;
            int h = texture.height;
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq <= Mathf.Epsilon)
                return;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                    Vector2 closest = a + ab * t;
                    float distance = Vector2.Distance(p, closest);
                    float alpha = Mathf.Clamp01(width * 0.5f + 0.5f - distance);
                    if (alpha <= 0f)
                        continue;

                    Color current = texture.GetPixel(x, y);
                    if (alpha > current.a)
                        texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
            }
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
            Transform panelTransform = FindChildRecursive(canvas.transform, RightPanelObjectName);
            if (panelTransform == null)
                return null;

            var rightPanel = panelTransform.GetComponent<RectTransform>();
            if (rightPanel == null)
                return null;

            var ownerCanvas = rightPanel.GetComponentInParent<Canvas>();
            return ownerCanvas == canvas ? rightPanel : null;
        }

        static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (root.name == childName)
                return root;

            foreach (Transform child in root)
            {
                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        static void DestroyRuntimeOrImmediate(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
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
