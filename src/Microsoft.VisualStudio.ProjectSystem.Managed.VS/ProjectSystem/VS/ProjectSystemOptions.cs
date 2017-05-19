// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectSystemOptions))]
    internal class ProjectSystemOptions : IProjectSystemOptions
    {
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        private const string FastUpToDateEnabledSettingKey = "NETCoreProjectSystem\\FastUpToDateCheckEnabled";
        private const string OutputPaneEnabledSettingKey = "NETCoreProjectSystem\\OutputPaneEnabled";

        private readonly ISettingsManager _settingsManager;
        private readonly IEnvironmentHelper _environment;

        [ImportingConstructor]
        public ProjectSystemOptions(IEnvironmentHelper environment, [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
        {
            Requires.NotNull(environment, nameof(environment));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            _environment = environment;
            _settingsManager = (ISettingsManager)serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
        }

        public bool IsProjectOutputPaneEnabled
        {
            get
            {
#if DEBUG
                return true;
#else
                return _settingsManager?.GetValueOrDefault(FastUpToDateEnabledSettingKey, false) ?? false 
                        || IsEnabled("PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED");
#endif
            }
        }

        public bool IsFastUpToDateCheckEnabled
        {
            get
            {
#if DEBUG
                return true;
#else
                return _settingsManager?.GetValueOrDefault(FastUpToDateEnabledSettingKey, false) ?? false;
#endif
            }
        }

        private bool IsEnabled(string variable)
        {
            string value = _environment.GetEnvironmentVariable(variable);

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
