﻿// Copyright (c) 2023 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DeNA.Anjin.Settings;
using DeNA.Anjin.TestDoubles;
using DeNA.Anjin.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeNA.Anjin.Agents
{
    public class OneTimeAgentTest
    {
        private static int s_childAgentCount;

        private static SpyAgent CreateChildAgent(long lifespanMillis)
        {
            var agent = ScriptableObject.CreateInstance<SpyAgent>();
            agent.name = $"ChildAgent {++s_childAgentCount}";
            agent.lifespanMillis = lifespanMillis;
            return agent;
        }

        [Test]
        public async Task Run_cancelTask_stopAgent()
        {
            var childAgent = CreateChildAgent(5000);

            var agent = ScriptableObject.CreateInstance<OneTimeAgent>();
            agent.Logger = Debug.unityLogger;
            agent.Random = new RandomFactory(0).CreateRandom();
            agent.name = nameof(Run_cancelTask_stopAgent);
            agent.agent = childAgent;

            var gameObject = new GameObject();
            var cancellationToken = gameObject.GetCancellationTokenOnDestroy();
            var task = agent.Run(cancellationToken);
            await UniTask.NextFrame();

            Object.DestroyImmediate(gameObject);
            await UniTask.NextFrame();

            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));

            LogAssert.Expect(LogType.Log, $"Enter {agent.name}.Run()");
            LogAssert.Expect(LogType.Log, $"Exit {agent.name}.Run()");
        }

        [Test]
        public async Task Run_markWasExecuted()
        {
            var childAgent = CreateChildAgent(100);

            var agent = ScriptableObject.CreateInstance<OneTimeAgent>();
            agent.Logger = Debug.unityLogger;
            agent.Random = new RandomFactory(0).CreateRandom();
            agent.name = nameof(Run_cancelTask_stopAgent);
            agent.agent = childAgent;

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var cancellationToken = cancellationTokenSource.Token;
                var task = agent.Run(cancellationToken);
                await UniTask.Delay(500); // Consider overhead

                Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
                Assert.That(agent.WasExecuted, Is.True);
            }

            LogAssert.Expect(LogType.Log, $"Enter {agent.name}.Run()");
            LogAssert.Expect(LogType.Log, $"Exit {agent.name}.Run()");
        }

        [Test]
        public async Task Run_wasExecuted_notExecuteChildAgent()
        {
            var childAgent = CreateChildAgent(100);

            var agent = ScriptableObject.CreateInstance<OneTimeAgent>();
            agent.Logger = Debug.unityLogger;
            agent.Random = new RandomFactory(0).CreateRandom();
            agent.name = nameof(Run_cancelTask_stopAgent);
            agent.agent = childAgent;
            agent.WasExecuted = true;

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                var task = agent.Run(token);
                await UniTask.Delay(500); // Consider overhead

                Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
                Assert.That(childAgent.CompleteCount, Is.EqualTo(0)); // Skip run child agent
            }

            LogAssert.Expect(LogType.Log, new Regex($"^Skip {agent.name}"));
        }

        [Test]
        public async Task Run_setLoggerAndRandomInstanceToChildAgent()
        {
            var childAgent = CreateChildAgent(100);

            var agent = ScriptableObject.CreateInstance<OneTimeAgent>();
            agent.Logger = Debug.unityLogger;
            agent.Random = new RandomFactory(0).CreateRandom();
            agent.name = nameof(Run_cancelTask_stopAgent);
            agent.agent = childAgent;

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                var task = agent.Run(token);
                await UniTask.Delay(500); // Consider overhead

                Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
                Assert.That(childAgent.Logger, Is.EqualTo(agent.Logger)); // Instances inherited from parent
                Assert.That(childAgent.Random, Is.EqualTo(agent.Random)); // Instances inherited from parent
            }

            LogAssert.Expect(LogType.Log, $"Enter {agent.name}.Run()");
            LogAssert.Expect(LogType.Log, $"Exit {agent.name}.Run()");
        }

        [Test]
        public async Task ResetExecutedFlagWhenLaunchAutopilot()
        {
            var sut = ScriptableObject.CreateInstance<OneTimeAgent>();
            sut.WasExecuted = true;

            var settings = ScriptableObject.CreateInstance<AutopilotSettings>();
            settings.lifespanSec = 1;
            await Launcher.LaunchAutopilotAsync(settings);

            sut = ScriptableObject.CreateInstance<OneTimeAgent>(); // Reload because domain reloaded
            Assert.That(sut.WasExecuted, Is.False);                // was reset
        }
    }
}
