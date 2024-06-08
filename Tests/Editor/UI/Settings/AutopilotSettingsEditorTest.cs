// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using DeNA.Anjin.Editor.YieldInstructions;
using DeNA.Anjin.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

#pragma warning disable CS0618 // Type or member is obsolete

namespace DeNA.Anjin.Editor.UI.Settings
{
    /// <summary>
    /// Test case about AutopilotSettingsEditor and starting Autopilot from edit mode.
    /// Start/Stop from play mode tests are implemented in Runtime/LauncherTest.cs
    /// </summary>
    [TestFixture]
    [SuppressMessage("ApiDesign", "RS0030")]
    public class AutopilotSettingsEditorTest
    {
        private const string SettingsPath = "Packages/com.dena.anjin/Tests/TestAssets/AutopilotSettingsForTests.asset";

        /// <summary>
        /// Detect disabled domain reloading. Because test will not work as intended.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (EditorSettings.enterPlayModeOptionsEnabled)
            {
                Assume.That(EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload),
                    Is.False, "Enabled domain reloading option");
            }
        }

        [UnityTest]
        public IEnumerator Launch_InEditMode_RunAutopilot()
        {
            AutopilotState.Instance.Reset();

            var settings = AssetDatabase.LoadAssetAtPath<AutopilotSettings>(SettingsPath);
            var editor = (AutopilotSettingsEditor)UnityEditor.Editor.CreateEditor(settings);
            editor.Launch(); // Note: Can not call editor.OnInspectorGUI() and GUILayout.Button()
            yield return new WaitForAutopilotToRun();

            var state = AutopilotState.Instance;
            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.EditorEditMode));
            Assert.That(state.IsRunning, Is.True);

            // Tear down
            yield return AutopilotSettingsEditor.Stop().ToCoroutine();
        }

        [UnityTest]
        public IEnumerator Launch_InPlayMode_RunAutopilot()
        {
            AutopilotState.Instance.Reset();
            yield return new EnterPlayMode();

            var settings = AssetDatabase.LoadAssetAtPath<AutopilotSettings>(SettingsPath);
            var editor = (AutopilotSettingsEditor)UnityEditor.Editor.CreateEditor(settings);
            editor.Launch(); // Note: Can not call editor.OnInspectorGUI() and GUILayout.Button()
            yield return new WaitForAutopilotToRun();

            var state = AutopilotState.Instance;
            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.EditorPlayMode));
            Assert.That(state.IsRunning, Is.True);

            // Tear down
            yield return AutopilotSettingsEditor.Stop().ToCoroutine();
        }

        [UnityTest]
        public IEnumerator Stop_LaunchFromEditMode_TerminateAutopilotAndExitPlayMode()
        {
            AutopilotState.Instance.Reset();

            var settings = AssetDatabase.LoadAssetAtPath<AutopilotSettings>(SettingsPath);
            var editor = (AutopilotSettingsEditor)UnityEditor.Editor.CreateEditor(settings);
            editor.Launch(); // Note: Can not call editor.OnInspectorGUI() and GUILayout.Button()
            yield return new WaitForAutopilotToRun();

            yield return AutopilotSettingsEditor.Stop().ToCoroutine();
            yield return null;
            Assert.That(Object.FindObjectOfType<Autopilot>(), Is.Null, "Autopilot is terminated");
            Assert.That(EditorApplication.isPlaying, Is.False, "Exit play mode");
        }

        [UnityTest]
        public IEnumerator Stop_LaunchFromPlayMode_TerminateAutopilotAndKeepPlayMode()
        {
            AutopilotState.Instance.Reset();
            yield return new EnterPlayMode();

            var settings = AssetDatabase.LoadAssetAtPath<AutopilotSettings>(SettingsPath);
            var editor = (AutopilotSettingsEditor)UnityEditor.Editor.CreateEditor(settings);
            editor.Launch(); // Note: Can not call editor.OnInspectorGUI() and GUILayout.Button()
            yield return new WaitForAutopilotToRun();

            yield return AutopilotSettingsEditor.Stop().ToCoroutine();
            yield return null;
            Assert.That(Object.FindObjectOfType<Autopilot>(), Is.Null, "Autopilot is terminated");
            Assert.That(EditorApplication.isPlaying, Is.True, "Keep play mode");
        }
    }
}
