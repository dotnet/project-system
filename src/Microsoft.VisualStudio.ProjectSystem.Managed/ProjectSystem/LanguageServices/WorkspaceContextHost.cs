// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Responsible for creating and pushing changes to an <see cref="IWorkspaceProjectContext"/> with 
    ///     evaluation and design-time build results.
    /// </summary>
    [Export(typeof(IImplicitlyActiveService))]
    internal partial class WorkspaceContextHost : AbstractMultiLifetimeComponent, IImplicitlyActiveService
    {
        private readonly ConfiguredProject _project;
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;
        private readonly Lazy<ISafeProjectGuidService> _projectGuidService;
        private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

        [ImportingConstructor]
        public WorkspaceContextHost(ConfiguredProject project,
                                    IUnconfiguredProjectCommonServices projectServices,
                                    IProjectThreadingService threadingService,
                                    IProjectSubscriptionService projectSubscriptionService,
                                    Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
                                    Lazy<ISafeProjectGuidService> projectGuidService,
                                    ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
            : base(projectServices.ThreadingService.JoinableTaskContext)
        {
            _project = project;
            _projectServices = projectServices;
            _threadingService = threadingService;
            _projectSubscriptionService = projectSubscriptionService;
            _workspaceProjectContextFactory = workspaceProjectContextFactory;
            _projectGuidService = projectGuidService;
            _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
        }

        public Task ActivateAsync()
        {
            return LoadAsync();
        }

        public Task DeactivateAsync()
        {
            return UnloadAsync();
        }

        protected override AbstractMultiLifetimeInstance CreateInstance()
        {
            return new WorkspaceContextHostInstance(_project, _projectServices, _projectSubscriptionService, _threadingService, _workspaceProjectContextFactory, _projectGuidService, _applyChangesToWorkspaceContextFactory);
        }
    }
}
