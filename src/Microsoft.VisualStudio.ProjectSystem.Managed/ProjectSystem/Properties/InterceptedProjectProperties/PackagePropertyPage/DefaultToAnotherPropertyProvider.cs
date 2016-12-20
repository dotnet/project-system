// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    internal abstract class DefaultToAnotherPropertyProvider : InterceptingPropertyValueProviderBase
    {
        protected abstract string DelegatedPropertyName { get; }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            // Default value is from the DelegatedProperty
            return await defaultProperties.GetEvaluatedPropertyValueAsync(DelegatedPropertyName).ConfigureAwait(true);
        }

        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            if (string.IsNullOrEmpty(unevaluatedPropertyValue))
            {
                return null;
            }

            // Explicitly set the new value only if it differs from the current DelegatedProperty value.
            var assemblyName = await defaultProperties.GetEvaluatedPropertyValueAsync(DelegatedPropertyName).ConfigureAwait(true);
            if (StringComparers.PropertyValues.Equals(assemblyName, unevaluatedPropertyValue))
            {
                return null;
            }

            return await base.OnSetPropertyValueAsync(unevaluatedPropertyValue, defaultProperties, dimensionalConditions).ConfigureAwait(true);
        }
    }
}
