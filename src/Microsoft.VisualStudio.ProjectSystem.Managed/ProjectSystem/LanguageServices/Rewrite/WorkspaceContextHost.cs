// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Responsible for creating and pushing changes to an <see cref="IWorkspaceProjectContext"/> with 
    ///     evaluation and design-time build results.
    /// </summary>
    internal partial class WorkspaceContextHost : AbstractImplicitlyActiveComponent
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly Lazy<WorkspaceProjectContextCreator> _workspaceProjectContextCreator;
        private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

        [ImportingConstructor]
        public WorkspaceContextHost(ConfiguredProject project,
                                    IUnconfiguredProjectCommonServices projectServices,
                                    IProjectThreadingService threadingService,
                                    IProjectSubscriptionService projectSubscriptionService,
                                    IConfiguredProjectImplicitActivationTracking activationTracking,
                                    Lazy<WorkspaceProjectContextCreator> workspaceProjectContextCreator,
                                    ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
            : base(activationTracking, projectServices.ThreadingService.JoinableTaskContext)
        {
            _project = project;
            _threadingService = threadingService;
            _projectSubscriptionService = projectSubscriptionService;
            _workspaceProjectContextCreator = workspaceProjectContextCreator;
            _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
        }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.DotNetLanguageService)]
        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override AbstractProjectDynamicLoadInstance CreateInstance()
        {
            return new WorkspaceContextHostInstance(_project, _projectSubscriptionService, _threadingService, _workspaceProjectContextCreator, _applyChangesToWorkspaceContextFactory);
        }
    }
}
