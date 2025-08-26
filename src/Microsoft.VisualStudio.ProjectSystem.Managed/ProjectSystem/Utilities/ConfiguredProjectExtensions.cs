// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.TestHooks;

namespace Microsoft.VisualStudio.ProjectSystem;

internal static class ConfiguredProjectExtensions
{
    public static string GetProjectName(this ConfiguredProject project)
        => GetProjectName(project.UnconfiguredProject);

    public static string GetProjectName(this UnconfiguredProject project)
        => Path.GetFileNameWithoutExtension(project.FullPath);

    public static async ValueTask<string> GetProjectPropertyValueAsync(this ConfiguredProject configuredProject, string propertyName)
    {
        var provider = configuredProject.Services.ProjectPropertiesProvider;
        Assumes.Present(provider);
        return await provider.GetCommonProperties().GetEvaluatedPropertyValueAsync(propertyName);
    }

    public static async ValueTask<bool> GetProjectPropertyBoolAsync(this ConfiguredProject configuredProject, string propertyName, bool defaultValue = false)
    {
        var value = await configuredProject.GetProjectPropertyValueAsync(propertyName);
        return value is "" ? defaultValue : StringComparers.PropertyLiteralValues.Equals(value, "true");
    }

    public static T GetExportedService<T>(this ConfiguredProject configuredProject)
        => configuredProject.Services is IExportProviderTestHook testExportProvider
            ? testExportProvider.GetExportedValue<T>()
            : configuredProject.Services.ExportProvider.GetExportedValue<T>();
}
