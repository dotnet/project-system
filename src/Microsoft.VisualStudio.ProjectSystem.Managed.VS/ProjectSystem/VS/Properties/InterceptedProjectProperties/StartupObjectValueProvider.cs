// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

// The AppliesTo metadata has no effect given the limitations described in https://github.com/dotnet/project-system/issues/8170.
[ExportInterceptingPropertyValueProvider(StartupObjectProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.VisualBasic)]
internal class StartupObjectValueProvider : InterceptingPropertyValueProviderBase
{
    private readonly IMyAppFileAccessor _myAppXmlFileAccessor;

    internal const string ApplicationFrameworkProperty = "MyType";
    private const string EnabledValue = "WindowsForms";
    private const string DisabledValue = "WindowsFormsWithCustomSubMain";

    internal const string UseWinFormsProperty = "UseWindowsForms";
    internal const string StartupObjectProperty = "StartupObject";
    internal const string RootNamespaceProperty = "RootNamespace";

    private readonly UnconfiguredProject _project;

    [ImportingConstructor]
    public StartupObjectValueProvider(IMyAppFileAccessor myAppXmlFileAccessor, UnconfiguredProject project)
    {
        _myAppXmlFileAccessor = myAppXmlFileAccessor;
        _project = project;
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        IProjectCapabilitiesScope capabilities = _project.Capabilities;
        bool isWindowsForms = capabilities.Contains(ProjectCapability.WindowsForms);
        string rootNameSpace = await defaultProperties.GetEvaluatedPropertyValueAsync(RootNamespaceProperty);

        if (isWindowsForms)
        {
            string applicationFrameworkValue = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkProperty);

            if (string.Compare(applicationFrameworkValue, EnabledValue) == 0)
            {
                // Set the startup object in the myapp file.
                if (unevaluatedPropertyValue.StartsWith(rootNameSpace + ".", StringComparison.OrdinalIgnoreCase))
                {
                    unevaluatedPropertyValue = unevaluatedPropertyValue.Substring(rootNameSpace.Length + 1);
                }
                await _myAppXmlFileAccessor.SetMainFormAsync(unevaluatedPropertyValue);

                // And save namespace.My.MyApplication in the project file.
                await defaultProperties.SetPropertyValueAsync(StartupObjectProperty, rootNameSpace + ".My.MyApplication");
            }
        }

        // Else, if it's other than a Windows Forms project, save the StartupObject property in the project file as usual.
        // Or if the ApplicationFramework property is disabled, save the StartupObject property in the project file as usual.
        await defaultProperties.SetPropertyValueAsync(StartupObjectProperty, rootNameSpace + "." + unevaluatedPropertyValue);
        return null;
    }

    public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        // StartupObject can come from the project file or the myapp file.
        string applicationFrameworkValue = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkProperty);

        if (string.Compare(applicationFrameworkValue, DisabledValue) == 0)
            return evaluatedPropertyValue;

        if (string.IsNullOrEmpty(evaluatedPropertyValue))
            return await _myAppXmlFileAccessor.GetMainFormAsync() ?? string.Empty;

        return evaluatedPropertyValue;
    }

    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        // StartupObject can come from the project file or the myapp file.
        string? applicationFrameworkValue = await defaultProperties.GetUnevaluatedPropertyValueAsync(ApplicationFrameworkProperty);
                
        if (string.Compare(applicationFrameworkValue, DisabledValue) == 0)
            return unevaluatedPropertyValue;

        if (string.IsNullOrEmpty(unevaluatedPropertyValue))
            return await _myAppXmlFileAccessor.GetMainFormAsync() ?? string.Empty;

        return unevaluatedPropertyValue;
    }
}
