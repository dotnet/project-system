// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An implementation of IProjectProperties that intercepts all the get/set callbacks for each property on
    /// the given default <see cref="IProjectProperties"/> and passes it to corresponding <see cref="IInterceptingPropertyValueProvider"/>
    /// to validate and/or transform the property value to get/set.
    /// </summary>
    internal sealed class InterceptedProjectProperties : DelegatedProjectPropertiesBase
    {
        private readonly ImmutableDictionary<string, IInterceptingPropertyValueProvider> _valueProviders;

        public InterceptedProjectProperties(ImmutableArray<Lazy<IInterceptingPropertyValueProvider>> valueProviders, IProjectProperties defaultProperties)
            : base(defaultProperties)
        {
            Requires.NotNullOrEmpty(valueProviders, nameof(valueProviders));

            var builder = ImmutableDictionary.CreateBuilder<string, IInterceptingPropertyValueProvider>(StringComparer.OrdinalIgnoreCase);
            foreach (var valueProvider in valueProviders)
            {
                var properyName = valueProvider.Value.GetPropertyName();
                builder.Add(properyName, valueProvider.Value);
            }

            _valueProviders = builder.ToImmutable();
        }

        public override async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            var evaluatedProperty = await base.GetEvaluatedPropertyValueAsync(propertyName).ConfigureAwait(false);

            IInterceptingPropertyValueProvider valueProvider;
            if (_valueProviders.TryGetValue(propertyName, out valueProvider))
            {
                evaluatedProperty = await valueProvider.InterceptGetEvaluatedPropertyValueAsync(evaluatedProperty, DelegatedProperties).ConfigureAwait(false);
            }

            return evaluatedProperty;
        }

        public override async Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            var unevaluatedProperty = await base.GetUnevaluatedPropertyValueAsync(propertyName).ConfigureAwait(false);

            IInterceptingPropertyValueProvider valueProvider;
            if (_valueProviders.TryGetValue(propertyName, out valueProvider))
            {
                unevaluatedProperty = await valueProvider.InterceptGetUnevaluatedPropertyValueAsync(unevaluatedProperty, DelegatedProperties).ConfigureAwait(false);
            }

            return unevaluatedProperty;
        }

        public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            IInterceptingPropertyValueProvider valueProvider;
            if (_valueProviders.TryGetValue(propertyName, out valueProvider))
            {
                unevaluatedPropertyValue = await valueProvider.InterceptSetPropertyValueAsync(unevaluatedPropertyValue, DelegatedProperties, dimensionalConditions).ConfigureAwait(false);
            }

            await base.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions).ConfigureAwait(false);
        }
    }
}