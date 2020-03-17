// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Intercepts attempts to read or write the launch target and redirects them to the
    /// <see cref="ILaunchSettingsProvider" /> to update the active launch profile, if
    /// any.
    /// </summary>
    [ExportInterceptingPropertyValueProvider(LaunchTargetPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class LaunchTargetValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string LaunchTargetPropertyName = "LaunchTarget";

        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly IProjectThreadingService _projectThreadingService;

        [ImportingConstructor]
        public LaunchTargetValueProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _projectThreadingService = projectThreadingService;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        /// <summary>
        /// Handles the core logic of retrieving the property.
        /// </summary>
        /// <remarks>
        /// Since we're redirecting the properties through the <see cref="ILaunchSettingsProvider"/>
        /// there's no distinction between "evaluated" and "unevaluated" properties, so
        /// we handle both in the same way.
        /// </remarks>
        private async Task<string> GetPropertyValueAsync()
        {
            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

            return launchSettings.ActiveProfile?.CommandName ?? string.Empty;
        }

        public override Task<string?> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            _projectThreadingService.RunAndForget(async () =>
            {
                ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

                var writableLaunchSettings = launchSettings.ToWritableLaunchSettings();
                var activeProfile = writableLaunchSettings.ActiveProfile;
                if (activeProfile != null)
                {
                    activeProfile.CommandName = unevaluatedPropertyValue;

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
    }
}
