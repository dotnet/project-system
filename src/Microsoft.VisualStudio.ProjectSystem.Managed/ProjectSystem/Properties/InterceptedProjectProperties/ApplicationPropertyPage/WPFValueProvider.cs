// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.WPF;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(
    new[]
    {
        StartupURIPropertyName,
        ShutdownModePropertyName
    },
    ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal class WPFValueProvider : InterceptingPropertyValueProviderBase
{
    private const string StartupURIPropertyName = "StartupURI";
    private const string ShutdownModePropertyName = "ShutdownMode_WPF";

    private readonly IApplicationXamlFileAccessor _applicationXamlFileAccessor;

    [ImportingConstructor]
    public WPFValueProvider(IApplicationXamlFileAccessor applicationXamlFileAccessor)
    {
        _applicationXamlFileAccessor = applicationXamlFileAccessor;
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        await (propertyName switch
        {
            StartupURIPropertyName => _applicationXamlFileAccessor.SetStartupUriAsync(unevaluatedPropertyValue),
            ShutdownModePropertyName => _applicationXamlFileAccessor.SetShutdownModeAsync(unevaluatedPropertyValue),

            _ => throw new InvalidOperationException($"The {nameof(WPFValueProvider)} does not support the '{propertyName}' property.")
        });

        return null;
    }

    private async Task<string> GetPropertyValueAsync(string propertyName)
    {
        return propertyName switch
        {
            StartupURIPropertyName => await _applicationXamlFileAccessor.GetStartupUriAsync() ?? string.Empty,
            ShutdownModePropertyName => await _applicationXamlFileAccessor.GetShutdownModeAsync() ?? "OnLastWindowClose",

            _ => throw new InvalidOperationException($"The {nameof(WPFValueProvider)} does not support the '{propertyName}' property.")
        };
    }
}
