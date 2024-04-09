// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        private readonly UnconfiguredProject _project;
        private readonly InterceptedPropertiesProviderBase _valueProvider;
        
        public InterceptedProjectProperties(InterceptedPropertiesProviderBase valueProvider, IProjectProperties defaultProperties, UnconfiguredProject project)
            : base(defaultProperties)
        {
            _project = project;
            _valueProvider = valueProvider;
        }

        public override async Task<bool> IsValueInheritedAsync(string propertyName)
        {
            if (!_valueProvider.TryGetInterceptingValueProvider(propertyName, out Providers? propertyValueProviders) || 
                propertyValueProviders.GetFilteredProvider(propertyName, _project.Capabilities.AppliesTo) is not { } valueProvider)
            {
                return await base.IsValueInheritedAsync(propertyName);
            }

            if (valueProvider is IInterceptingPropertyValueProvider2 valueProviderValueWithMsBuildProperties)
            {
                return await valueProviderValueWithMsBuildProperties.IsValueDefinedInContextAsync(propertyName, DelegatedProperties);
            }

            return await base.IsValueInheritedAsync(propertyName);
        }
        

        public override async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            string evaluatedProperty = await base.GetEvaluatedPropertyValueAsync(propertyName);
            if (_valueProvider.TryGetInterceptingValueProvider(propertyName, out Providers? propertyValueProviders) &&
                propertyValueProviders.GetFilteredProvider(propertyName, _project.Capabilities.AppliesTo) is { } valueProvider)
            {
                evaluatedProperty = await valueProvider.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedProperty, DelegatedProperties);
            }

            return evaluatedProperty;
        }

        public override async Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            string? unevaluatedProperty = await base.GetUnevaluatedPropertyValueAsync(propertyName);
            if (_valueProvider.TryGetInterceptingValueProvider(propertyName, out Providers? propertyValueProviders) &&
                propertyValueProviders.GetFilteredProvider(propertyName, _project.Capabilities.AppliesTo) is { } valueProvider)
            {
                unevaluatedProperty = await valueProvider.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedProperty ?? "", DelegatedProperties);
            }

            return unevaluatedProperty;
        }

        public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string? valueToSet;
            if (_valueProvider.TryGetInterceptingValueProvider(propertyName, out Providers? propertyValueProviders) &&
                propertyValueProviders.GetFilteredProvider(propertyName, _project.Capabilities.AppliesTo) is { } valueProvider)
            {
                valueToSet = await valueProvider.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue, DelegatedProperties, dimensionalConditions);
            }
            else
            {
                valueToSet = unevaluatedPropertyValue;
            }

            if (valueToSet is not null)
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
