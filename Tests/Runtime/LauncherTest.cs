// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DeNA.Anjin.Agents;
using DeNA.Anjin.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable CS0618 // Type or member is obsolete

namespace DeNA.Anjin
{
    [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor)] // Fail on Unity 2019 Linux editor
    [SuppressMessage("ApiDesign", "RS0030")]
    public class LauncherTest
    {
        [SetUp]
        public void SetUp()
        {
            Assume.That(AutopilotState.Instance.IsRunning, Is.False);
        }

        private static AutopilotSettings CreateAutopilotSettings(int lifespanSec = 2)
        {
            var settings = (AutopilotSettings)ScriptableObject.CreateInstance(typeof(AutopilotSettings));
            settings.sceneAgentMaps = new List<SceneAgentMap>();
            settings.lifespanSec = lifespanSec;
            return settings;
        }

        [Test]
        public async Task Launch_InPlayMode_RunAutopilot()
        {
            var state = AutopilotState.Instance;
            state.launchFrom = LaunchType.EditorPlayMode;
            state.settings = CreateAutopilotSettings();
            Launcher.LaunchAutopilot();
            await Task.Delay(200);

            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.EditorPlayMode));
            Assert.That(state.IsRunning, Is.True, "AutopilotState is running");

            var autopilot = Object.FindObjectOfType<Autopilot>();
            Assert.That((bool)autopilot, Is.True, "Autopilot object is alive");

            // Tear down
            await autopilot.TerminateAsync(ExitCode.Normally);
        }

        [Test]
        public async Task Launch_StopAutopilotThatRunInPlayMode_KeepPlayMode()
        {
            var settings = CreateAutopilotSettings();
            await Launcher.LaunchAutopilotAsync(settings);

            var state = AutopilotState.Instance;
            Assert.That(state.IsRunning, Is.False, "AutopilotState is terminated");
#if UNITY_EDITOR
            Assert.That(EditorApplication.isPlaying, Is.True, "Keep play mode");
#endif
        }

        [Test]
        public async Task Stop_TerminateAutopilotAndKeepPlayMode()
        {
            var state = AutopilotState.Instance;
            state.launchFrom = LaunchType.EditorPlayMode;
            state.settings = CreateAutopilotSettings(0); // endless
            Launcher.LaunchAutopilot();
            await Task.Delay(200);

            var autopilot = Object.FindObjectOfType<Autopilot>();
            await autopilot.TerminateAsync(ExitCode.Normally);
            // Note: If Autopilot stops for life before Stop, a NullReference exception is raised here.

            Assert.That(state.IsRunning, Is.False, "AutopilotState is terminated");
#if UNITY_EDITOR
            Assert.That(EditorApplication.isPlaying, Is.True, "Keep play mode");
#endif
        }

        [Test]
        public async Task LaunchAutopilotAsync_RunAutopilot()
        {
            var agent = (DoNothingAgent)ScriptableObject.CreateInstance(typeof(DoNothingAgent));
            agent.lifespanSec = 1;

            var settings = CreateAutopilotSettings();
            settings.fallbackAgent = agent;

            var beforeTimestamp = Time.time;
            await Launcher.LaunchAutopilotAsync(settings);

            var afterTimestamp = Time.time;
            Assert.That(afterTimestamp - beforeTimestamp, Is.GreaterThan(2f), "Autopilot is running for 2 seconds");

            var state = AutopilotState.Instance;
            Assert.That(state.IsRunning, Is.False, "AutopilotState is terminated");
            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.NotSet), "Launch from is reset");
            Assert.That(state.exitCode, Is.EqualTo(ExitCode.Normally), "Exit code is reset");
        }

        [Test]
        [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor, RuntimePlatform.LinuxEditor)]
        public async Task LaunchAutopilotAsync_WithAssetFile_RunAutopilot()
        {
            const string AssetPath = "Packages/com.dena.anjin/Tests/TestAssets/AutopilotSettingsForTests.asset";

            var beforeTimestamp = Time.time;
            await Launcher.LaunchAutopilotAsync(AssetPath);

            var afterTimestamp = Time.time;
            Assert.That(afterTimestamp - beforeTimestamp, Is.GreaterThan(2f), "Autopilot is running for 2 seconds");

            var state = AutopilotState.Instance;
            Assert.That(state.IsRunning, Is.False, "AutopilotState is terminated");
            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.NotSet), "Launch from is reset");
            Assert.That(state.exitCode, Is.EqualTo(ExitCode.Normally), "Exit code is reset");
        }
    }
}
