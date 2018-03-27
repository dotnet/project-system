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
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly ISafeProjectGuidService _projectGuidService;
        private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;
        private readonly ExportFactory<IApplyChangesToWorkspaceContext> _applyChangesToWorkspaceContextFactory;

        [ImportingConstructor]
        public WorkspaceContextHost(ConfiguredProject project,
                                    IUnconfiguredProjectCommonServices projectServices,
                                    IProjectSubscriptionService projectSubscriptionService,
                                    IConfiguredProjectImplicitActivationTracking activationTracking,
                                    ISafeProjectGuidService projectGuidService,
                                    Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
                                    ExportFactory<IApplyChangesToWorkspaceContext> applyChangesToWorkspaceContextFactory)
            : base(activationTracking, projectServices.ThreadingService.JoinableTaskContext)
        {
            _project = project;
            _projectServices = projectServices;
            _projectSubscriptionService = projectSubscriptionService;
            _projectGuidService = projectGuidService;
            _workspaceProjectContextFactory = workspaceProjectContextFactory;
            _applyChangesToWorkspaceContextFactory = applyChangesToWorkspaceContextFactory;
        }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
        public Task InitializeAsync()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override AbstractProjectDynamicLoadInstance CreateInstance()
        {
            return new WorkspaceContextHostInstance(_project, _projectServices, _projectSubscriptionService, _projectGuidService, _workspaceProjectContextFactory, _applyChangesToWorkspaceContextFactory);
        }
    }
}
