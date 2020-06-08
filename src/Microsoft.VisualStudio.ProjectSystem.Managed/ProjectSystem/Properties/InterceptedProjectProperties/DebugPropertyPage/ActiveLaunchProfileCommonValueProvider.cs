// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Reads and writes the common properties of the active <see cref="ILaunchProfile"/>
    /// via the <see cref="ILaunchSettingsProvider"/>.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="ActiveLaunchProfileNameValueProvider" />,
    /// which reads and writes the _name_ of the active launch profile but does not
    /// access its members.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(
        new[]
        {
            CommandLineArgumentsPropertyName,
            ExecutablePathPropertyName,
            LaunchTargetPropertyName,
            WorkingDirectoryPropertyName,
        },
        ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ActiveLaunchProfileCommonValueProvider : InterceptingPropertyValueProviderBase
    {
        internal const string CommandLineArgumentsPropertyName = "CommandLineArguments";
        internal const string ExecutablePathPropertyName = "ExecutablePath";
        internal const string LaunchTargetPropertyName = "LaunchTarget";
        internal const string WorkingDirectoryPropertyName = "WorkingDirectory";

        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly IProjectThreadingService _projectThreadingService;

        [ImportingConstructor]
        public ActiveLaunchProfileCommonValueProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _projectThreadingService = projectThreadingService;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync(propertyName);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync(propertyName);
        }

        private async Task<string> GetPropertyValueAsync(string propertyName)
        {
            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

            string? activeProfilePropertyValue = propertyName switch
            {
                CommandLineArgumentsPropertyName => launchSettings.ActiveProfile?.CommandLineArgs,
                ExecutablePathPropertyName => launchSettings.ActiveProfile?.ExecutablePath,
                LaunchTargetPropertyName => launchSettings.ActiveProfile?.CommandName,
                WorkingDirectoryPropertyName => launchSettings.ActiveProfile?.WorkingDirectory,
                _ => throw new InvalidOperationException($"{nameof(ActiveLaunchProfileCommonValueProvider)} does not handle property '{propertyName}'.")
            };

            return activeProfilePropertyValue ?? string.Empty;
        }

        public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            _projectThreadingService.RunAndForget(async () =>
            {
                ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

                var writableLaunchSettings = launchSettings.ToWritableLaunchSettings();
                var activeProfile = writableLaunchSettings.ActiveProfile;
                if (activeProfile != null)
                {
                    UpdateActiveLaunchProfile(activeProfile, propertyName, unevaluatedPropertyValue);

                    await _launchSettingsProvider.UpdateAndSaveSettingsAsync(writableLaunchSettings.ToLaunchSettings());
                }
            },
            options: ForkOptions.HideLocks,
            unconfiguredProject: _project);

            // We've intercepted the "set" operation and redirected it to the launch settings.
            // Return "null" to indicate that the value should _not_ be set in the project file
            // as well.
            return Task.FromResult<string?>(null);
        }

        private static void UpdateActiveLaunchProfile(IWritableLaunchProfile activeProfile, string propertyName, string newValue)
        {
            switch (propertyName)
            {
                case CommandLineArgumentsPropertyName:
                    activeProfile.CommandLineArgs = newValue;
                    break;

                case ExecutablePathPropertyName:
                    activeProfile.ExecutablePath = newValue;
                    break;

                case LaunchTargetPropertyName:
                    activeProfile.CommandName = newValue;
                    break;

                case WorkingDirectoryPropertyName:
                    activeProfile.WorkingDirectory = newValue;
                    break;

                default:
                    throw new InvalidOperationException($"{nameof(ActiveLaunchProfileCommonValueProvider)} does not handle property '{propertyName}'.");
            }
        }
    }
}
