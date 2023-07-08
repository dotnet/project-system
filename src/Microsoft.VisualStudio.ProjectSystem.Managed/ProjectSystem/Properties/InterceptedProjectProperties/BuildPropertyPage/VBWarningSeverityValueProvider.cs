// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(WarningSeverityPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class VBWarningSeverityValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string WarningSeverityPropertyName = "WarningSeverity";

    private const string WarningLevelPropertyName = "WarningLevel";
    private const string TreatWarningsAsErrorsPropertyName = "TreatWarningsAsErrors";

    internal const string IndividualValue = "Individual";
    internal const string DisableAllValue = "DisableAll";
    internal const string AllAsErrorsValue = "AllAsErrors";

    private static readonly string[] s_msBuildPropertyNames = { WarningSeverityPropertyName, WarningLevelPropertyName, TreatWarningsAsErrorsPropertyName };
    
    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(defaultProperties);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return OnGetPropertyValueAsync(defaultProperties);
    }

    public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
    {
        return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        switch (unevaluatedPropertyValue)
        {
            case IndividualValue:
            {
                await defaultProperties.DeletePropertyAsync(WarningLevelPropertyName, dimensionalConditions);
                await defaultProperties.DeletePropertyAsync(TreatWarningsAsErrorsPropertyName, dimensionalConditions);
                break;
            }

            case DisableAllValue:
            {
                await defaultProperties.SetPropertyValueAsync(WarningLevelPropertyName, "0", dimensionalConditions);
                await defaultProperties.DeletePropertyAsync(TreatWarningsAsErrorsPropertyName, dimensionalConditions);
                break;
            }

            case AllAsErrorsValue:
            {
                await defaultProperties.DeletePropertyAsync(WarningLevelPropertyName, dimensionalConditions);
                await defaultProperties.SetPropertyValueAsync(TreatWarningsAsErrorsPropertyName, "true", dimensionalConditions);
                break;
            }

            default:
                break;
        }

        return null;
    }

    private static async Task<string> OnGetPropertyValueAsync(IProjectProperties defaultProperties)
    {
        string warningLevelvalue = await defaultProperties.GetEvaluatedPropertyValueAsync(WarningLevelPropertyName);

        if (StringComparers.PropertyLiteralValues.Equals(warningLevelvalue, "0"))
        {
            // All warnings as disabled.
            return DisableAllValue;
        }

        string treatWarningsAsErrorsValue = await defaultProperties.GetEvaluatedPropertyValueAsync(TreatWarningsAsErrorsPropertyName);
        if (bool.TryParse(treatWarningsAsErrorsValue, out bool treatWarningsAsErrors)
            && treatWarningsAsErrors)
        {
            // All warnings are promoted to errors (except those that are disabled).
            return AllAsErrorsValue;
        }

        return IndividualValue;
    }
}
