// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IConfiguredProjectReadyToBuild))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    [Order(int.MaxValue)]
    internal sealed class ConfiguredProjectReadyToBuild : IConfiguredProjectReadyToBuild
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IUnconfiguredProjectCommonServices _commonServices;
        private readonly ActiveConfiguredProjectsIgnoringTargetFrameworkProvider _activeConfiguredProjectsProvider;

        private TaskCompletionSource<object> _activationTask;
        private bool _refreshActivationTask;

        [ImportingConstructor]
        public ConfiguredProjectReadyToBuild(
            ConfiguredProject configuredProject,
            IUnconfiguredProjectCommonServices commonServices,
            ActiveConfiguredProjectsIgnoringTargetFrameworkProvider activeConfiguredProjectsProvider,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(commonServices, nameof(commonServices));
            Requires.NotNull(activeConfiguredProjectsProvider, nameof(activeConfiguredProjectsProvider));
            Requires.NotNull(activeConfiguredProjectProvider, nameof(activeConfiguredProjectProvider));

            _configuredProject = configuredProject;
            _commonServices = commonServices;
            _activeConfiguredProjectsProvider = activeConfiguredProjectsProvider;

            activeConfiguredProjectProvider.Changed += ActiveConfiguredProjectProvider_Changed;
            _activationTask = new TaskCompletionSource<object>();
            _refreshActivationTask = true;
        }

        private void ActiveConfiguredProjectProvider_Changed(object sender, ActiveConfigurationChangedEventArgs e)
        {
            _refreshActivationTask = true;
            Task.Run(async () => await RefreshActivationTaskIfNeeded().ConfigureAwait(false));
        }

        public bool IsValidToBuild => _activationTask.Task.IsCompleted;

        public async Task WaitReadyToBuildAsync()
        {
            await RefreshActivationTaskIfNeeded().ConfigureAwait(false);
            await _activationTask.Task.ConfigureAwait(false);
        }

        private async Task RefreshActivationTaskIfNeeded()
        {
            if (_refreshActivationTask)
            {
                var previouslyActive = IsValidToBuild;
                var activeConfigurations = await _activeConfiguredProjectsProvider.GetActiveProjectConfigurationsAsync().ConfigureAwait(false);
                var nowActive = activeConfigurations.Contains(_configuredProject.ProjectConfiguration);
                if (previouslyActive)
                {
                    if (!nowActive)
                    {
                        Interlocked.Exchange(ref _activationTask, new TaskCompletionSource<object>());
                    }
                }
                else if (nowActive)
                {
                    _activationTask.TrySetResult(null);
                }

                _refreshActivationTask = false;
            }
        }
    }
}
