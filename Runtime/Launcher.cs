// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DeNA.Anjin.Attributes;
using DeNA.Anjin.Settings;
using DeNA.Anjin.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeNA.Anjin
{
    /// <summary>
    /// Launch process when run on Unity editor.
    /// </summary>
    public static class Launcher
    {
#if UNITY_EDITOR
        /// <summary>
        /// Reset event handlers even if domain reload is off
        /// <see href="https://docs.unity3d.com/ja/current/Manual/ConfigurableEnterPlayMode.html"/>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetEventHandlers()
        {
            EditorApplication.playModeStateChanged -= OnChangePlayModeState;
        }
#endif

        /// <summary>
        /// Run autopilot
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void LaunchAutopilot()
        {
            var state = AutopilotState.Instance;
            if (!state.IsRunning)
            {
                return; // Normally play mode (not run autopilot)
            }

#if UNITY_EDITOR
            if (!state.IsLaunchFromPlayMode)
            {
                EditorApplication.playModeStateChanged += OnChangePlayModeState;
            }
#endif

            ScreenshotStore.CleanDirectories();

            CallAttachedInitializeOnLaunchAutopilotAttributeMethods();

            var autopilot = new GameObject(nameof(Autopilot)).AddComponent<Autopilot>();
            Object.DontDestroyOnLoad(autopilot);
        }

        private static void CallAttachedInitializeOnLaunchAutopilotAttributeMethods()
        {
            foreach (var methodInfo in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(x => x.GetTypes())
                         .SelectMany(x => x.GetMethods())
                         .Where(x => x.GetCustomAttributes(typeof(InitializeOnLaunchAutopilotAttribute), false).Any()))
            {
                methodInfo.Invoke(null, null); // static method only
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Stop autopilot on play mode exit event when run on Unity editor.
        /// Not called when invoked from play mode (not registered in event listener).
        /// </summary>
        /// <param name="playModeStateChange"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void OnChangePlayModeState(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnChangePlayModeState;

            var state = AutopilotState.Instance;
            switch (state.launchFrom)
            {
                case LaunchType.EditorEditMode:
                    Debug.Log("Exit play mode");
                    break;

                case LaunchType.Commandline:
                    // Exit Unity when returning from play mode to edit mode.
                    // Because it may freeze when exiting without going through edit mode.
                    var exitCode = (int)state.exitCode;
                    Debug.Log($"Exit Unity-editor by autopilot, exit code={exitCode}");
                    EditorApplication.Exit(exitCode);
                    break;

                default:
#pragma warning disable S3928
                    throw new ArgumentOutOfRangeException(nameof(state.launchFrom));
#pragma warning restore S3928
            }
        }
#endif

        /// <summary>
        /// Run autopilot from Play Mode test.
        /// If an error is detected in running, it will be output to `LogError` and the test will fail.
        /// </summary>
        /// <param name="settings">Autopilot settings</param>
        public static async UniTask LaunchAutopilotAsync(AutopilotSettings settings)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Not support run from Edit Mode");
            }
#endif
            var state = AutopilotState.Instance;
            if (state.IsRunning)
            {
                throw new InvalidOperationException("Autopilot is already running");
            }

            state.launchFrom = LaunchType.PlayModeTests;
            state.settings = settings;
            Launcher.LaunchAutopilot();

            await UniTask.WaitUntil(() => !state.IsRunning);
        }

        /// <summary>
        /// Run autopilot from Play Mode test.
        /// If an error is detected in running, it will be output to `LogError` and the test will fail.
        /// </summary>
        /// <param name="autopilotSettingsPath">Asset file path for autopilot settings</param>
        public static async UniTask LaunchAutopilotAsync(string autopilotSettingsPath)
        {
#if UNITY_EDITOR
            var settings = AssetDatabase.LoadAssetAtPath<AutopilotSettings>(autopilotSettingsPath);
            await LaunchAutopilotAsync(settings);
#else
            throw new NotImplementedException();
#endif
        }
    }
}
#endif
