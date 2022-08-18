// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ImplicitUsings", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ImplicitUsingsEnabledValueProvider : InterceptingPropertyValueProviderBase
    {
        private static readonly Task<string?> s_enableStringTaskResult = Task.FromResult<string?>("enable");
        private static readonly Task<string?> s_disableStringTaskResult = Task.FromResult<string?>("disable");

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return ToBooleanStringAsync(evaluatedPropertyValue);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return ToBooleanStringAsync(unevaluatedPropertyValue);
        }

        public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            return FromBooleanStringAsync(unevaluatedPropertyValue);
        }

        private static Task<string> ToBooleanStringAsync(string value)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(value, "enable"))
            {
                return TaskResult.TrueString;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(value, "disable"))
            {
                return TaskResult.FalseString;
            }

            return Task.FromResult(value);
        }

        private static Task<string?> FromBooleanStringAsync(string? value)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(value, bool.TrueString))
            {
                return s_enableStringTaskResult;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(value, bool.FalseString))
            {
                return s_disableStringTaskResult;
            }

            if (value is null)
            {
                return TaskResult.Null<string>();
            }

            return Task.FromResult<string?>(value);
        }
    }
}
