// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(NeutralLanguagePropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class NeutralLanguageValueProvider : InterceptingPropertyValueProviderBase
    {
        internal const string NeutralLanguagePropertyName = "NeutralLanguage";
        internal const string NoneValue = "(none)";

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (string.Equals(unevaluatedPropertyValue, NoneValue))
            {
                await defaultProperties.DeletePropertyAsync(NeutralLanguagePropertyName);
                return null;
            }

            return unevaluatedPropertyValue;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return Task.FromResult(NoneValue);
            }

            return Task.FromResult(evaluatedPropertyValue);
        }
    }
}
