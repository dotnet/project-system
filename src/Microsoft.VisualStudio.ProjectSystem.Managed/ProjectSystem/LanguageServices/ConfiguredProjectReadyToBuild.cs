// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IConfiguredProjectReadyToBuild))]
    [AppliesTo(ProjectCapability.ManagedLanguageService)]
    [Order(int.MaxValue)]
    internal sealed class ConfiguredProjectReadyToBuild : IConfiguredProjectReadyToBuild, IDisposable
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
        
        private TaskCompletionSource<object> _activationTask;

        [ImportingConstructor]
        public ConfiguredProjectReadyToBuild(
            ConfiguredProject configuredProject,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(activeConfiguredProjectProvider, nameof(activeConfiguredProjectProvider));

            _configuredProject = configuredProject;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
            _activationTask = new TaskCompletionSource<object>();

            _activeConfiguredProjectProvider.Changed += ActiveConfiguredProject_Changed;
        }

        private void ActiveConfiguredProject_Changed(object sender, ActiveConfigurationChangedEventArgs e) => GetLatestActivationTask();

        public bool IsValidToBuild => GetLatestActivationTask().IsCompleted;
        public async Task WaitReadyToBuildAsync() => await GetLatestActivationTask().ConfigureAwait(false);

        private Task GetLatestActivationTask()
        {
            lock (_configuredProject)
            {
                var previouslyActive = _activationTask.Task.IsCompleted;
                var nowActive = _configuredProject.ProjectConfiguration.EqualIgnoringTargetFramework(_activeConfiguredProjectProvider.ActiveProjectConfiguration);
                if (previouslyActive)
                {
                    if (!nowActive)
                    {
                        _activationTask = new TaskCompletionSource<object>();
                    }
                }
                else if (nowActive)
                {
                    _activationTask.TrySetResult(null);
                }

                return _activationTask.Task;
            }
        }

        public void Dispose()
        {
            _activationTask.TrySetCanceled();
            _activeConfiguredProjectProvider.Changed -= ActiveConfiguredProject_Changed;            
        }
    }
}
