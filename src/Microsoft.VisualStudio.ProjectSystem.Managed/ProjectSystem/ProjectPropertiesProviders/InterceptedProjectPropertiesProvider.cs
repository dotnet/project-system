// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    /// <summary>
    /// An intercepting project properties provider that validates and/or transforms the default <see cref="IProjectProperties"/>
    /// using the exported <see cref="IInterceptingPropertyValueProvider"/>s.
    /// </summary>
    [Export("ProjectFileWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "ProjectFileWithInterception")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class InterceptedProjectPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        private readonly ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _interceptingValueProviders;

        [ImportingConstructor]
        public InterceptedProjectPropertiesProvider(
            [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectPropertiesProvider provider,
            [ImportMany]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider)
        {
            _interceptingValueProviders = interceptingValueProviders.ToImmutableArray();
        }

        // internal for testing purpose.
        internal InterceptedProjectPropertiesProvider(
            IProjectPropertiesProvider provider,
            IInterceptingPropertyValueProvider interceptingValueProvider,
            IInterceptingPropertyValueProviderMetadata interceptingValueProviderMetadata)
            : base (provider)
        {
            _interceptingValueProviders = ImmutableArray.Create(new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(() => interceptingValueProvider, interceptingValueProviderMetadata));
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
        {
            var defaultProperties = base.GetProperties(file, itemType, item);
            return _interceptingValueProviders.IsDefaultOrEmpty ? defaultProperties : new InterceptedProjectProperties(_interceptingValueProviders, defaultProperties);
        }
    }
}