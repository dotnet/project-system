// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An intercepting project properties provider that validates and/or transforms the default <see cref="IProjectProperties"/>
    /// using the exported <see cref="IInterceptingPropertyValueProvider"/>s.
    /// </summary>
    internal abstract class InterceptedProjectPropertiesProviderBase : DelegatedProjectPropertiesProviderBase
    {
        private readonly UnconfiguredProject _project;
        private readonly ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _interceptingValueProviders;

        protected InterceptedProjectPropertiesProviderBase(
            IProjectPropertiesProvider provider,
            IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project)
        {
            _project = project;
            _interceptingValueProviders = interceptingValueProviders.ToImmutableArray();
        }

        public override IProjectProperties GetProperties(string file, string? itemType, string? item)
        {
            IProjectProperties defaultProperties = base.GetProperties(file, itemType, item);
            return InterceptProperties(defaultProperties);
        }

        protected IProjectProperties InterceptProperties(IProjectProperties defaultProperties)
        {
            return _interceptingValueProviders.IsDefaultOrEmpty ? defaultProperties : new InterceptedProjectProperties(_interceptingValueProviders, defaultProperties, _project);
        }
    }
}
