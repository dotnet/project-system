using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("LocalDebuggerCommand", ExportInterceptingPropertyValueProviderFile.UserFile)]
    class LocalDebuggerCommandValueProvider : InterceptingPropertyValueProviderBase
    {
        public override Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return base.OnGetEvaluatedPropertyValueAsync(evaluatedPropertyValue, defaultProperties);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return base.OnGetUnevaluatedPropertyValueAsync(unevaluatedPropertyValue, defaultProperties);
        }

        public override Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            return base.OnSetPropertyValueAsync(unevaluatedPropertyValue, defaultProperties, dimensionalConditions);
        }
    }
}
