// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using DeNA.Anjin.Attributes;
using DeNA.Anjin.Reporters.Slack;
using DeNA.Anjin.Settings;
using UnityEngine;

namespace DeNA.Anjin.Agents
{
    public enum HandlingBehavior
    {
        /// <summary>
        /// Ignore this log type.
        /// </summary>
        Ignore,

        /// <summary>
        /// Reporting only when handle this log type.
        /// </summary>
        ReportOnly,

        /// <summary>
        /// Terminate Autopilot when handle this log type.
        /// </summary>
        TerminateAutopilot,
    }

    /// <summary>
    /// Error log handling using <c>Application.logMessageReceived</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "New ErrorHandlerAgent", menuName = "Anjin/Error Handler Agent", order = 50)]
    public class ErrorHandlerAgent : AbstractAgent
    {
        /// <summary>
        /// Autopilot terminates and/or reports when an Exception is detected in the log.
        /// </summary>
        public HandlingBehavior handleException = HandlingBehavior.TerminateAutopilot;

        /// <summary>
        /// Autopilot terminates and/or reports when an Error is detected in the log.
        /// </summary>
        public HandlingBehavior handleError = HandlingBehavior.TerminateAutopilot;

        /// <summary>
        /// Autopilot terminates and/or reports when an Assert is detected in the log.
        /// </summary>
        public HandlingBehavior handleAssert = HandlingBehavior.TerminateAutopilot;

        /// <summary>
        /// Autopilot terminates and/or reports when an Warning is detected in the log.
        /// </summary>
        public HandlingBehavior handleWarning = HandlingBehavior.Ignore;

        /// <summary>
        /// Do not send Slack notifications when log messages contain this string
        /// </summary>
        public string[] ignoreMessages = new string[] { };

        private List<Regex> _ignoreMessagesRegexes;

        [InitializeOnLaunchAutopilot]
        private static void ResetInstances()
        {
            // Reset runtime instances
            var agents = Resources.FindObjectsOfTypeAll<ErrorHandlerAgent>();
            foreach (var agent in agents)
            {
                agent._ignoreMessagesRegexes = null;
            }
        }

        public override async UniTask Run(CancellationToken cancellationToken)
        {
            try
            {
                Logger.Log($"Enter {this.name}.Run()");

                OverwriteByCommandLineArguments(new Arguments());

                Application.logMessageReceivedThreaded += this.HandleLog;

                await UniTask.WaitWhile(() => true, cancellationToken: cancellationToken);
            }
            finally
            {
                Application.logMessageReceivedThreaded -= this.HandleLog;
                Logger.Log($"Exit {this.name}.Run()");
            }
        }

        /// <summary>
        /// Handle log message and post notification to Reporter (if necessary).
        /// </summary>
        /// <param name="logString">Log message string</param>
        /// <param name="stackTrace">Stack trace</param>
        /// <param name="type">Log message type</param>
        [SuppressMessage("ReSharper", "AsyncVoidMethod")] // Called by Application.logMessageReceivedThreaded
        internal async void HandleLog(string logString, string stackTrace, LogType type)
        {
            var handlingBehavior = JudgeHandlingBehavior(logString, stackTrace, type);
            if (handlingBehavior == HandlingBehavior.Ignore)
            {
                return;
            }

            // NOTE: HandleLog may called by non-main thread because it subscribe Application.logMessageReceivedThreaded
            await UniTask.SwitchToMainThread();

            Logger.Log(type, logString, stackTrace);

            var exitCode = type == LogType.Exception ? ExitCode.UnCatchExceptions : ExitCode.DetectErrorsInLog;
            if (handlingBehavior == HandlingBehavior.ReportOnly)
            {
                var settings = AutopilotState.Instance.settings;
                if (settings == null)
                {
                    throw new InvalidOperationException("Autopilot is not running");
                }

                // ReSharper disable once MethodSupportsCancellation; Do not use this Agent's CancellationToken.
                settings.Reporter.PostReportAsync(logString, stackTrace, exitCode).Forget();
            }
            else
            {
                // ReSharper disable once MethodSupportsCancellation; Do not use this Agent's CancellationToken.
                AutopilotInstance.TerminateAsync(exitCode, logString, stackTrace).Forget();
            }
        }

        private HandlingBehavior JudgeHandlingBehavior(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
            {
                return HandlingBehavior.Ignore;
            }

            if (stackTrace.Contains(nameof(ErrorHandlerAgent)) || stackTrace.Contains(nameof(SlackAPI)))
            {
                Debug.Log($"Ignore looped message: {logString}");
                return HandlingBehavior.Ignore;
            }

            if (IsIgnoreMessage(logString))
            {
                return HandlingBehavior.Ignore;
            }

            switch (type)
            {
                case LogType.Exception:
                    return this.handleException;
                case LogType.Error:
                    return this.handleError;
                case LogType.Assert:
                    return this.handleAssert;
                case LogType.Warning:
                    return this.handleWarning;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private bool IsIgnoreMessage(string logString)
        {
            if (this._ignoreMessagesRegexes == null)
            {
                this._ignoreMessagesRegexes = CreateIgnoreMessageRegexes();
            }

            return this._ignoreMessagesRegexes.Exists(regex => regex.IsMatch(logString));
        }

        private List<Regex> CreateIgnoreMessageRegexes()
        {
            var regexs = new List<Regex>();
            foreach (var message in this.ignoreMessages)
            {
                Regex pattern;
                try
                {
                    pattern = new Regex(message);
                }
                catch (ArgumentException e)
                {
                    Debug.LogException(e);
                    continue;
                }

                regexs.Add(pattern);
            }

            return regexs;
        }

        internal void OverwriteByCommandLineArguments(Arguments args)
        {
            if (args.HandleException.IsCaptured())
            {
                this.handleException = HandlingBehaviorFrom(args.HandleException.Value());
            }

            if (args.HandleError.IsCaptured())
            {
                this.handleError = HandlingBehaviorFrom(args.HandleError.Value());
            }

            if (args.HandleAssert.IsCaptured())
            {
                this.handleAssert = HandlingBehaviorFrom(args.HandleAssert.Value());
            }

            if (args.HandleWarning.IsCaptured())
            {
                this.handleWarning = HandlingBehaviorFrom(args.HandleWarning.Value());
            }
        }

        internal static HandlingBehavior HandlingBehaviorFrom(bool value)
        {
            return value ? HandlingBehavior.TerminateAutopilot : HandlingBehavior.Ignore;
        }
    }
}
