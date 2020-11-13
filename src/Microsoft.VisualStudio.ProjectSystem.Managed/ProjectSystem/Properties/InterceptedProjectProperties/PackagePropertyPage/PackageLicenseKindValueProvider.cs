// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("PackageLicenseKind", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageLicenseKindValueProvider : InterceptingPropertyValueProviderBase
    {
        public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            return Task.FromResult<string?>(null);
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return ComputeValueAsync(defaultProperties.GetEvaluatedPropertyValueAsync!);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return ComputeValueAsync(defaultProperties.GetUnevaluatedPropertyValueAsync);
        }

        private static async Task<string> ComputeValueAsync(Func<string, Task<string?>> getValue)
        {
            if (!string.IsNullOrEmpty(await getValue("PackageLicenseExpression")))
            {
                return "License";
            }

            if (!string.IsNullOrEmpty(await getValue("PackageLicenseFile")))
            {
                return "File";
            }
                
            return "None";
        }
    }
}
