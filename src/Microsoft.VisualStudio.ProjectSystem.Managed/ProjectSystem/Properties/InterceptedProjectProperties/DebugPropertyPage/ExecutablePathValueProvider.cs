// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Intercepts attempts to read or write the executable path and redirects them to
    /// the <see cref="ILaunchSettingsProvider"/> to update the active launch profile,
    /// if any.
    /// </summary>
    [ExportInterceptingPropertyValueProvider(ExecutablePathPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ExecutablePathValueProvider : ActiveLaunchProfileValueProviderBase
    {
        private const string ExecutablePathPropertyName = "ExecutablePath";


        [ImportingConstructor]
        public ExecutablePathValueProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
            :base(project, launchSettingsProvider, projectThreadingService)
        {
        }

        protected override string GetValueFromLaunchSettings(ILaunchProfile? activeLaunchProfile)
        {
            return activeLaunchProfile?.ExecutablePath ?? string.Empty;
        }

        protected override void UpdateActiveLaunchProfile(IWritableLaunchProfile activeLaunchProfile, string newValue)
        {
            activeLaunchProfile.ExecutablePath = newValue;
        }
    }
}
