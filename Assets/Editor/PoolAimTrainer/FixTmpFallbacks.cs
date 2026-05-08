#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Clears stale / destroyed font-asset references from TMP_Settings fallback list.
    /// Call when you see MissingReferenceException from TMP_MaterialManager.GetFallbackMaterial.
    ///
    /// Menu: PoolAimTrainer -> Fix TMP Fallbacks
    /// </summary>
    public static class FixTmpFallbacks
    {
        [MenuItem("PoolAimTrainer/Fix TMP Fallbacks")]
        public static void Fix()
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                Debug.LogError("[FixTmpFallbacks] TMP_Settings.instance is null");
                return;
            }
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_fallbackFontAssets");
            if (prop == null)
            {
                Debug.LogError("[FixTmpFallbacks] m_fallbackFontAssets property not found");
                return;
            }

            int removed = 0;
            for (int i = prop.arraySize - 1; i >= 0; i--)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                var obj = elem.objectReferenceValue;
                if (obj == null)
                {
                    prop.DeleteArrayElementAtIndex(i);
                    removed++;
                    continue;
                }
                var fontAsset = obj as TMP_FontAsset;
                if (fontAsset == null || fontAsset.material == null || fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0 || fontAsset.atlasTextures[0] == null)
                {
                    prop.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixTmpFallbacks] Removed {removed} stale fallback entries. Remaining: {prop.arraySize}");
        }
    }
}
#endif
