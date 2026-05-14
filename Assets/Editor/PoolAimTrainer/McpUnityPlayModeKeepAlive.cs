#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PoolAimTrainer.EditorTools
{
    /// <summary>
    /// Keeps the MCP Unity WebSocket available after entering Play Mode.
    /// The upstream package deliberately stops the server on ExitingEditMode;
    /// this project uses MCP for visual smoke tests, so restart it once Play Mode
    /// has finished transitioning.
    /// </summary>
    [InitializeOnLoad]
    public static class McpUnityPlayModeKeepAlive
    {
        const string SettingsTypeName = "McpUnity.Unity.McpUnitySettings";
        const string ServerTypeName = "McpUnity.Unity.McpUnityServer";

        static McpUnityPlayModeKeepAlive()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += StartServerIfNeeded;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += StartServerIfNeeded;
            }
        }

        static void StartServerIfNeeded()
        {
            try
            {
                Type settingsType = FindType(SettingsTypeName);
                Type serverType = FindType(ServerTypeName);
                if (settingsType == null || serverType == null) return;

                PropertyInfo settingsInstanceProperty = settingsType.GetProperty(
                    "Instance",
                    BindingFlags.Public | BindingFlags.Static);
                FieldInfo autoStartField = settingsType.GetField("AutoStartServer");
                if (settingsInstanceProperty == null || autoStartField == null) return;

                object settings = settingsInstanceProperty.GetValue(null);
                bool autoStart = settings != null && (bool)autoStartField.GetValue(settings);
                if (!autoStart) return;

                PropertyInfo serverInstanceProperty = serverType.GetProperty(
                    "Instance",
                    BindingFlags.Public | BindingFlags.Static);
                PropertyInfo isListeningProperty = serverType.GetProperty(
                    "IsListening",
                    BindingFlags.Public | BindingFlags.Instance);
                MethodInfo startServerMethod = serverType.GetMethod(
                    "StartServer",
                    BindingFlags.Public | BindingFlags.Instance);
                if (serverInstanceProperty == null ||
                    isListeningProperty == null ||
                    startServerMethod == null)
                {
                    return;
                }

                object server = serverInstanceProperty.GetValue(null);
                if (server == null) return;

                bool isListening = (bool)isListeningProperty.GetValue(server);
                if (!isListening)
                {
                    startServerMethod.Invoke(server, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PoolAimTrainer] MCP keep-alive restart failed: " +
                    ex.GetBaseException().Message);
            }
        }

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }
    }
}
#endif
