// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An intercepting project properties provider that validates and/or transforms the default <see cref="IProjectProperties"/>
    /// using the exported <see cref="IInterceptingPropertyValueProvider"/>s.
    /// </summary>
    internal abstract class InterceptedProjectPropertiesProviderBase : InterceptedPropertiesProviderBase
    {
        private readonly UnconfiguredProject _project;

        protected InterceptedProjectPropertiesProviderBase(
            IProjectPropertiesProvider provider,
            IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
            _project = project;
        }

        public override IProjectProperties GetProperties(string file, string? itemType, string? item)
        {
            IProjectProperties defaultProperties = base.GetProperties(file, itemType, item);
            return InterceptProperties(defaultProperties);
        }

        protected IProjectProperties InterceptProperties(IProjectProperties defaultProperties)
        {
            return HasInterceptingValueProvider ? new InterceptedProjectProperties(this, defaultProperties, _project) : defaultProperties;
        }
    }
}
