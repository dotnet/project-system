// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        private const string FastUpToDateEnabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckEnabled";
        private const string FastUpToDateLogLevelSettingKey = @"ManagedProjectSystem\FastUpToDateLogLevel";
        private const string UseDesignerByDefaultSettingKey = @"ManagedProjectSystem\UseDesignerByDefault";
        private readonly IProjectService _projectService;
        private readonly IVsUIService<ISettingsManager> _settingsManager;
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly Lazy<bool> _isProjectOutputPaneEnabled;

        [ImportingConstructor]
        public ProjectSystemOptions(IProjectService projectService, IVsUIService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager, JoinableTaskContext joinableTaskContext)
        {
            _projectService = projectService;
            _settingsManager = settingsManager;
            _joinableTaskContext = joinableTaskContext;
            _isProjectOutputPaneEnabled = new Lazy<bool>(() => _projectService.Capabilities.Contains(ServiceCapability.DiagnosticRuntimeServiceCapability));
        }

        public bool IsProjectOutputPaneEnabled => _isProjectOutputPaneEnabled.Value;

        public Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefault(FastUpToDateEnabledSettingKey, true, cancellationToken);
        }

        public Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefault(FastUpToDateLogLevelSettingKey, LogLevel.None, cancellationToken);
        }

        public Task<bool> GetUseDesignerByDefaultAsync(string designerCategory, bool defaultValue, CancellationToken cancellationToken = default)
        {
            return GetSettingValueOrDefault(UseDesignerByDefaultSettingKey + "\\" + designerCategory, defaultValue, cancellationToken);
        }

        public Task SetUseDesignerByDefaultAsync(string designerCategory, bool value, CancellationToken cancellationToken = default)
        {
            return SetSettingValueAsync(UseDesignerByDefaultSettingKey + "\\" + designerCategory, value, cancellationToken);
        }

        private async Task<T> GetSettingValueOrDefault<T>(string name, T defaultValue, CancellationToken cancellationToken)
        {
            await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

            return _settingsManager.Value.GetValueOrDefault(name, defaultValue);
        }

        private async Task SetSettingValueAsync(string name, object value, CancellationToken cancellationToken)
        {
            await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

            await _settingsManager.Value.SetValueAsync(name, value, isMachineLocal: false);
        }
    }
}
