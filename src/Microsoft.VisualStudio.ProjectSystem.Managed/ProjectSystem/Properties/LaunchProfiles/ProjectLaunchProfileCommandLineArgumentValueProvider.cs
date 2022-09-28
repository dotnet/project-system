// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.LaunchProfiles;

[ExportLaunchProfileExtensionValueProvider(
    CommandLineArgumentsPropertyName,
    ExportLaunchProfileExtensionValueProviderScope.LaunchProfile)]
internal class ProjectLaunchProfileCommandLineArgumentValueProvider : ILaunchProfileExtensionValueProvider
{
    internal const string CommandLineArgumentsPropertyName = "CommandLineArguments";

    public string OnGetPropertyValue(string propertyName, ILaunchProfile launchProfile, ImmutableDictionary<string, object> globalSettings, Rule? rule)
    {
        return launchProfile.CommandLineArgs ?? string.Empty;
    }

    public void OnSetPropertyValue(string propertyName, string propertyValue, IWritableLaunchProfile launchProfile, ImmutableDictionary<string, object> globalSettings, Rule? rule)
    {
        launchProfile.CommandLineArgs = propertyValue;
    }
}
