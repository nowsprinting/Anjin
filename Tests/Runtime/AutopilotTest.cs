// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DeNA.Anjin.Agents;
using DeNA.Anjin.Settings;
using DeNA.Anjin.TestDoubles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeNA.Anjin
{
    [TestFixture]
    public class AutopilotTest
    {
        private static DoNothingAgent CreateAgent()
        {
            var agent = ScriptableObject.CreateInstance<DoNothingAgent>();
            agent.name = "TestAgent";
            return agent;
        }

        private static AutopilotSettings CreateAutopilotSettings(int lifespanSec)
        {
            var settings = (AutopilotSettings)ScriptableObject.CreateInstance(typeof(AutopilotSettings));
            settings.sceneAgentMaps = new List<SceneAgentMap>();
            settings.fallbackAgent = CreateAgent(); // infinity alive
            settings.lifespanSec = lifespanSec;
            return settings;
        }

        [Test]
        public async Task TerminateAsync_Normally_DestroyedAutopilotAndAgentObjects()
        {
            var settings = CreateAutopilotSettings(2);
            Launcher.LaunchAutopilotAsync(settings).Forget();
            await UniTask.Delay(500); // wait for launch

            var autopilot = Object.FindObjectOfType<Autopilot>();
            Assume.That(autopilot, Is.Not.Null, "Autopilot is running");

            var agents = Object.FindObjectsOfType<AgentInspector>();
            Assume.That(agents, Is.Not.Empty, "Agents are running");

            await autopilot.TerminateAsync(ExitCode.Normally);
            await UniTask.NextFrame(); // wait for destroy

            autopilot = Object.FindObjectOfType<Autopilot>(); // re-find after terminated
            Assert.That(autopilot, Is.Null, "Autopilot was destroyed");

            agents = Object.FindObjectsOfType<AgentInspector>(); // re-find after terminated
            Assert.That(agents, Is.Empty, "Agents were destroyed");
        }

        [Test]
        public async Task Start_LoggerIsNotSet_UsingDefaultLogger()
        {
            var autopilotSettings = ScriptableObject.CreateInstance<AutopilotSettings>();
            autopilotSettings.lifespanSec = 1;

            await Launcher.LaunchAutopilotAsync(autopilotSettings);

            LogAssert.Expect(LogType.Log, "Launched autopilot"); // using console logger
        }

        [Test]
        public async Task Start_LoggerSpecified_UsingSpecifiedLogger()
        {
            var autopilotSettings = ScriptableObject.CreateInstance<AutopilotSettings>();
            autopilotSettings.lifespanSec = 1;
            var spyLogger = ScriptableObject.CreateInstance<SpyLoggerAsset>();
            autopilotSettings.loggerAsset = spyLogger;

            await Launcher.LaunchAutopilotAsync(autopilotSettings);

            Assert.That(spyLogger.Logs, Does.Contain("Launched autopilot")); // using spy logger
            LogAssert.NoUnexpectedReceived(); // not write to console
        }
    }
}
