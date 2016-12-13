// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// A provider for assembly info properties that are stored either in the project file OR the source code of the project.
    /// </summary>
    internal abstract class AbstractProjectFileOrAssemblyInfoPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        private readonly ProjectProperties _projectProperties;
        private readonly ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _interceptingValueProviders;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        public AbstractProjectFileOrAssemblyInfoPropertiesProvider(
            IProjectPropertiesProvider delegatedProvider,
            IProjectInstancePropertiesProvider instanceProvider,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders,
            UnconfiguredProject unconfiguredProject,
            ProjectProperties projectProperties,
            ILanguageServiceHost languageServiceHost,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base (delegatedProvider, instanceProvider, unconfiguredProject)
        {
            Requires.NotNull(projectProperties, nameof(projectProperties));
            Requires.NotNull(interceptingValueProviders, nameof(interceptingValueProviders));
            Requires.NotNull(languageServiceHost, nameof(languageServiceHost));
            Requires.NotNull(workspace, nameof(workspace));
            Requires.NotNull(threadingService, nameof(threadingService));
            
            _projectProperties = projectProperties;
            _interceptingValueProviders = interceptingValueProviders.ToImmutableArray();
            _languageServiceHost = languageServiceHost;
            _workspace = workspace;
            _threadingService = threadingService;
        }

        /// <summary>
        /// Gets the properties for a property or item.
        /// </summary>
        public override IProjectProperties GetProperties(string file, string itemType, string item)
        {
            var delegatedProperties = base.GetProperties(file, itemType, item);
            IProjectProperties assemblyInfoProperties = new AssemblyInfoProperties(delegatedProperties, _projectProperties, _languageServiceHost, _workspace, _threadingService);
            return _interceptingValueProviders.IsDefaultOrEmpty ?
                assemblyInfoProperties :
                new InterceptedProjectProperties(_interceptingValueProviders, assemblyInfoProperties);
        }
    }
}
