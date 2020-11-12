// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetMultipleFrameworks", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetMultipleFrameworksValueProvider : InterceptingPropertyValueProviderBase
    {
        public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            return Task.FromResult<string?>(null);
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(
                evaluatedPropertyValue.Length == 0
                ? "false"
                : "true");
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(
                unevaluatedPropertyValue.Length == 0
                ? "false"
                : "true");
        }
    }
}
