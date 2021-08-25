// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ImplicitUsings", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ImplicitUsingsValueProvider : InterceptingPropertyValueProviderBase
    {
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = await base.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedPropertyValue, defaultProperties);
            
            return ToBooleanString(value);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = await base.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties);

            return ToBooleanString(value);
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string? value = await base.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties, dimensionalConditions);

            return FromBooleanString(value);
        }

        private static string ToBooleanString(string value)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(value, "enable"))
            {
                return bool.TrueString;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(value, "disable"))
            {
                return bool.FalseString;
            }

            return value;
        }

        private static string? FromBooleanString(string? value)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(value, bool.TrueString))
            {
                return "enable";
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(value, bool.FalseString))
            {
                return "disable";
            }

            return value;
        }
    }
}
