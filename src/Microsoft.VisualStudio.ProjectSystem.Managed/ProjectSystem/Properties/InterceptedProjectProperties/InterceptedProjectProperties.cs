// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An implementation of IProjectProperties that intercepts all the get/set callbacks for each property on
    /// the given default <see cref="IProjectProperties"/> and passes it to corresponding <see cref="IInterceptingPropertyValueProvider"/>
    /// to validate and/or transform the property value to get/set.
    /// </summary>
    internal sealed class InterceptedProjectProperties : DelegatedProjectPropertiesBase, IRuleAwareProjectProperties
    {
        private readonly ImmutableDictionary<string, Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _valueProviders;

        public InterceptedProjectProperties(ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> valueProviders, IProjectProperties defaultProperties)
            : base(defaultProperties)
        {
            Requires.NotNullOrEmpty(valueProviders, nameof(valueProviders));

            ImmutableDictionary<string, Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>>.Builder builder = ImmutableDictionary.CreateBuilder<string, Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>>(StringComparers.PropertyNames);
            foreach (Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> valueProvider in valueProviders)
            {
                string propertyName = valueProvider.Metadata.PropertyName;

                // CONSIDER: Allow duplicate intercepting property value providers for same property name.
                Requires.Argument(!builder.ContainsKey(propertyName), nameof(valueProviders), "Duplicate property value providers for same property name");

                builder.Add(propertyName, valueProvider);
            }

            _valueProviders = builder.ToImmutable();
        }

        public override async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            string evaluatedProperty = await base.GetEvaluatedPropertyValueAsync(propertyName);
            if (_valueProviders.TryGetValue(propertyName, out Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> valueProvider))
            {
                evaluatedProperty = await valueProvider.Value.OnGetEvaluatedPropertyValueAsync(evaluatedProperty, DelegatedProperties);
            }

            return evaluatedProperty;
        }

        public override async Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            string unevaluatedProperty = await base.GetUnevaluatedPropertyValueAsync(propertyName);
            if (_valueProviders.TryGetValue(propertyName, out Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> valueProvider))
            {
                unevaluatedProperty = await valueProvider.Value.OnGetUnevaluatedPropertyValueAsync(unevaluatedProperty, DelegatedProperties);
            }

            return unevaluatedProperty;
        }

        public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            if (_valueProviders.TryGetValue(propertyName, out Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> valueProvider))
            {
                unevaluatedPropertyValue = await valueProvider.Value.OnSetPropertyValueAsync(unevaluatedPropertyValue, DelegatedProperties, dimensionalConditions);
            }

            if (unevaluatedPropertyValue != null)
            {
                await base.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions);
            }
        }

        public void SetRuleContext(Rule rule)
        {
            var ruleAwareProperties = DelegatedProperties as IRuleAwareProjectProperties;
            ruleAwareProperties?.SetRuleContext(rule);
        }
    }
}
