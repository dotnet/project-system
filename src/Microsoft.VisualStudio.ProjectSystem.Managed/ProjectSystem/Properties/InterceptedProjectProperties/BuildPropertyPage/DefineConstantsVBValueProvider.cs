// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(DefineConstantsPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.VisualBasic)]
internal sealed class DefineConstantsVBValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string DefineConstantsPropertyName = "DefineConstants";

    private readonly NameQuotedValuePairListEncoding _encoding = new();

    public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        return Task.FromResult<string?>(_encoding.Format(_encoding.Parse(unevaluatedPropertyValue)));
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {

        return Task.FromResult<string>(_encoding.DisplayFormat(_encoding.Decode(evaluatedPropertyValue)));
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult<string>(_encoding.DisplayFormat(_encoding.Decode(unevaluatedPropertyValue)));
    }
}

