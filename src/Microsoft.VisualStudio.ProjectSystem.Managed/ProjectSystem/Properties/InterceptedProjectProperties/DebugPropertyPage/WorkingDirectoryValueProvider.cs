// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Intercepts attempts to read or write the working directory paty and redirects
    /// them to the <see cref="ILaunchSettingsProvider"/> to update the active launch
    /// profile, if any.
    /// </summary>
    [ExportInterceptingPropertyValueProvider(WorkingDirectoryPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class WorkingDirectoryValueProvider : ActiveLaunchProfileValueProviderBase
    {
        private const string WorkingDirectoryPropertyName = "WorkingDirectory";


        [ImportingConstructor]
        public WorkingDirectoryValueProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
            :base(project, launchSettingsProvider, projectThreadingService)
        {
        }

        protected override string GetValueFromLaunchSettings(ILaunchProfile? activeLaunchProfile)
        {
            return activeLaunchProfile?.WorkingDirectory ?? string.Empty;
        }

        protected override void UpdateActiveLaunchProfile(IWritableLaunchProfile activeLaunchProfile, string newValue)
        {
            activeLaunchProfile.WorkingDirectory = newValue;
        }
    }
}
