// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Base intercepting project property provider that intercepts all the callbacks for a specific property name
    /// on the default <see cref="IProjectPropertiesProvider"/> for validation and/or transformation of the property value.
    /// </summary>
    internal abstract class InterceptingPropertyValueProviderBase : IInterceptingPropertyValueProvider
    {
        /// <inheritdoc />
        public abstract string GetPropertyName();

        /// <inheritdoc />
        public virtual Task<string> InterceptGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(evaluatedPropertyValue);
        }

        /// <inheritdoc />
        public virtual Task<string> InterceptGetUnevaluatedPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(unevaluatedPropertyValue);
        }

        /// <inheritdoc />
        public virtual Task<string> InterceptSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            return Task.FromResult(unevaluatedPropertyValue);
        }
    }
}