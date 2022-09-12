// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    [Export(typeof(IConfiguredProjectReadyToBuild))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    [Order(Order.Default)]
    internal sealed class ImplicitlyActiveConfiguredProjectReadyToBuild : IConfiguredProjectReadyToBuild, IDisposable
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;

        private TaskCompletionSource _activationTask;

        [ImportingConstructor]
        public ImplicitlyActiveConfiguredProjectReadyToBuild(
            ConfiguredProject configuredProject,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider)
        {
            _configuredProject = configuredProject;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
            _activationTask = new TaskCompletionSource();

            _activeConfiguredProjectProvider.Changed += ActiveConfiguredProject_Changed;
        }

        private void ActiveConfiguredProject_Changed(object sender, ActiveConfigurationChangedEventArgs e) => GetLatestActivationTask();

        public bool IsValidToBuild => GetLatestActivationTask().IsCompleted;

        public Task WaitReadyToBuildAsync() => GetLatestActivationTask();

        private Task GetLatestActivationTask()
        {
            lock (_configuredProject)
            {
                bool previouslyActive = _activationTask.Task.IsCompleted;
                bool nowActive = IsActive();
                if (previouslyActive)
                {
                    if (!nowActive)
                    {
                        _activationTask = new TaskCompletionSource();
                    }
                }
                else if (nowActive)
                {
                    _activationTask.TrySetResult();
                }

                return _activationTask.Task;
            }
        }

        public void Dispose()
        {
            _activationTask.TrySetCanceled();
            _activeConfiguredProjectProvider.Changed -= ActiveConfiguredProject_Changed;
        }

        private bool IsActive()
        {
            ProjectConfiguration? activeConfig = _activeConfiguredProjectProvider.ActiveProjectConfiguration;

            if (activeConfig is null)
                return false;

            return _configuredProject.ProjectConfiguration.EqualIgnoringTargetFramework(activeConfig);
        }
    }
}
