// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// A provider for assembly info properties that are stored either in the project file OR the source code of the project.
    /// </summary>
    internal abstract class AbstractProjectFileOrAssemblyInfoPropertiesProvider : InterceptedPropertiesProviderBase
    {
        private readonly UnconfiguredProject _project;
        private readonly Func<ProjectId?> _getActiveProjectId;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        protected AbstractProjectFileOrAssemblyInfoPropertiesProvider(
            IProjectPropertiesProvider delegatedProvider,
            IProjectInstancePropertiesProvider instanceProvider,
            IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata2>> interceptingValueProviders,
            UnconfiguredProject project,
            Func<ProjectId?> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base(delegatedProvider, instanceProvider, project, interceptingValueProviders)
        {
            Requires.NotNull(getActiveProjectId);
            Requires.NotNull(workspace);
            Requires.NotNull(threadingService);

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
            return HasInterceptingValueProvider
                ? new InterceptedProjectProperties(this, assemblyInfoProperties, _project)
                : assemblyInfoProperties;
        }
    }
}
