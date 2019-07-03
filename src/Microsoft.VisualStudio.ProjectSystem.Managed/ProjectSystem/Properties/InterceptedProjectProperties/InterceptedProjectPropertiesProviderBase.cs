// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An intercepting project properties provider that validates and/or transforms the default <see cref="IProjectProperties"/>
    /// using the exported <see cref="IInterceptingPropertyValueProvider"/>s.
    /// </summary>

    internal abstract class InterceptedProjectPropertiesProviderBase : DelegatedProjectPropertiesProviderBase
    {
        private readonly ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _interceptingValueProviders;

        protected InterceptedProjectPropertiesProviderBase(
            IProjectPropertiesProvider provider,
            IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project)
        {
            _interceptingValueProviders = interceptingValueProviders.ToImmutableArray();
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
        {
            IProjectProperties defaultProperties = base.GetProperties(file, itemType, item);
            return InterceptProperties(defaultProperties);
        }

        protected IProjectProperties InterceptProperties(IProjectProperties defaultProperties)
        {
            return _interceptingValueProviders.IsDefaultOrEmpty ? defaultProperties : new InterceptedProjectProperties(_interceptingValueProviders, defaultProperties);
        }
    }
}
