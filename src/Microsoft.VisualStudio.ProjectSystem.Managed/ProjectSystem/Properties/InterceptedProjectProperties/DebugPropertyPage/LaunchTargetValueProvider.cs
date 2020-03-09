// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private readonly ILaunchSettingsProvider _launchSettingsProvider;

        [ImportingConstructor]
        public LaunchTargetValueProvider(ILaunchSettingsProvider launchSettingsProvider)
        {
            _launchSettingsProvider = launchSettingsProvider;
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

        public override async Task<string?> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

            var writableLaunchSettings = launchSettings.ToWritableLaunchSettings();
            var activeProfile = writableLaunchSettings.ActiveProfile;
            if (activeProfile != null)
            {
                activeProfile.CommandName = unevaluatedPropertyValue;

                await _launchSettingsProvider.UpdateAndSaveSettingsAsync(writableLaunchSettings.ToLaunchSettings());
            }

            // We've intercepted the "set" operation and redirected it to the launch settings.
            // Return "null" to indicate that the value should _not_ be set in the project file
            // as well.
            return null;
        }
    }
}
