// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Managed.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Build properties provider for cross-targeting startup projects to ensure that
    /// they build only for the target framework that will be run by F5/Ctrl+F5.
    /// </summary>
    /// <remarks>
    /// The intent here is to speed up the inner-loop development cycle by only building
    /// the target framework that is relevant to the user. Note that this overrides
    /// global build properties in <see cref="TargetFrameworkGlobalBuildPropertyProvider"/>,
    /// so it needs to have a higher <see cref="OrderAttribute"/> value.
    /// </remarks>
    [ExportBuildGlobalPropertiesProvider(designTimeBuildProperties: false)]
    [AppliesTo(ProjectCapability.DotNet + " & " + ProjectCapability.SingleTargetBuildForStartupProjects)]
    [Order(Order.BeforeDefault)]
    internal class StartupProjectSingleTargetGlobalBuildPropertyProvider : StaticGlobalPropertiesProviderBase
    {
        private readonly Task<IImmutableDictionary<string, string>> _empty = Task.FromResult<IImmutableDictionary<string, string>>(Empty.PropertiesMap);
        private ImmutableDictionary<string, IImmutableDictionary<string, string>> _propertiesByTargetFramework = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinalIgnoreCase;

        private readonly ConfiguredProject _configuredProject;
        private readonly IActiveDebugFrameworkServices _activeDebugFrameworkServices;
        private readonly IImplicitlyTriggeredBuildState _implicitlyTriggeredBuildState;
        private readonly IProjectSystemOptions _projectSystemOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFrameworkGlobalBuildPropertyProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        internal StartupProjectSingleTargetGlobalBuildPropertyProvider(
            IProjectService projectService,
            ConfiguredProject configuredProject,
            IActiveDebugFrameworkServices activeDebugFrameworkServices,
            IImplicitlyTriggeredBuildState implicitlyTriggeredBuildState,
            IProjectSystemOptions projectSystemOptions)
            : base(projectService.Services)
        {
            _configuredProject = configuredProject;
            _activeDebugFrameworkServices = activeDebugFrameworkServices;
            _implicitlyTriggeredBuildState = implicitlyTriggeredBuildState;
            _projectSystemOptions = projectSystemOptions;
        }

        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            // Check:
            //   - if this is an implicitly-triggered build
            //   - if there is a single startup project
            //   - if this is the startup project in question
            //   - if this is a cross targeting project, i.e. project configuration has a "TargetFramework" dimension
            //   - if the option to prefer single-target builds is turned on
            if (_implicitlyTriggeredBuildState.IsImplicitlyTriggeredBuild
                && _implicitlyTriggeredBuildState.StartupProjectFullPaths.Length == 1
                && StringComparers.Paths.Equals(_implicitlyTriggeredBuildState.StartupProjectFullPaths[0], _configuredProject.UnconfiguredProject.FullPath)
                && _configuredProject.ProjectConfiguration.IsCrossTargeting())
            {
                // Defer async state machine creation until needed.
                return DoAsync();
            }

            return _empty;

            async Task<IImmutableDictionary<string, string>> DoAsync()
            {
                if (await _projectSystemOptions.GetPreferSingleTargetBuildsForStartupProjectsAsync(cancellationToken))
                {
                    // We only want to build this for the framework that we will launch.
                    string? activeDebuggingFramework = await _activeDebugFrameworkServices.GetActiveDebuggingFrameworkPropertyAsync();
                    
                    if (activeDebuggingFramework is not null)
                    {
                        return ImmutableInterlocked.GetOrAdd(
                            ref _propertiesByTargetFramework,
                            activeDebuggingFramework,
                            tf => Empty.PropertiesMap.Add(ConfigurationGeneral.TargetFrameworkProperty, tf));
                    }
                }

                return Empty.PropertiesMap;
            }
        }
    }
}
