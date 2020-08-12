// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;

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
                string[] propertyNames = valueProvider.Metadata.PropertyNames;

                foreach (string propertyName in propertyNames)
                {
                    Requires.Argument(!string.IsNullOrEmpty(propertyName), nameof(valueProvider), "A null or empty property name was found");

                    // CONSIDER: Allow duplicate intercepting property value providers for same property name.
                    Requires.Argument(!builder.ContainsKey(propertyName), nameof(valueProviders), "Duplicate property value providers for same property name");

                    builder.Add(propertyName, valueProvider);
                }
            }

            _valueProviders = builder.ToImmutable();
        }

        public override async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            string evaluatedProperty = await base.GetEvaluatedPropertyValueAsync(propertyName);
            if (_valueProviders.TryGetValue(propertyName, out Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>? valueProvider))
            {
                evaluatedProperty = await valueProvider.Value.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedProperty, DelegatedProperties);
            }

            return evaluatedProperty;
        }

        public override async Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            string? unevaluatedProperty = await base.GetUnevaluatedPropertyValueAsync(propertyName);
            if (_valueProviders.TryGetValue(propertyName, out Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>? valueProvider))
            {
                unevaluatedProperty = await valueProvider.Value.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedProperty ?? "", DelegatedProperties);
            }

            return unevaluatedProperty;
        }

        public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string? valueToSet;
            if (_valueProviders.TryGetValue(propertyName, out Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>? valueProvider))
            {
                valueToSet = await valueProvider.Value.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue, DelegatedProperties, dimensionalConditions);
            }
            else
            {
                valueToSet = unevaluatedPropertyValue;
            }

            if (valueToSet != null)
            {
                await base.SetPropertyValueAsync(propertyName, valueToSet, dimensionalConditions);
            }
        }

        public void SetRuleContext(Rule rule)
        {
            var ruleAwareProperties = DelegatedProperties as IRuleAwareProjectProperties;
            ruleAwareProperties?.SetRuleContext(rule);
        }
    }
}
