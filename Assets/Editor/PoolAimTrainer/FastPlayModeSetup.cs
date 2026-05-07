#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Auto-configures "Enter Play Mode Options" on project load so that entering
    /// play mode skips Domain Reload and Scene Reload. This cuts play-mode entry
    /// from 3~10s to under 1s.
    ///
    /// Runs once per editor session (check via SessionState).
    /// </summary>
    [InitializeOnLoad]
    public static class FastPlayModeSetup
    {
        const string AppliedKey = "PoolAimTrainer.FastPlayMode.Applied";

        static FastPlayModeSetup()
        {
            if (SessionState.GetBool(AppliedKey, false)) return;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload;

            SessionState.SetBool(AppliedKey, true);
            Debug.Log("[FastPlayModeSetup] Enter Play Mode Options enabled: skip Domain & Scene reload.");
        }
    }
}
#endif
