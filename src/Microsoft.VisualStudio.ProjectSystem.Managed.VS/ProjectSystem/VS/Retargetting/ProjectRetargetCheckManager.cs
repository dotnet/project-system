// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectRetargetCheckManager))]
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ProjectRetargetCheckManager : OnceInitializedOnceDisposed, IProjectRetargetCheckManager, IProjectDynamicLoadComponent
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectRetargetingManager _projectRetargetingManager;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private IDisposable? _subscription;

        [ImportingConstructor]
        internal ProjectRetargetCheckManager(ConfiguredProject project,
                                                  IProjectRetargetingManager projectRetargetingManager,
                                                  IProjectSubscriptionService projectSubscriptionService)
        {
            _project = project;
            _projectRetargetingManager = projectRetargetingManager;
            _projectSubscriptionService = projectSubscriptionService;

            ProjectRetargetCheckProviders = new OrderPrecedenceImportCollection<IProjectRetargetCheckProvider>(projectCapabilityCheckProvider: project);
        }

        /// <summary>
        /// Import the LaunchTargetProviders which know how to run profiles
        /// </summary>
        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectRetargetCheckProvider> ProjectRetargetCheckProviders { get; }

        public Task LoadAsync()
        {
            EnsureInitialized();
            return Task.CompletedTask;
        }

        private async Task OnProjectChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> arg)
        {
            List<IProjectTargetChange> changes = new List<IProjectTargetChange>();

            foreach (IProjectRetargetCheckProvider provider in ProjectRetargetCheckProviders.ExtensionValues())
            {
                IProjectTargetChange? change = await provider.CheckAsync(arg.Value.CurrentState);
                if (change != null)
                {
                    changes.Add(change);
                }
            }

            await _projectRetargetingManager.ReportProjectNeedsRetargetingAsync(_project.UnconfiguredProject.FullPath, changes);
        }

        public Task UnloadAsync()
        {
            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            _subscription = _projectSubscriptionService.ProjectRuleSource.SourceBlock.LinkToAsyncAction(
                target: OnProjectChangedAsync,
                _project.UnconfiguredProject);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subscription?.Dispose();
            }
        }
    }
}
