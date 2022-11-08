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
[AppliesTo(ProjectCapability.WPF)]
internal class WPFValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string StartupURIPropertyName = "StartupURI";
    internal const string ShutdownModePropertyName = "ShutdownMode_WPF";
    internal const string OutputTypePropertyName = "OutputType";
    internal const string WinExeOutputTypeValue = "WinExe";

    private readonly IApplicationXamlFileAccessor _applicationXamlFileAccessor;
    private readonly UnconfiguredProject _project;

    [ImportingConstructor]
    public WPFValueProvider(IApplicationXamlFileAccessor applicationXamlFileAccessor, UnconfiguredProject project)
    {
        _applicationXamlFileAccessor = applicationXamlFileAccessor;
        _project = project;
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName, defaultProperties);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName, defaultProperties);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        if (await IsWpfAndNotWinFormsApplicationAsync(defaultProperties))
        {
            await (propertyName switch
            {
                StartupURIPropertyName => _applicationXamlFileAccessor.SetStartupUriAsync(unevaluatedPropertyValue),
                ShutdownModePropertyName => _applicationXamlFileAccessor.SetShutdownModeAsync(unevaluatedPropertyValue),

                _ => throw new InvalidOperationException($"The {nameof(WPFValueProvider)} does not support the '{propertyName}' property.")
            });
        }

        return null;
    }

    private async Task<string> GetPropertyValueAsync(string propertyName, IProjectProperties defaultProperties)
    {
        if (await IsWpfAndNotWinFormsApplicationAsync(defaultProperties))
        {
            return propertyName switch
            {
                StartupURIPropertyName => await _applicationXamlFileAccessor.GetStartupUriAsync() ?? string.Empty,
                ShutdownModePropertyName => await _applicationXamlFileAccessor.GetShutdownModeAsync() ?? "OnLastWindowClose",

                _ => throw new InvalidOperationException($"The {nameof(WPFValueProvider)} does not support the '{propertyName}' property.")
            };
        }

        return string.Empty;
    }

    /// <summary>
    /// This method will help us determine if we need to load the files
    /// where the properties are stored. For WPF, that is the Application.xaml file;
    /// for WinForms, that is the .myApp file.
    /// </summary>
    private async Task<bool> IsWpfAndNotWinFormsApplicationAsync(IProjectProperties defaultProperties)
    {
        IProjectCapabilitiesScope capabilities = _project.Capabilities;

        bool useWPF = capabilities.Contains(ProjectCapability.WPF);
        bool useWindowsForms = capabilities.Contains(ProjectCapability.WindowsForms);
        string outputTypeString = await defaultProperties.GetEvaluatedPropertyValueAsync(OutputTypePropertyName);

        return useWPF
            && StringComparers.PropertyLiteralValues.Equals(outputTypeString, WinExeOutputTypeValue)
            && !useWindowsForms;
    }
}
