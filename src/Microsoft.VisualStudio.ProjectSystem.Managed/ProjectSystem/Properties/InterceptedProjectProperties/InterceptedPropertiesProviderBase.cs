// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An intercepting project properties provider that validates and/or transforms the default <see cref="IProjectProperties"/>
    /// using the exported <see cref="IInterceptingPropertyValueProvider"/>s.
    /// </summary>
    internal class InterceptedPropertiesProviderBase : DelegatedProjectPropertiesProviderBase
    {
        private readonly Dictionary<string, Providers> _interceptingValueProviders = new(StringComparers.PropertyNames);

        protected InterceptedPropertiesProviderBase(
            IProjectPropertiesProvider provider,
            IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>> interceptingValueProviders)
            : base(provider, instanceProvider, project)
        {
            Requires.NotNullOrEmpty(interceptingValueProviders);

            foreach (Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2> valueProvider in interceptingValueProviders)
            {
                string[] propertyNames = valueProvider.Metadata.PropertyNames;

                foreach (string propertyName in propertyNames)
                {
                    Requires.Argument(!string.IsNullOrEmpty(propertyName), nameof(valueProvider), "A null or empty property name was found");

                    if (!_interceptingValueProviders.TryGetValue(propertyName, out Providers? entry))
                    {
                        entry = new Providers(new List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>>(1) { valueProvider });
                        _interceptingValueProviders.Add(propertyName, entry);
                    }
                    else
                    {
                        entry.Exports.Add(valueProvider);
                    }
                }
            }
        }

        protected bool HasInterceptingValueProvider => _interceptingValueProviders.Count > 0;

        internal bool TryGetInterceptingValueProvider(string propertyName, [NotNullWhen(returnValue: true)] out Providers? propertyValueProviders)
        {
            return _interceptingValueProviders.TryGetValue(propertyName, out propertyValueProviders);
        }
    }

    internal class Providers
    {
        public Providers(List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>> exports)
        {
            Exports = exports;
        }

        public List<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>> Exports { get; }

        public IInterceptingPropertyValueProvider? GetFilteredProvider(
            string propertyName,
            Func<string, bool> appliesToEvaluator)
        {
            // todo consider caching this based on capability
            IInterceptingPropertyValueProvider? firstProvider = null;
            foreach (var lazyProvider in Exports)
            {
                string? appliesToExpression = lazyProvider.Metadata.AppliesTo;
                if (appliesToExpression is null || appliesToEvaluator(appliesToExpression))
                {
                    if (firstProvider is null)
                    {
                        firstProvider = lazyProvider.Value;
                    }
                    else if (lazyProvider.Value.GetType() != firstProvider.GetType())
                    {
                        throw new ArgumentException($"Duplicate property value providers for same property name: {propertyName}");
                    }
                }
            }

            return firstProvider;
        }
    }
}
