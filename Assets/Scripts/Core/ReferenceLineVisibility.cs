using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoolAimTrainer.Core
{
    public class ReferenceLineVisibility : MonoBehaviour
    {
        [Serializable]
        public class LayerState
        {
            public ReferenceVisualLayer layer;
            public string label;
            public bool visible = true;
        }

        public static ReferenceLineVisibility Active { get; private set; }

        public const string DefaultPlayerPrefsKeyPrefix = "PoolAimTrainer.ReferenceLineVisibility.";

        [Tooltip("总开关：关闭时隐藏所有参考线，但保留每条线自己的开关状态")]
        public bool masterVisible = true;
        [Tooltip("本地存储键名前缀；一般保持默认即可")]
        public string playerPrefsKeyPrefix = DefaultPlayerPrefsKeyPrefix;

        [SerializeField]
        List<LayerState> layers = new List<LayerState>();

        readonly Dictionary<ReferenceVisualLayer, bool> runtimeLayerVisibilityOverrides =
            new Dictionary<ReferenceVisualLayer, bool>();

        public event Action Changed;

        public IReadOnlyList<LayerState> Layers
        {
            get
            {
                EnsureDefaultLayers();
                return layers;
            }
        }

        void Awake()
        {
            ActivateAsCurrent();
        }

        void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        void OnValidate()
        {
            EnsureDefaultLayers();
        }

        public void ActivateAsCurrent()
        {
            EnsureDefaultLayers();
            LoadPreferences();
            Active = this;
        }

        public static bool IsLayerVisible(ReferenceVisualLayer layer)
        {
            return Active == null || Active.IsVisible(layer);
        }

        public bool IsVisible(ReferenceVisualLayer layer)
        {
            EnsureDefaultLayers();
            bool visible = masterVisible && GetOrCreateState(layer).visible;
            if (runtimeLayerVisibilityOverrides.TryGetValue(layer, out bool runtimeVisible))
                visible = visible && runtimeVisible;
            return visible;
        }

        public bool IsLayerEnabled(ReferenceVisualLayer layer)
        {
            EnsureDefaultLayers();
            return GetOrCreateState(layer).visible;
        }

        public string GetLabel(ReferenceVisualLayer layer)
        {
            EnsureDefaultLayers();
            return GetOrCreateState(layer).label;
        }

        public void SetMasterVisible(bool visible)
        {
            EnsureDefaultLayers();
            if (masterVisible == visible) return;
            masterVisible = visible;
            SavePreferences();
            NotifyChanged();
        }

        public void ToggleMaster()
        {
            SetMasterVisible(!masterVisible);
        }

        public void SetLayerVisible(ReferenceVisualLayer layer, bool visible)
        {
            EnsureDefaultLayers();
            LayerState state = GetOrCreateState(layer);
            if (state.visible == visible) return;
            state.visible = visible;
            SavePreferences();
            NotifyChanged();
        }

        public void ToggleLayer(ReferenceVisualLayer layer)
        {
            SetLayerVisible(layer, !IsLayerEnabled(layer));
        }

        public void SetRuntimeLayerVisibilityOverride(ReferenceVisualLayer layer, bool visible)
        {
            if (runtimeLayerVisibilityOverrides.TryGetValue(layer, out bool current) && current == visible)
                return;

            runtimeLayerVisibilityOverrides[layer] = visible;
            NotifyChanged();
        }

        public void ClearRuntimeLayerVisibilityOverrides()
        {
            if (runtimeLayerVisibilityOverrides.Count == 0)
                return;

            runtimeLayerVisibilityOverrides.Clear();
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        public void EnsureDefaultLayers()
        {
            foreach (ReferenceVisualLayer layer in Enum.GetValues(typeof(ReferenceVisualLayer)))
                AddMissing(layer);
        }

        public void LoadPreferences()
        {
            EnsureDefaultLayers();
            string prefix = EffectivePrefsPrefix();

            string masterKey = prefix + "Master";
            if (PlayerPrefs.HasKey(masterKey))
                masterVisible = PlayerPrefs.GetInt(masterKey, masterVisible ? 1 : 0) != 0;

            for (int i = 0; i < layers.Count; i++)
            {
                LayerState state = layers[i];
                if (state == null) continue;
                string key = LayerKey(prefix, state.layer);
                if (PlayerPrefs.HasKey(key))
                    state.visible = PlayerPrefs.GetInt(key, state.visible ? 1 : 0) != 0;
            }
        }

        public void SavePreferences()
        {
            EnsureDefaultLayers();
            string prefix = EffectivePrefsPrefix();
            PlayerPrefs.SetInt(prefix + "Master", masterVisible ? 1 : 0);

            for (int i = 0; i < layers.Count; i++)
            {
                LayerState state = layers[i];
                if (state == null) continue;
                PlayerPrefs.SetInt(LayerKey(prefix, state.layer), state.visible ? 1 : 0);
            }

            PlayerPrefs.Save();
        }

        void AddMissing(ReferenceVisualLayer layer)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].layer == layer)
                {
                    if (ShouldUseDefaultLabel(layer, layers[i].label))
                        layers[i].label = DefaultLabel(layer);
                    return;
                }
            }

            layers.Add(new LayerState
            {
                layer = layer,
                label = DefaultLabel(layer),
                visible = true,
            });
        }

        LayerState GetOrCreateState(ReferenceVisualLayer layer)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].layer == layer)
                    return layers[i];
            }

            var state = new LayerState
            {
                layer = layer,
                label = DefaultLabel(layer),
                visible = true,
            };
            layers.Add(state);
            return state;
        }

        static string DefaultLabel(ReferenceVisualLayer layer)
        {
            return layer switch
            {
                ReferenceVisualLayer.CueToGhost => "白线 主球-Ghost",
                ReferenceVisualLayer.ObjectToPocket => "绿线 子球-袋口",
                ReferenceVisualLayer.CueThroughTarget => "红线 主球穿子球",
                ReferenceVisualLayer.MirroredCueThroughTarget => "镜像红线",
                ReferenceVisualLayer.TargetBallPath => "橙线 模拟子球",
                ReferenceVisualLayer.ManualCuePath => "蓝线 手动击球",
                ReferenceVisualLayer.EstimatedAimLine => "洋红线 估瞄线",
                ReferenceVisualLayer.CutAngleArc => "∠1 黄弧 红-绿夹角",
                ReferenceVisualLayer.AimVsTargetArc => "∠2 青弧 偏差角",
                ReferenceVisualLayer.EstimatedAimToTargetPathAngle => "∠3 洋红-橙角",
                ReferenceVisualLayer.TargetPathToCueThroughAngle => "∠4 橙-红角",
                ReferenceVisualLayer.ToleranceFanArea => "容错着色区",
                ReferenceVisualLayer.ToleranceLowerTargetPath => "容错下边界子球线",
                ReferenceVisualLayer.ToleranceUpperTargetPath => "容错上边界子球线",
                _ => layer.ToString(),
            };
        }

        static bool ShouldUseDefaultLabel(ReferenceVisualLayer layer, string label)
        {
            if (string.IsNullOrEmpty(label))
                return true;

            return layer switch
            {
                ReferenceVisualLayer.CutAngleArc => label == "黄弧 切角" || label == "∠1 黄弧 切角",
                ReferenceVisualLayer.AimVsTargetArc => label == "青弧 偏差角",
                ReferenceVisualLayer.EstimatedAimToTargetPathAngle => label == "洋红-橙角",
                ReferenceVisualLayer.TargetPathToCueThroughAngle => label == "橙-红角",
                _ => false,
            };
        }

        string EffectivePrefsPrefix()
        {
            return string.IsNullOrEmpty(playerPrefsKeyPrefix)
                ? DefaultPlayerPrefsKeyPrefix
                : playerPrefsKeyPrefix;
        }

        static string LayerKey(string prefix, ReferenceVisualLayer layer)
        {
            return prefix + "Layer." + layer;
        }
    }
}
