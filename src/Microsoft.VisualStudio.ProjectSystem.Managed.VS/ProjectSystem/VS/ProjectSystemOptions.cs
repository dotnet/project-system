// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        private const string FastUpToDateEnabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckEnabled";
        private const string FastUpToDateLogLevelSettingKey = @"ManagedProjectSystem\FastUpToDateLogLevel";

        private readonly IVsUIService<ISettingsManager> _settingsManager;
        private readonly IEnvironmentHelper _environment;
        private bool? _isProjectOutputPaneEnabled;

        [ImportingConstructor]
        public ProjectSystemOptions(IEnvironmentHelper environment, IVsUIService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager)
        {
            _environment = environment;
            _settingsManager = settingsManager;
        }

        public bool IsProjectOutputPaneEnabled
        {
            get
            {
                return IsEnabled("PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED", ref _isProjectOutputPaneEnabled);
            }
        }

        public async Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return _settingsManager.Value?.GetValueOrDefault(FastUpToDateEnabledSettingKey, true) ?? true;
        }

        public async Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return _settingsManager.Value?.GetValueOrDefault(FastUpToDateLogLevelSettingKey, LogLevel.None) ?? LogLevel.None;
        }

        private bool IsEnabled(string variable, ref bool? result)
        {
            if (result == null)
            {
                string value = _environment.GetEnvironmentVariable(variable);

                result = string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
            }

            return result.Value;
        }
    }
}
