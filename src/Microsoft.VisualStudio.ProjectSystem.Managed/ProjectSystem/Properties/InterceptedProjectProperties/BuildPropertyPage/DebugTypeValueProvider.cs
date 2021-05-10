// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("DebugType", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class DebugTypeValueProvider : InterceptingPropertyValueProviderBase
    {
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = await base.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedPropertyValue, defaultProperties);

            return value == "pdbonly" ? "full" : value;
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = await base.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties);

            return value == "pdbonly" ? "full" : value;
        }
    }
}
