// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Intercepts attempts to read or write the name of the active launch profile and
    /// redirects them to an <see cref="ILaunchSettingsProvider" />. Setting the name
    /// makes the related <see cref="ILaunchProfile" /> the active one.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="ActiveLaunchProfileCommonValueProvider"/>
    /// which reads and writes values within the active launch profile.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(ActiveLaunchProfilePropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ActiveLaunchProfileNameValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string ActiveLaunchProfilePropertyName = "ActiveLaunchProfile";

        private readonly ILaunchSettingsProvider _launchSettings;

        [ImportingConstructor]
        public ActiveLaunchProfileNameValueProvider(ILaunchSettingsProvider launchSettings)
        {
            _launchSettings = launchSettings;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
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
            // Infinite timeout means this will not actually be null.
            ILaunchSettings? launchSettings = await _launchSettings.WaitForFirstSnapshot(Timeout.Infinite);
            Assumes.NotNull(launchSettings);

            return launchSettings.ActiveProfile?.Name ?? string.Empty;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            await _launchSettings.SetActiveProfileAsync(unevaluatedPropertyValue);

            // We've intercepted the "set" operation and redirected it to the launch settings.
            // Return "null" to indicate that the value should _not_ be set in the project file
            // as well.
            return null;
        }
    }
}
