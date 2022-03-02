// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("DebugType", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class DebugTypeValueProvider : InterceptingPropertyValueProviderBase
    {
        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = evaluatedPropertyValue == "pdbonly"
                ? "full"
                : evaluatedPropertyValue;

            return Task.FromResult(value);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = unevaluatedPropertyValue == "pdbonly"
                ? "full"
                : unevaluatedPropertyValue;

            return Task.FromResult(value);
        }
    }
}
