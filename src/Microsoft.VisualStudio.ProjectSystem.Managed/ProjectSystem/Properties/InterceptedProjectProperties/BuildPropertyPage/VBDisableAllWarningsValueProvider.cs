// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(DisableAllWarningsPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class VBDisableAllWarningsValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string DisableAllWarningsPropertyName = "DisableAllWarnings";

    internal const string WarningLevelPropertyName = "WarningLevel";

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(defaultProperties);
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(defaultProperties);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, "true"))
        {
            await defaultProperties.SetPropertyValueAsync(WarningLevelPropertyName, "0", dimensionalConditions);
        }
        else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, "false"))
        {
            await defaultProperties.SetPropertyValueAsync(WarningLevelPropertyName, "1", dimensionalConditions);
        }

        return null;
    }

    private async Task<string> OnGetPropertyValueAsync(IProjectProperties projectProperties)
    {
        string warningLevelValue = await projectProperties.GetEvaluatedPropertyValueAsync(WarningLevelPropertyName);

        return warningLevelValue == "0" ? "true" : "false";
    }
}
