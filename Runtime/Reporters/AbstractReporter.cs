// Copyright (c) 2023-2024 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DeNA.Anjin.Reporters
{
    /// <summary>
    /// Reporter base class
    /// </summary>
    public abstract class AbstractReporter : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>
        /// Description about this agent instance.
        /// </summary>
        [Multiline] public string description;
#endif

        /// <summary>
        /// Post report log message, stacktrace and screenshot
        /// </summary>
        /// <param name="logString">Log message</param>
        /// <param name="stackTrace">Stack trace</param>
        /// <param name="type">Log message type</param>
        /// <param name="withScreenshot">With screenshot</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public abstract UniTask PostReportAsync(
            string logString,
            string stackTrace,
            LogType type,
            bool withScreenshot,
            CancellationToken cancellationToken = default
        );
    }
}
