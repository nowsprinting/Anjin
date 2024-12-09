﻿// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeNA.Anjin.Settings
{
    /// <summary>
    /// Autopilot run state.
    /// 
    /// Auto-generated by Anjin and used internally.
    /// It is recommended to add `Assets/AutopilotState.asset` to your .gitignore file
    /// </summary>
    public class AutopilotState : ScriptableObject
    {
        /// <summary>
        /// Launch type
        /// </summary>
        [HideInInspector]
        public LaunchType launchFrom = LaunchType.NotSet;

        /// <summary>
        /// Run autopilot settings instance
        /// </summary>
        [HideInInspector]
        [CanBeNull]
        public AutopilotSettings settings;

        /// <summary>
        /// Exit code when terminate autopilot from commandline interface
        /// </summary>
        [HideInInspector]
        public ExitCode exitCode;

        /// <summary>
        /// Reset run state
        /// </summary>
        public void Reset()
        {
            launchFrom = LaunchType.NotSet;
            settings = null;
            exitCode = ExitCode.Normally;
#if UNITY_EDITOR && UNITY_2020_3_OR_NEWER
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this); // Note: Sync with virtual players of MPPM package
#endif
        }

        /// <summary>
        /// Is running (readonly)
        /// </summary>
        /// <remarks>
        /// Judged by the exist of Settings.
        /// See also EditorApplication.isPlayingOrWillChangePlaymode
        /// </remarks>
        public bool IsRunning
        {
            get
            {
                return this.settings;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// This state Is invalid (read-only).
        /// True if Autopilot is running and in Edit Mode, it is required to reset.
        /// </summary>
        internal bool IsInvalidState => IsRunning && !EditorApplication.isPlayingOrWillChangePlaymode;
#endif

        [NonSerialized]
        private static AutopilotState s_instance;

        /// <summary>
        /// <c>AutopilotState</c> instance
        /// </summary>
        [NotNull]
        public static AutopilotState Instance
        {
            get
            {
                if (!s_instance)
                {
#if UNITY_EDITOR
                    var stateArray = AssetDatabase
                        .FindAssets($"t:{nameof(AutopilotState)}")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<AutopilotState>)
                        //.Where(b => b)
                        .ToArray();
                    // Use files in the project instead of specifying paths.

                    if (stateArray.Length > 1)
                    {
                        Debug.LogWarning("Find multiple AutopilotState files!");
                    }

                    s_instance = stateArray.FirstOrDefault();
#endif
                    if (!s_instance)
                    {
                        Debug.Log("Create new AutopilotState instance");
                        s_instance = CreateInstance<AutopilotState>();
#if UNITY_EDITOR
                        AssetDatabase.CreateAsset(s_instance, $"Assets/{nameof(AutopilotState)}.asset");
                        // Note: Create asset file only running in Editor.
#endif
                    }
                }

                return s_instance;
            }
        }
    }
}
