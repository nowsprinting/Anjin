// Copyright (c) 2023-2025 DeNA Co., Ltd.
// This software is released under the MIT License.

using System.Threading;
using Cysharp.Threading.Tasks;
using DeNA.Anjin.Annotations;
using DeNA.Anjin.Settings;
using DeNA.Anjin.Strategies;
using TestHelper.UI;
using TestHelper.UI.Strategies;
using TestHelper.UI.Operators;
using UnityEngine;
using UnityEngine.UI;

namespace DeNA.Anjin.Agents
{
    /// <summary>
    /// Wait for the <c>EmergencyExitAnnotation</c> component to appear and click attached <c>UnityEngine.UI.Button</c>.
    /// This Agent will not be terminated and will continue to be observed.
    /// </summary>
    [CreateAssetMenu(fileName = "New UGUIEmergencyExitAgent", menuName = "Anjin/uGUI Emergency Exit Agent", order = 51)]
    public class UGUIEmergencyExitAgent : AbstractAgent
    {
        /// <summary>
        /// Interval in milliseconds to check for the appearance of the <c>EmergencyExitAnnotation</c> component.
        /// </summary>
        public int intervalMillis = 1000;

        /// <summary>
        /// Take a screenshot when click the EmergencyExit button.
        /// </summary>
        public bool screenshot;

        private IReachableStrategy _reachableStrategy;
        private IClickOperator _clickOperator;
        private ScreenshotOptions _screenshotOptions;

        /// <inheritdoc />
        public override async UniTask Run(CancellationToken cancellationToken)
        {
            Logger.Log($"Enter {this.name}.Run()");

            _reachableStrategy = new DefaultReachableStrategy();
            _screenshotOptions = screenshot
                ? new ScreenshotOptions
                {
                    // ReSharper disable once PossibleNullReferenceException
                    Directory = AutopilotState.Instance.settings.ScreenshotsPath,
                    FilenameStrategy = new TwoTieredCounterStrategy(this.name),
                }
                : null;
            _clickOperator = new UguiClickOperator(logger: Logger, screenshotOptions: _screenshotOptions);

            try
            {
                var selectables = new Selectable[20];

                while (true) // Note: This agent is not terminate myself
                {
                    var allSelectableCount = Selectable.allSelectableCount;
                    if (selectables.Length < allSelectableCount)
                    {
                        selectables = new Selectable[allSelectableCount];
                    }

                    Selectable.AllSelectablesNoAlloc(selectables);
                    for (var i = 0; i < allSelectableCount; i++)
                    {
                        if (selectables[i].TryGetComponent<EmergencyExitAnnotation>(out var emergencyExit)
                            && emergencyExit.isActiveAndEnabled)
                        {
                            await ClickEmergencyExitButton(emergencyExit, cancellationToken);
                        }
                    }

                    await UniTask.Delay(intervalMillis, ignoreTimeScale: true, cancellationToken: cancellationToken);
                }
            }
            finally
            {
                Logger.Log($"Exit {this.name}.Run()");
            }
        }

        private async UniTask ClickEmergencyExitButton(EmergencyExitAnnotation emergencyExit,
            CancellationToken cancellationToken = default)
        {
            var button = emergencyExit.gameObject;
            if (!_clickOperator.CanOperate(button))
            {
                Logger.Log(LogType.Warning,
                    $"EmergencyExitAnnotation attached {button.name}({GetIdForLog(button)}) appears but cannot be operated");
                return;
            }

            if (!_reachableStrategy.IsReachable(button, out var raycastResult))
            {
                Logger.Log(LogType.Warning,
                    $"EmergencyExitAnnotation attached {button.name}({GetIdForLog(button)}) appears but not reachable");
                return;
            }

            await _clickOperator.OperateAsync(button, raycastResult, cancellationToken);
        }

        /// <summary>
        /// Returns the identifier of the GameObject for logging.
        /// </summary>
#if UNITY_6000_4_OR_NEWER
        private static EntityId GetIdForLog(GameObject gameObject) => gameObject.GetEntityId();
        // Note: Unity 6000.4 replaces InstanceID with EntityId, and Unity 6000.5 turns the obsolete InstanceID APIs into compile errors.
#else
        private static int GetIdForLog(GameObject gameObject) => gameObject.GetInstanceID();
#endif
    }
}
