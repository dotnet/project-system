// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    [Export(typeof(IProjectSystemOptionsWithChanges))]
    internal class ProjectSystemOptions : IProjectSystemOptionsWithChanges
    {
        private const string FastUpToDateEnabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckEnabled";
        private const string FastUpToDateLogLevelSettingKey = @"ManagedProjectSystem\FastUpToDateLogLevel";
        private const string UseDesignerByDefaultSettingKey = @"ManagedProjectSystem\UseDesignerByDefault";

        // This setting exists as an option in Roslyn repo: 'FeatureOnOffOptions.SkipAnalyzersForImplicitlyTriggeredBuilds'.
        // Do not change this setting key unless the Roslyn option name is changed.
        internal const string SkipAnalyzersForImplicitlyTriggeredBuildSettingKey = "TextEditor.SkipAnalyzersForImplicitlyTriggeredBuilds";

        private readonly IVsUIService<ISettingsManager> _settingsManager;
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportingConstructor]
        public ProjectSystemOptions(IVsUIService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager, JoinableTaskContext joinableTaskContext)
        {
            _settingsManager = settingsManager;
            _joinableTaskContext = joinableTaskContext;
        }

        public Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(FastUpToDateEnabledSettingKey, true, cancellationToken);
        }

        public Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(FastUpToDateLogLevelSettingKey, LogLevel.None, cancellationToken);
        }

        public Task<bool> GetUseDesignerByDefaultAsync(string designerCategory, bool defaultValue, CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, defaultValue, cancellationToken);
        }

        public Task SetUseDesignerByDefaultAsync(string designerCategory, bool value, CancellationToken cancellationToken = default)
        {
            return SetSettingValueAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, value, cancellationToken);
        }

        public Task<bool> GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefaultAsync(SkipAnalyzersForImplicitlyTriggeredBuildSettingKey, defaultValue: true, cancellationToken);
        }

        private async Task<T> GetSettingValueOrDefaultAsync<T>(string name, T defaultValue, CancellationToken cancellationToken)
        {
            await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

            return _settingsManager.Value.GetValueOrDefault(name, defaultValue);
        }

        private async Task SetSettingValueAsync(string name, object value, CancellationToken cancellationToken)
        {
            await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

            await _settingsManager.Value.SetValueAsync(name, value, isMachineLocal: false);
        }

        public void RegisterOptionChangedEventHandler(PropertyChangedAsyncEventHandler handler)
        {
            ISettingsSubset? settingsSubset = _settingsManager.Value.GetSubset("*");
            if (settingsSubset != null)
            {
                settingsSubset.SettingChangedAsync += handler;
            }
        }

        public void UnregisterOptionChangedEventHandler(PropertyChangedAsyncEventHandler handler)
        {
            ISettingsSubset? settingsSubset = _settingsManager.Value.GetSubset("*");
            if (settingsSubset != null)
            {
                settingsSubset.SettingChangedAsync -= handler;
            }
        }
    }
}
