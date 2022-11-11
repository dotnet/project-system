// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

/// <summary>
/// Supports the setting of constants in the property page, corresponding to the "DefineConstants" property
/// </summary>
[ExportInterceptingPropertyValueProvider(DefineConstantsPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.VisualBasic)]
internal sealed class DefineConstantsVBValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string DefineConstantsPropertyName = "DefineConstants";

    public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        Dictionary<string, string> constantsDictionary = StringListEncoding.ParseIntoDictionary(unevaluatedPropertyValue);
        return Task.FromResult<string?>(KeyQuotedValuePairListEncoding.Instance.Format(StringListEncoding.EnumerateDictionary(constantsDictionary)));
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult<string>(OnGetPropertyValue(evaluatedPropertyValue));
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult<string>(OnGetPropertyValue(unevaluatedPropertyValue));
    }

    private static string OnGetPropertyValue(string propertyValue)
    {
        Dictionary<string, string> constantsDictionary = StringListEncoding.ParseIntoDictionary(propertyValue);
        return KeyValuePairListEncoding.Instance.Format(StringListEncoding.EnumerateDictionary(constantsDictionary));
    }
}
