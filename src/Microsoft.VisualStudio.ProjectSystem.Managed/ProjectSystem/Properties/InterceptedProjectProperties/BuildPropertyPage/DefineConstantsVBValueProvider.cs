using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(DefineConstantsPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
[AppliesTo(ProjectCapability.VisualBasic)]
internal sealed class DefineConstantsVBValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string DefineConstantsPropertyName = "DefineConstants";

    public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        return Task.FromResult<string?>(NameQuotedValuePairListEncoding.Instance.Format(NameQuotedValuePairListEncoding.Instance.Parse(unevaluatedPropertyValue)));
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult<string?>(NameQuotedValuePairListEncoding.Instance.Format(evaluatedPropertyValue));
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult<string?>(NameQuotedValuePairListEncoding.Instance.Format(unevaluatedPropertyValue));
    }
}

