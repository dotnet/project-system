// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget
{
    /// <summary>
    /// Immutable map of configured project to target framework.
    /// </summary>
    internal sealed class AggregateCrossTargetProjectContext
    {
        private readonly ImmutableDictionary<string, ConfiguredProject> _configuredProjectByTargetFramework;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        public bool IsCrossTargeting { get; }
        public ImmutableArray<TargetFramework> TargetFrameworks { get; }
        public TargetFramework ActiveTargetFramework { get; }

        public AggregateCrossTargetProjectContext(
            bool isCrossTargeting,
            ImmutableArray<TargetFramework> targetFrameworks,
            ImmutableDictionary<string, ConfiguredProject> configuredProjectByTargetFramework,
            TargetFramework activeTargetFramework,
            ITargetFrameworkProvider targetFrameworkProvider)
        {
            Requires.Argument(!targetFrameworks.IsDefaultOrEmpty, nameof(targetFrameworks), "Must contain at least one item.");
            Requires.NotNullOrEmpty(configuredProjectByTargetFramework, nameof(configuredProjectByTargetFramework));
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));

            if (!targetFrameworks.Contains(activeTargetFramework))
            {
                Requires.Argument(false, nameof(targetFrameworks), $"Must contain 'activeTargetFramework' ({activeTargetFramework.TargetFrameworkAlias}). Contains {string.Join(", ", targetFrameworks.Select(targetFramework => $"'{targetFramework.TargetFrameworkAlias}'"))}.");
            }

            IsCrossTargeting = isCrossTargeting;
            TargetFrameworks = targetFrameworks;
            _configuredProjectByTargetFramework = configuredProjectByTargetFramework;
            ActiveTargetFramework = activeTargetFramework;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public IEnumerable<ConfiguredProject> InnerConfiguredProjects => _configuredProjectByTargetFramework.Values;

        public TargetFramework? GetProjectFramework(ProjectConfiguration projectConfiguration)
        {
            if (projectConfiguration.Dimensions.TryGetValue(ConfigurationGeneral.TargetFrameworkProperty, out string targetFrameworkMoniker))
            {
                return _targetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);
            }
            else
            {
                return TargetFrameworks.Length > 1 ? null : TargetFrameworks[0];
            }
        }

        public ConfiguredProject? GetInnerConfiguredProject(TargetFramework target)
        {
            return _configuredProjectByTargetFramework.FirstOrDefault((x, t) => t.Equals(x.Key), target).Value;
        }
    }
}
