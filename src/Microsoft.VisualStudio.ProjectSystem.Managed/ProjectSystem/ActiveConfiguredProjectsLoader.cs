// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Force loads the active <see cref="ConfiguredProject"/> objects so that any configured project-level 
    ///     services, such as evaluation and build services, are started.
    /// </summary>
    internal class ActiveConfiguredProjectsLoader : OnceInitializedOnceDisposed
    {
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly UnconfiguredProject _project;
        private IDisposable _subscription;

        [ImportingConstructor]
        public ActiveConfiguredProjectsLoader(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
        }

        [ProjectAutoLoad(ProjectLoadCheckpoint.ProjectInitialCapabilitiesEstablished)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
        private Task OnProjectFactoryCompletedAsync()
        {
            Initialize();
            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            Action<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> target = OnActiveConfigurationsChanged;

            _subscription = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.LinkTo(
                target: new ActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>(target),
                linkOptions: new DataflowLinkOptions() { PropagateCompletion = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subscription?.Dispose();
            }
        }

        private void OnActiveConfigurationsChanged(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> e)
        {
            foreach (ProjectConfiguration configuration in e.Value)
            {
                _project.LoadConfiguredProjectAsync(configuration);
            }
        }
    }
}
