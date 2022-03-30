// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    /// MEF component which has methods for consumers to get to project specific MEF exports
    /// </summary>
    [Export(typeof(IProjectExportProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ProjectExportProvider : IProjectExportProvider
    {
        private readonly IProjectServiceAccessor _projectServiceAccessor;

        [ImportingConstructor]
        public ProjectExportProvider(IProjectServiceAccessor serviceAccessor)
        {
            _projectServiceAccessor = serviceAccessor;
        }

        public T? GetExport<T>(string projectFilePath) where T : class
        {
            Requires.NotNullOrEmpty(projectFilePath, nameof(projectFilePath));

            IProjectService projectService = _projectServiceAccessor.GetProjectService();

            UnconfiguredProject? project = projectService?.LoadedUnconfiguredProjects
                .FirstOrDefault(x => string.Equals(x.FullPath, projectFilePath, StringComparisons.Paths));

            return project?.Services.ExportProvider.GetExportedValueOrDefault<T>();
        }
    }
}
