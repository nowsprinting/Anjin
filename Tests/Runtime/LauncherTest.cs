// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DeNA.Anjin.Settings;
using DeNA.Anjin.TestDoubles;
using NUnit.Framework;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace DeNA.Anjin
{
    /// <summary>
    /// Launch autopilot tests from Play Mode.
    /// </summary>
    /// <remarks>
    /// Note: Test cases of launch/terminate from Edit Mode are in <see cref="Editor.UI.Settings.AutopilotSettingsEditorTest"/>.
    /// </remarks>
    [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
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
        public async Task LaunchAutopilotAsync_RunAutopilot()
        {
            var beforeTimestamp = Time.time;
            var settings = CreateAutopilotSettings();
            await Launcher.LaunchAutopilotAsync(settings);

            var runningTime = Time.time - beforeTimestamp;
            Assert.That(runningTime, Is.GreaterThan(2f).And.LessThan(3f), "Autopilot is running for 2 seconds");

            var state = AutopilotState.Instance;
            Assert.That(state.IsRunning, Is.False, "AutopilotState is terminated");
            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.NotSet), "Launch from was reset");
            Assert.That(state.exitCode, Is.EqualTo(ExitCode.Normally), "Exit code was reset");
        }

        [Test]
        public async Task LaunchAutopilotAsync_WithAssetFile_RunAutopilot()
        {
            const string AssetPath = "Packages/com.dena.anjin/Tests/TestAssets/AutopilotSettingsForTests.asset";

            var beforeTimestamp = Time.time;
            await Launcher.LaunchAutopilotAsync(AssetPath);

            var runningTime = Time.time - beforeTimestamp;
            Assert.That(runningTime, Is.GreaterThan(2f).And.LessThan(3f), "Autopilot is running for 2 seconds");

            var state = AutopilotState.Instance;
            Assert.That(state.IsRunning, Is.False, "AutopilotState is terminated");
            Assert.That(state.launchFrom, Is.EqualTo(LaunchType.NotSet), "Launch from was reset");
            Assert.That(state.exitCode, Is.EqualTo(ExitCode.Normally), "Exit code was reset");
        }

        [Test]
        public async Task LaunchAutopilotAsync_CallMethodWithInitializeOnLaunchAutopilotAttribute()
        {
            SpyInitializeOnLaunchAutopilot.Reset();

            var settings = CreateAutopilotSettings();
            await Launcher.LaunchAutopilotAsync(settings);

            Assert.That(SpyInitializeOnLaunchAutopilot.IsCallInitializeOnLaunchAutopilotMethod, Is.True);
        }
    }
}
