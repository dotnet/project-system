// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Common base type for <see cref="IInterceptingPropertyValueProvider"/>s that read
    /// and write data to the active <see cref="ILaunchProfile"/> via the <see cref="ILaunchSettingsProvider"/>.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="ActiveLaunchProfileNameValueProvider" />,
    /// which reads and writes the _name_ of the active launch profile but does not
    /// access its members.
    /// </remarks>
    internal abstract class ActiveLaunchProfileValueProviderBase : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly IProjectThreadingService _projectThreadingService;

        internal ActiveLaunchProfileValueProviderBase(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _projectThreadingService = projectThreadingService;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        private async Task<string> GetPropertyValueAsync()
        {
            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

            return GetValueFromLaunchSettings(launchSettings.ActiveProfile);
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
                    UpdateActiveLaunchProfile(activeProfile, unevaluatedPropertyValue);

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

        /// <summary>
        /// Returns the relevant value from the <paramref name="activeLaunchProfile"/>,
        /// or an appropriate alternative if there is no active launch profile.
        /// </summary>
        /// <param name="activeLaunchProfile">The active launch profile, if any.</param>
        /// <returns>The value from the <paramref name="activeLaunchProfile"/>, or an
        /// alternative value if there is no active launch profile.</returns>
        protected abstract string GetValueFromLaunchSettings(ILaunchProfile? activeLaunchProfile);

        /// <summary>
        /// Updates the <paramref name="activeLaunchProfile"/> with a new value.
        /// </summary>
        /// <param name="activeLaunchProfile">The <see cref="ILaunchProfile"/> to update.</param>
        /// <param name="newValue">The new value property value.</param>
        protected abstract void UpdateActiveLaunchProfile(IWritableLaunchProfile activeLaunchProfile, string newValue);
    }
}
