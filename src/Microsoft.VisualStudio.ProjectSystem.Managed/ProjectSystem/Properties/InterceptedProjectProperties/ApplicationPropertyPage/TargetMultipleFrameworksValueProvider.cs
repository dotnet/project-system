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

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string targetFrameworks = await defaultProperties.GetEvaluatedPropertyValueAsync("TargetFrameworks");
            string targetFramework = await defaultProperties.GetEvaluatedPropertyValueAsync("TargetFramework");

            return ComputeValue(targetFrameworks, targetFramework);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string? targetFrameworks = await defaultProperties.GetUnevaluatedPropertyValueAsync("TargetFrameworks");
            string? targetFramework = await defaultProperties.GetUnevaluatedPropertyValueAsync("TargetFramework");

            return ComputeValue(targetFrameworks, targetFramework);
        }

        private static string ComputeValue(string? targetFrameworks, string? targetFramework)
        {
            return (string.IsNullOrEmpty(targetFramework), string.IsNullOrEmpty(targetFrameworks)) switch
            {
                (true, false) => bool.TrueString,
                (false, true) => bool.FalseString,
                _ => bool.TrueString
            };
        }
    }
}
