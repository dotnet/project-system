// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides the base class for <see cref="ActiveConfiguredValue{T}"/> and <see cref="ActiveConfiguredValues{T}"/>.
    /// </summary>
    internal abstract class AbstractActiveConfiguredValue<T> : OnceInitializedOnceDisposed
    {
        private T _value = default!;
        private readonly UnconfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;

        protected AbstractActiveConfiguredValue(UnconfiguredProject project, IActiveConfiguredProjectProvider activeConfiguredProjectProvider, IProjectThreadingService threadingService)
        {
            _project = project;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
            _threadingService = threadingService;
        }

        public T Value
        {
            get
            {
                EnsureInitialized();

                return _value!;
            }
        }

        protected override void Initialize()
        {
            _activeConfiguredProjectProvider.Changed += OnActiveConfigurationChanged;

            ConfiguredProject? configuredProject = _activeConfiguredProjectProvider.ActiveConfiguredProject;
            if (configuredProject is null)
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
                });
            }

            Assumes.NotNull(configuredProject);

            SetValueForConfiguration(configuredProject);
        }

        protected override void Dispose(bool disposing)
        {
            _activeConfiguredProjectProvider.Changed -= OnActiveConfigurationChanged;
        }

        protected abstract T GetValue(ConfiguredProject project);

        private void OnActiveConfigurationChanged(object sender, ActiveConfigurationChangedEventArgs e)
        {
            SetValueForConfiguration(e.NowActive);
        }

        private void SetValueForConfiguration(ConfiguredProject project)
        {
            _value = GetValue(project);
        }
    }
}
