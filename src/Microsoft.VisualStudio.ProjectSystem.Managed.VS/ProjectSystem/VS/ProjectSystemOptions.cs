// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        private const string FastUpToDateDisabledSettingKey = @"ManagedProjectSystem\FastUpToDateCheckDisabled";
        private const string VerboseFastUpToDateLoggingSettingKey = @"ManagedProjectSystem\VerboseFastUpToDateLogging";

        private readonly IServiceProvider _serviceProvider;
        private readonly IEnvironmentHelper _environment;

        [ImportingConstructor]
        public ProjectSystemOptions(IEnvironmentHelper environment, [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
        {
            Requires.NotNull(environment, nameof(environment));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            _environment = environment;
            _serviceProvider = serviceProvider;
        }

        private ISettingsManager SettingsManager => _serviceProvider.GetService<ISettingsManager, SVsSettingsPersistenceManager>();

        public bool IsProjectOutputPaneEnabled
        {
            get
            {
#if DEBUG
                return true;
#else
                return IsEnabled("PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED");
#endif
            }
        }

        public async Task<bool> GetIsFastUpToDateCheckDisabledAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return SettingsManager?.GetValueOrDefault(FastUpToDateDisabledSettingKey, false) ?? false;
        }

        public async Task<bool> GetVerboseFastUpToDateLoggingAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return SettingsManager?.GetValueOrDefault(VerboseFastUpToDateLoggingSettingKey, false) ?? false;
        }

        private bool IsEnabled(string variable)
        {
            string value = _environment.GetEnvironmentVariable(variable);

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
