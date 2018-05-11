// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a default implementation of <see cref="IUnconfiguredProjectCommonServices"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectCommonServices))]
    internal class UnconfiguredProjectCommonServices : IUnconfiguredProjectCommonServices
    {
        private readonly UnconfiguredProject _project;
        private readonly Lazy<IPhysicalProjectTree> _projectTree;
        private readonly Lazy<IProjectThreadingService> _threadingService;
        private readonly Lazy<IProjectLockService> _projectLockService;
        private readonly ActiveConfiguredProject<ConfiguredProject> _activeConfiguredProject;
        private readonly ActiveConfiguredProject<ProjectProperties> _activeConfiguredProjectProperties;

        [ImportingConstructor]
        public UnconfiguredProjectCommonServices(UnconfiguredProject project, Lazy<IPhysicalProjectTree> projectTree, Lazy<IProjectThreadingService> threadingService,
                                                 ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject, ActiveConfiguredProject<ProjectProperties> activeConfiguredProjectProperties,
                                                 Lazy<IProjectLockService> projectLockService)
        {
            _project = project;
            _projectTree = projectTree;
            _threadingService = threadingService;
            _activeConfiguredProject = activeConfiguredProject;
            _activeConfiguredProjectProperties = activeConfiguredProjectProperties;
            _projectLockService = projectLockService;
        }

        public IProjectThreadingService ThreadingService
        {
            get { return _threadingService.Value; }
        }

        public UnconfiguredProject Project
        {
            get { return _project; }
        }

        public IPhysicalProjectTree ProjectTree
        {
            get { return _projectTree.Value; }
        }

        public ConfiguredProject ActiveConfiguredProject
        {
            get { return _activeConfiguredProject.Value; }
        }

        public ProjectProperties ActiveConfiguredProjectProperties
        {
            get { return _activeConfiguredProjectProperties.Value; }
        }

        public IProjectLockService ProjectLockService
        {
            get { return _projectLockService.Value; }
        }
    }
}
