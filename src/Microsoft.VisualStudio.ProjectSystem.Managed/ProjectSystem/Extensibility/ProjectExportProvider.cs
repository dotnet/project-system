// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;

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
