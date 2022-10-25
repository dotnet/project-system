// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// A provider for assembly info properties that are stored either in the project file OR the source code of the project.
    /// </summary>
    internal abstract class AbstractProjectFileOrAssemblyInfoPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        private readonly ImmutableArray<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> _interceptingValueProviders;
        private readonly UnconfiguredProject _project;
        private readonly Func<ProjectId?> _getActiveProjectId;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        protected AbstractProjectFileOrAssemblyInfoPropertiesProvider(
            IProjectPropertiesProvider delegatedProvider,
            IProjectInstancePropertiesProvider instanceProvider,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders,
            UnconfiguredProject project,
            Func<ProjectId?> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base(delegatedProvider, instanceProvider, project)
        {
            Requires.NotNull(interceptingValueProviders, nameof(interceptingValueProviders));
            Requires.NotNull(getActiveProjectId, nameof(getActiveProjectId));
            Requires.NotNull(workspace, nameof(workspace));
            Requires.NotNull(threadingService, nameof(threadingService));

            _interceptingValueProviders = interceptingValueProviders.ToImmutableArray();
            _project = project;
            _getActiveProjectId = getActiveProjectId;
            _workspace = workspace;
            _threadingService = threadingService;
        }

        /// <summary>
        /// Gets the properties for a property or item.
        /// </summary>
        public override IProjectProperties GetProperties(string file, string? itemType, string? item)
        {
            IProjectProperties delegatedProperties = base.GetProperties(file, itemType, item);
            IProjectProperties assemblyInfoProperties = new AssemblyInfoProperties(delegatedProperties, _getActiveProjectId, _workspace, _threadingService);
            return _interceptingValueProviders.IsDefaultOrEmpty ?
                assemblyInfoProperties :
                new InterceptedProjectProperties(_interceptingValueProviders, assemblyInfoProperties, _project);
        }
    }
}
