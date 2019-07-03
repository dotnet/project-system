// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    [Export(typeof(IConfiguredProjectReadyToBuild))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    [Order(Order.Default)]
    internal sealed class ImplicitlyActiveConfiguredProjectReadyToBuild : IConfiguredProjectReadyToBuild, IDisposable
    {
        private readonly ConfiguredProject _configuredProject;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;

        private TaskCompletionSource<object?> _activationTask;

        [ImportingConstructor]
        public ImplicitlyActiveConfiguredProjectReadyToBuild(
            ConfiguredProject configuredProject,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider)
        {
            _configuredProject = configuredProject;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
            _activationTask = new TaskCompletionSource<object?>();

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
                        _activationTask = new TaskCompletionSource<object?>();
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

        private bool IsActive()
        {
            ProjectConfiguration? activeConfig = _activeConfiguredProjectProvider.ActiveProjectConfiguration;

            if (activeConfig == null)
                return false;

            return _configuredProject.ProjectConfiguration.EqualIgnoringTargetFramework(activeConfig);
        }
    }
}
