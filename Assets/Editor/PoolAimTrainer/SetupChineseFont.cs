#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Copies a Chinese font TTF from Windows Fonts into the project, creates a dynamic
    /// TMP Font Asset (SDF atlas populated on-demand), and force-assigns it to every
    /// TMP_Text in the active scene. Also adds it to TMP's fallback list.
    ///
    /// Menu: PoolAimTrainer -> Setup Chinese Font
    /// </summary>
    public static class SetupChineseFont
    {
        const string TargetAssetPath = "Assets/Fonts/Chinese_TMP.asset";
        const string DestFontDir = "Assets/Fonts";

        static readonly string[] CandidateSystemFonts = new[]
        {
            "C:/Windows/Fonts/msyh.ttc",
            "C:/Windows/Fonts/msyh.ttf",
            "C:/Windows/Fonts/simhei.ttf",
            "C:/Windows/Fonts/simsun.ttc",
            "C:/Windows/Fonts/simsun.ttf",
            "C:/Windows/Fonts/Deng.ttf",
        };

        [MenuItem("PoolAimTrainer/Setup Chinese Font")]
        public static void Setup()
        {
            if (!Directory.Exists(DestFontDir)) Directory.CreateDirectory(DestFontDir);

            string chosenSource = null;
            foreach (var p in CandidateSystemFonts)
                if (File.Exists(p)) { chosenSource = p; break; }

            if (chosenSource == null)
            {
                EditorUtility.DisplayDialog("No Chinese font found",
                    "Could not locate a Chinese font under C:/Windows/Fonts/. Please download a Chinese TTF (e.g. Source Han Sans), put it in Assets/Fonts/, then try again.",
                    "OK");
                return;
            }
            Debug.Log($"[SetupChineseFont] Using source font: {chosenSource}");

            string ext = Path.GetExtension(chosenSource);
            string destTtf = DestFontDir + "/Chinese" + ext;
            if (!File.Exists(destTtf))
            {
                File.Copy(chosenSource, destTtf, overwrite: false);
                Debug.Log($"[SetupChineseFont] Copied to: {destTtf}");
            }
            AssetDatabase.Refresh();

            var font = AssetDatabase.LoadAssetAtPath<Font>(destTtf);
            if (font == null)
            {
                EditorUtility.DisplayDialog("Font import failed",
                    $"Unity did not import {destTtf} as a Font. Try manually re-importing via Project view right-click > Reimport.",
                    "OK");
                return;
            }
            Debug.Log($"[SetupChineseFont] Font loaded: {font.name}");

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetAssetPath);
            TMP_FontAsset tmpAsset;
            if (existing != null)
            {
                tmpAsset = existing;
                Debug.Log($"[SetupChineseFont] Using existing TMP asset at {TargetAssetPath}");
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
                    atlasPopulationMode: AtlasPopulationMode.Dynamic);
                AssetDatabase.CreateAsset(tmpAsset, TargetAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[SetupChineseFont] Created TMP asset at {TargetAssetPath}");
            }

            AddToTmpFallbacks(tmpAsset);

            var texts = Object.FindObjectsOfType<TMP_Text>(includeInactive: true);
            int reassigned = 0;
            foreach (var t in texts)
            {
                t.font = tmpAsset;
                t.ForceMeshUpdate();
                EditorUtility.SetDirty(t);
                reassigned++;
            }
            Debug.Log($"[SetupChineseFont] Reassigned {reassigned} TMP_Text components");

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Done",
                $"Imported: {Path.GetFileName(chosenSource)}\nReassigned {reassigned} TMP_Text(s) to Chinese font.\n\nPress Play — Chinese characters should now render correctly.",
                "OK");
        }

        static void AddToTmpFallbacks(TMP_FontAsset fontAsset)
        {
            var settings = TMP_Settings.instance;
            if (settings == null) { Debug.LogWarning("[SetupChineseFont] TMP_Settings.instance is null"); return; }
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_fallbackFontAssets");
            if (prop == null) { Debug.LogWarning("[SetupChineseFont] m_fallbackFontAssets property not found"); return; }
            for (int i = 0; i < prop.arraySize; i++)
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset) return;
            int idx = prop.arraySize;
            prop.arraySize++;
            prop.GetArrayElementAtIndex(idx).objectReferenceValue = fontAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupChineseFont] Added to TMP_Settings fallback list");
        }
    }
}
#endif
