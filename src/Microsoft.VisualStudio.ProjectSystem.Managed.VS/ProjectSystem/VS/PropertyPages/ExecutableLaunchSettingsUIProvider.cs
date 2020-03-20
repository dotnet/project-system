// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Windows.Controls;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implementation of ILaunchSettingsUIProvider for the Executable launch type.
    /// </summary>
    [Export(typeof(ILaunchSettingsUIProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [Order(Order.Lowest)] // Lowest priority to allow this to be overridden
    internal class ExecutableLaunchSettingsUIProvider : ILaunchSettingsUIProvider
    {
        [ImportingConstructor]
        public ExecutableLaunchSettingsUIProvider(UnconfiguredProject _) // force MEF scope
        {
        }

        public string CommandName => LaunchSettingsProvider.RunExecutableCommandName;

        public string FriendlyName => PropertyPageResources.ProfileKindExecutableName;

        public bool ShouldEnableProperty(string propertyName)
        {
            // Launch url is not supported
            return !string.Equals(propertyName, UIProfilePropertyName.LaunchUrl, StringComparisons.UIPropertyNames);
        }

        /// <inheritdoc />
        /// <remarks>This implementation does not provide any UI.</remarks>
        public UserControl? CustomUI => null;

        public void ProfileSelected(IWritableLaunchSettings curSettings)
        {
        }
    }
}
