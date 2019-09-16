// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// A provider for assembly info properties that are stored either in the project file OR the source code of the project.
    /// </summary>
    internal abstract class AbstractProjectFileOrAssemblyInfoPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        private readonly ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _interceptingValueProviders;
        private readonly Func<ProjectId> _getActiveProjectId;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        protected AbstractProjectFileOrAssemblyInfoPropertiesProvider(
            IProjectPropertiesProvider delegatedProvider,
            IProjectInstancePropertiesProvider instanceProvider,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders,
            UnconfiguredProject project,
            Func<ProjectId> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base(delegatedProvider, instanceProvider, project)
        {
            Requires.NotNull(interceptingValueProviders, nameof(interceptingValueProviders));
            Requires.NotNull(getActiveProjectId, nameof(getActiveProjectId));
            Requires.NotNull(workspace, nameof(workspace));
            Requires.NotNull(threadingService, nameof(threadingService));

            _interceptingValueProviders = interceptingValueProviders.ToImmutableArray();
            _getActiveProjectId = getActiveProjectId;
            _workspace = workspace;
            _threadingService = threadingService;
        }

        /// <summary>
        /// Gets the properties for a property or item.
        /// </summary>
        public override IProjectProperties GetProperties(string file, string itemType, string item)
        {
            IProjectProperties delegatedProperties = base.GetProperties(file, itemType, item);
            IProjectProperties assemblyInfoProperties = new AssemblyInfoProperties(delegatedProperties, _getActiveProjectId, _workspace, _threadingService);
            return _interceptingValueProviders.IsDefaultOrEmpty ?
                assemblyInfoProperties :
                new InterceptedProjectProperties(_interceptingValueProviders, assemblyInfoProperties);
        }
    }
}
