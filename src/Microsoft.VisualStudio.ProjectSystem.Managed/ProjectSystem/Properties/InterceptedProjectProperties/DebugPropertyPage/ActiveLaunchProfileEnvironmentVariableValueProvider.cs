// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Reads and writes the <see cref="ILaunchProfile.EnvironmentVariables"/> property
    /// of the active <see cref="ILaunchProfile"/> via the <see cref="ILaunchSettingsProvider"/>.
    /// </summary>
    /// <remarks>
    /// Most of the properties of the <see cref="ILaunchProfile"/> are handled by <see cref="ActiveLaunchProfileCommonValueProvider"/>
    /// and <see cref="ActiveLaunchProfileExtensionValueProvider"/>. Handling of <see cref="ILaunchProfile.EnvironmentVariables"/>
    /// is complex enough to warrant its own value provider.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(EnvironmentVariablesPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class ActiveLaunchProfileEnvironmentVariableValueProvider : LaunchSettingsValueProviderBase
    {
        internal const string EnvironmentVariablesPropertyName = "EnvironmentVariables";

        [ImportingConstructor]
        public ActiveLaunchProfileEnvironmentVariableValueProvider(ILaunchSettingsProvider launchSettingsProvider)
            : base(launchSettingsProvider)
        {
        }

        public override string GetPropertyValue(string propertyName, ILaunchSettings launchSettings)
        {
            if (propertyName != EnvironmentVariablesPropertyName)
            {
                throw new InvalidOperationException($"{nameof(ActiveLaunchProfileEnvironmentVariableValueProvider)} does not handle property '{propertyName}'.");
            }

            return LaunchProfileEnvironmentVariableEncoding.Format(launchSettings.ActiveProfile);
        }

        public override bool SetPropertyValue(string propertyName, string value, IWritableLaunchSettings launchSettings)
        {
            if (propertyName != EnvironmentVariablesPropertyName)
            {
                throw new InvalidOperationException($"{nameof(ActiveLaunchProfileEnvironmentVariableValueProvider)} does not handle property '{propertyName}'.");
            }

            var activeProfile = launchSettings.ActiveProfile;
            if (activeProfile == null)
            {
                return false;
            }

            LaunchProfileEnvironmentVariableEncoding.ParseIntoDictionary(value, activeProfile.EnvironmentVariables);

            return true;
        }
    }
}
