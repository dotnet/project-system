// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

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

    [ImportingConstructor]
    public StartupObjectValueProvider(IMyAppFileAccessor myAppXmlFileAccessor)
    {
        _myAppXmlFileAccessor = myAppXmlFileAccessor;
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        string isWindowsFormsProject = await defaultProperties.GetEvaluatedPropertyValueAsync(UseWinFormsProperty);

        if (isWindowsFormsProject == "true")
        {
            string applicationFrameworkValue = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkProperty);

            if (applicationFrameworkValue == EnabledValue)
            {
                if (unevaluatedPropertyValue.Contains("Form")) // Is there a better way to identify a form?
                {
                    // If the user selects a Form, the value should be serialized to the myapp file.
                    await _myAppXmlFileAccessor.SetStartupObjectAsync(unevaluatedPropertyValue);
                    await defaultProperties.DeletePropertyAsync(StartupObjectProperty);
                    return null;
                }

                // If the ApplicationFramework is enabled, the value Sub Main should always be serialized to the project file.
                await defaultProperties.SetPropertyValueAsync(StartupObjectProperty, "Sub Main");
                await _myAppXmlFileAccessor.SetStartupObjectAsync(string.Empty);
                return null;
            }
        }

        // Else, if it's other than a Windows Forms project, save the StartupObject property in the project file as usual.
        // Or if the ApplicationFramework property is disabled, save the StartupObject property in the project file as usual.
        await defaultProperties.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue);
        return null;
    }

    public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        // StartupObject can come from the project file or the myapp file.
        string applicationFrameworkValue = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkProperty);
        
        if (applicationFrameworkValue == DisabledValue)
            return await base.OnGetUnevaluatedPropertyValueAsync(propertyName, evaluatedPropertyValue, defaultProperties);

        string valueInProjectFile = await base.OnGetUnevaluatedPropertyValueAsync(propertyName, evaluatedPropertyValue, defaultProperties);

        if (string.IsNullOrEmpty(valueInProjectFile))
            return await _myAppXmlFileAccessor.GetStartupObjectAsync() ?? string.Empty;

        return valueInProjectFile;
    }

    public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        // StartupObject can come from the project file or the myapp file.
        string applicationFrameworkValue = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkProperty);

        if (applicationFrameworkValue == DisabledValue)
            return await base.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties);

        string valueInProjectFile = await base.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties);

        if (string.IsNullOrEmpty(valueInProjectFile))
            return await _myAppXmlFileAccessor.GetStartupObjectAsync() ?? string.Empty;

        return valueInProjectFile;
    }
}
