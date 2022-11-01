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
        private readonly ImmutableDictionary<string, Providers> _valueProviders;
        
        public InterceptedProjectProperties(ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> valueProviders, IProjectProperties defaultProperties, UnconfiguredProject project)
            : base(defaultProperties)
        {
            _project = project;
            Requires.NotNullOrEmpty(valueProviders, nameof(valueProviders));

            ImmutableDictionary<string, Providers>.Builder builder = 
                ImmutableDictionary.CreateBuilder<string, Providers>(StringComparers.PropertyNames);
            
            foreach (Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> valueProvider in valueProviders)
            {
                string[] propertyNames = valueProvider.Metadata.PropertyNames;

                foreach (string propertyName in propertyNames)
                {
                    Requires.Argument(!string.IsNullOrEmpty(propertyName), nameof(valueProvider), "A null or empty property name was found");

                    if (!builder.TryGetValue(propertyName, out Providers? entry))
                    {
                        entry = new Providers(new List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> { valueProvider });
                        builder.Add(propertyName, entry);
                    }

                    entry.Exports.Add(valueProvider);
                }
            }

            _valueProviders = builder.ToImmutable();
        }

        public override async Task<bool> IsValueInheritedAsync(string propertyName)
        {
            if (!_valueProviders.TryGetValue(propertyName, out Providers? propertyValueProviders) || 
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
            if (_valueProviders.TryGetValue(propertyName, out Providers? propertyValueProviders) &&
                propertyValueProviders.GetFilteredProvider(propertyName, _project.Capabilities.AppliesTo) is { } valueProvider)
            {
                evaluatedProperty = await valueProvider.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedProperty, DelegatedProperties);
            }

            return evaluatedProperty;
        }

        public override async Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            string? unevaluatedProperty = await base.GetUnevaluatedPropertyValueAsync(propertyName);
            if (_valueProviders.TryGetValue(propertyName, out Providers? propertyValueProviders) &&
                propertyValueProviders.GetFilteredProvider(propertyName, _project.Capabilities.AppliesTo) is { } valueProvider)
            {
                unevaluatedProperty = await valueProvider.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedProperty ?? "", DelegatedProperties);
            }

            return unevaluatedProperty;
        }

        public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string? valueToSet;
            if (_valueProviders.TryGetValue(propertyName, out Providers? propertyValueProviders) &&
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

    internal class Providers
    {
        public Providers(List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> exports)
        {
            Exports = exports;
        }

        public List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> Exports { get; private set; }

        public IInterceptingPropertyValueProvider? GetFilteredProvider(
            string propertyName,
            Func<string, bool> appliesToEvaluator)
        {
            // todo consider caching this based on capability
            var foundExports = Exports.Where(lazyProvider =>
            {
                string? appliesToExpression = lazyProvider.Value.GetType()
                    .GetCustomAttributes(typeof(AppliesToAttribute), inherit: true)
                    .OfType<AppliesToAttribute>()
                    .FirstOrDefault()?.AppliesTo;

                return appliesToExpression is null || appliesToEvaluator(appliesToExpression);
            })
                .GroupBy(x => x.Value.GetType()) // in case we end up importing multiple of the same provider, which *has happened with TargetFrameworkMoniker* 
                .Select(x => x.First())
                .ToList();

            return foundExports.Count switch
            {
                0 => null,
                1 => foundExports.First().Value,
                _ => throw new ArgumentException($"Duplicate property value providers for same property name: {propertyName}")
            };
        }
    }
}
