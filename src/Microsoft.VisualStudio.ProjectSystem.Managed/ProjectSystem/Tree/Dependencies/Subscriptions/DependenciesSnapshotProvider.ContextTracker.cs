// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    internal sealed partial class DependenciesSnapshotProvider
    {
        /// <summary>
        /// Used by <see cref="DependenciesSnapshotProvider"/> to keep an <see cref="AggregateCrossTargetProjectContext"/>
        /// instance up to date.
        /// </summary>
        private sealed class ContextTracker
        {
            private readonly ITargetFrameworkProvider _targetFrameworkProvider;
            private readonly IUnconfiguredProjectCommonServices _commonServices;
            private readonly Lazy<AggregateCrossTargetProjectContextProvider> _contextProvider;
            private readonly IActiveProjectConfigurationRefreshService _activeProjectConfigurationRefreshService;

            public ContextTracker(
                ITargetFrameworkProvider targetFrameworkProvider,
                IUnconfiguredProjectCommonServices commonServices,
                Lazy<AggregateCrossTargetProjectContextProvider> contextProvider,
                IActiveProjectConfigurationRefreshService activeProjectConfigurationRefreshService)
            {
                Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));
                Requires.NotNull(commonServices, nameof(commonServices));
                Requires.NotNull(contextProvider, nameof(contextProvider));
                Requires.NotNull(activeProjectConfigurationRefreshService, nameof(activeProjectConfigurationRefreshService));

                _targetFrameworkProvider = targetFrameworkProvider;
                _commonServices = commonServices;
                _contextProvider = contextProvider;
                _activeProjectConfigurationRefreshService = activeProjectConfigurationRefreshService;
            }

            /// <summary>
            ///     Current <see cref="AggregateCrossTargetProjectContext"/>, which is an immutable map of
            ///     configured project to target framework.
            /// </summary>
            /// <remarks>
            /// <para>
            ///     Updates of this field are serialized within a lock, but reads are free threaded as any
            ///     potential race can only be handled outside this class.
            /// </para>
            /// <para>
            ///     Value is null before initialization, and not null after.
            /// </para>
            /// </remarks>
            public AggregateCrossTargetProjectContext? Current { get; private set; }

            public async Task<AggregateCrossTargetProjectContext?> TryUpdateCurrentAggregateProjectContextAsync()
            {
                AggregateCrossTargetProjectContext? previousContext = Current;

                // Check if we have already computed the project context.
                if (previousContext is not null)
                {
                    // For non-cross targeting projects, we can use the current project context if the TargetFramework hasn't changed.
                    // For cross-targeting projects, we need to verify that the current project context matches latest frameworks targeted by the project.
                    // If not, we create a new one and dispose the current one.
                    ConfigurationGeneral projectProperties = await _commonServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();

                    if (!previousContext.IsCrossTargeting)
                    {
                        string? newTargetFrameworkName = (string?)await projectProperties.TargetFramework.GetValueAsync();

                        if (string.IsNullOrEmpty(newTargetFrameworkName) && TargetFramework.Empty.Equals(previousContext.ActiveTargetFramework))
                        {
                            // No change
                            return null;
                        }

                        TargetFramework? newTargetFramework = _targetFrameworkProvider.GetTargetFramework(newTargetFrameworkName);
                        if (previousContext.ActiveTargetFramework.Equals(newTargetFramework))
                        {
                            // No change
                            return null;
                        }
                    }
                    else
                    {
                        // Check if the current project context is up-to-date for the current active and known project configurations.
                        Assumes.Present(_commonServices.Project.Services.ProjectConfigurationsService);
                        ProjectConfiguration activeProjectConfiguration = _commonServices.ActiveConfiguredProject.ProjectConfiguration;
                        IImmutableSet<ProjectConfiguration> knownProjectConfigurations = await _commonServices.Project.Services.ProjectConfigurationsService.GetKnownProjectConfigurationsAsync();
                        if (knownProjectConfigurations.All(c => c.IsCrossTargeting()) &&
                            HasMatchingTargetFrameworks(activeProjectConfiguration, knownProjectConfigurations))
                        {
                            // No change
                            return null;
                        }
                    }
                }

                // Force refresh the CPS active project configuration (needs UI thread).
                await _commonServices.ThreadingService.SwitchToUIThread();
                await _activeProjectConfigurationRefreshService.RefreshActiveProjectConfigurationAsync();

                // Create new project context.
                AggregateCrossTargetProjectContext newContext = await _contextProvider.Value.CreateProjectContextAsync();

                Current = newContext;

                return newContext;

                bool HasMatchingTargetFrameworks(
                    ProjectConfiguration activeProjectConfiguration,
                    IReadOnlyCollection<ProjectConfiguration> knownProjectConfigurations)
                {
                    Assumes.NotNull(previousContext);
                    Assumes.True(activeProjectConfiguration.IsCrossTargeting());

                    TargetFramework? activeTargetFramework = _targetFrameworkProvider.GetTargetFramework(activeProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty]);

                    if (!previousContext.ActiveTargetFramework.Equals(activeTargetFramework))
                    {
                        // Active target framework is different.
                        return false;
                    }

                    var targetFrameworkMonikers = knownProjectConfigurations
                        .Select(c => c.Dimensions[ConfigurationGeneral.TargetFrameworkProperty])
                        .Distinct()
                        .ToList();

                    if (targetFrameworkMonikers.Count != previousContext.TargetFrameworks.Length)
                    {
                        // Different number of target frameworks.
                        return false;
                    }

                    foreach (string targetFrameworkMoniker in targetFrameworkMonikers)
                    {
                        TargetFramework? targetFramework = _targetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);

                        if (targetFramework is null || !previousContext.TargetFrameworks.Contains(targetFramework))
                        {
                            // Differing TargetFramework
                            return false;
                        }
                    }

                    return true;
                }
            }
        }
    }
}
