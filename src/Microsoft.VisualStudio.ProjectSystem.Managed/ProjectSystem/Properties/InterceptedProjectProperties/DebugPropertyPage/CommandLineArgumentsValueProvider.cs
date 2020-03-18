// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Intercepts attempts to read or write the command line arguments and redirects
    /// them to the <see cref="ILaunchSettingsProvider"/> to update the active launch
    /// profile, if any.
    /// </summary>
    [ExportInterceptingPropertyValueProvider(CommandLineArgumentsPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class CommandLineArgumentsValueProvider : ActiveLaunchProfileValueProviderBase
    {
        private const string CommandLineArgumentsPropertyName = "CommandLineArguments";


        [ImportingConstructor]
        public CommandLineArgumentsValueProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
            :base(project, launchSettingsProvider, projectThreadingService)
        {
        }

        protected override string GetValueFromLaunchSettings(ILaunchProfile? activeLaunchProfile)
        {
            return activeLaunchProfile?.CommandLineArgs ?? string.Empty;
        }

        protected override void UpdateActiveLaunchProfile(IWritableLaunchProfile activeLaunchProfile, string newValue)
        {
            activeLaunchProfile.CommandLineArgs = newValue;
        }
    }
}
