#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// One-click: copies a Chinese font TTF from Windows Fonts into the project,
    /// creates a dynamic TMP Font Asset (SDF atlas populated on-demand), and adds it
    /// to TMP's fallback list so every TMP_Text renders Chinese correctly.
    ///
    /// Menu: PoolAimTrainer -> Setup Chinese Font
    /// </summary>
    public static class SetupChineseFont
    {
        const string TargetAssetPath = "Assets/Fonts/Chinese_TMP.asset";
        const string DestFontDir = "Assets/Fonts";

        static readonly string[] CandidateSystemFonts = new[]
        {
            "C:/Windows/Fonts/msyh.ttc",    // Microsoft YaHei (best for simplified Chinese)
            "C:/Windows/Fonts/msyh.ttf",
            "C:/Windows/Fonts/simhei.ttf",
            "C:/Windows/Fonts/simsun.ttc",
            "C:/Windows/Fonts/Deng.ttf",
        };

        [MenuItem("PoolAimTrainer/Setup Chinese Font")]
        public static void Setup()
        {
            if (!Directory.Exists(DestFontDir)) Directory.CreateDirectory(DestFontDir);

            string chosenSource = null;
            foreach (var p in CandidateSystemFonts)
            {
                if (File.Exists(p)) { chosenSource = p; break; }
            }
            if (chosenSource == null)
            {
                EditorUtility.DisplayDialog("No Chinese font found",
                    "Could not locate a Chinese font under C:/Windows/Fonts/. Please download a Chinese TTF (e.g. Source Han Sans), put it in Assets/Fonts/, then try again with a manual font reference.",
                    "OK");
                return;
            }

            string ext = Path.GetExtension(chosenSource);
            string destTtf = DestFontDir + "/Chinese" + ext;
            if (!File.Exists(destTtf))
            {
                File.Copy(chosenSource, destTtf, overwrite: false);
            }
            AssetDatabase.Refresh();

            var font = AssetDatabase.LoadAssetAtPath<Font>(destTtf);
            if (font == null)
            {
                EditorUtility.DisplayDialog("Font import failed",
                    $"Unity did not import {destTtf} as a Font. Try manually selecting it in the Project window.",
                    "OK");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetAssetPath);
            TMP_FontAsset tmpAsset;
            if (existing != null)
            {
                tmpAsset = existing;
            }
            else
            {
                tmpAsset = TMP_FontAsset.CreateFontAsset(
                    font,
                    samplingPointSize: 90,
                    atlasPadding: 9,
                    renderMode: GlyphRenderMode.SDFAA,
                    atlasWidth: 1024,
                    atlasHeight: 1024,
                    atlasPopulationMode: AtlasPopulationMode.Dynamic,
                    enableMultiAtlasSupport: true);
                AssetDatabase.CreateAsset(tmpAsset, TargetAssetPath);
                AssetDatabase.SaveAssets();
            }

            AddToTmpFallbacks(tmpAsset);

            var texts = Object.FindObjectsOfType<TMP_Text>(includeInactive: true);
            int assigned = 0;
            foreach (var t in texts)
            {
                if (t.font == null || t.font.name.Contains("LiberationSans"))
                {
                    t.font = tmpAsset;
                    assigned++;
                }
                else
                {
                    // Also push into the per-text fallback list so Chinese glyphs resolve
                    // even if the primary font is the English default.
                    if (t.font.fallbackFontAssetTable != null
                        && !t.font.fallbackFontAssetTable.Contains(tmpAsset))
                    {
                        t.font.fallbackFontAssetTable.Add(tmpAsset);
                    }
                }
                EditorUtility.SetDirty(t);
            }

            EditorUtility.DisplayDialog("Done",
                $"Chinese font ({Path.GetFileName(chosenSource)}) imported and registered as TMP fallback. Reassigned {assigned} TMP_Text(s) directly. Press Play to verify Chinese displays correctly.",
                "OK");
        }

        static void AddToTmpFallbacks(TMP_FontAsset fontAsset)
        {
            var settings = TMP_Settings.instance;
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_fallbackFontAssets");
            if (prop == null) return;
            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset) return;
            }
            int idx = prop.arraySize;
            prop.arraySize++;
            prop.GetArrayElementAtIndex(idx).objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
