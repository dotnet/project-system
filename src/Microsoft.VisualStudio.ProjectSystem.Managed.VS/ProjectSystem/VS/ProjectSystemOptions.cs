// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        private const string FastUpToDateEnabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckEnabled";
        private const string FastUpToDateLogLevelSettingKey = @"ManagedProjectSystem\FastUpToDateLogLevel";

        private readonly IVsOptionalService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManager;
        private readonly IEnvironmentHelper _environment;
#if !DEBUG
        private bool? _isProjectOutputPaneEnabled;
#endif

        [ImportingConstructor]
        private ProjectSystemOptions(IEnvironmentHelper environment, IVsOptionalService<SVsSettingsPersistenceManager, ISettingsManager> settingsManager)
        {
            Requires.NotNull(environment, nameof(environment));

            _environment = environment;
            _settingsManager = settingsManager;
        }

        public bool IsProjectOutputPaneEnabled
        {
            get
            {
#if DEBUG
                return true;
#else
                return IsEnabled("PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED", ref _isProjectOutputPaneEnabled);
#endif
            }
        }

        public async Task<bool> GetIsFastUpToDateCheckEnabledAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return _settingsManager.Value?.GetValueOrDefault(FastUpToDateEnabledSettingKey, true) ?? true;
        }

        public async Task<LogLevel> GetFastUpToDateLoggingLevelAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
