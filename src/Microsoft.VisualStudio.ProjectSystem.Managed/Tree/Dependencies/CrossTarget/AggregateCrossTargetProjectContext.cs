// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    /// Immutable map of configured project to target framework.
    /// </summary>
    internal sealed class AggregateCrossTargetProjectContext
    {
        private readonly ImmutableDictionary<string, ConfiguredProject> _configuredProjectByTargetFramework;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        public bool IsCrossTargeting { get; }
        public ImmutableArray<ITargetFramework> TargetFrameworks { get; }
        public ITargetFramework ActiveTargetFramework { get; }

        public AggregateCrossTargetProjectContext(
            bool isCrossTargeting,
            ImmutableArray<ITargetFramework> targetFrameworks,
            ImmutableDictionary<string, ConfiguredProject> configuredProjectByTargetFramework,
            ITargetFramework activeTargetFramework,
            ITargetFrameworkProvider targetFrameworkProvider)
        {
            Requires.Argument(!targetFrameworks.IsDefaultOrEmpty, nameof(targetFrameworks), "Must contain at least one item.");
            Requires.NotNullOrEmpty(configuredProjectByTargetFramework, nameof(configuredProjectByTargetFramework));
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));
            Requires.Argument(targetFrameworks.Contains(activeTargetFramework), nameof(targetFrameworks), "Must contain 'activeTargetFramework'.");

            IsCrossTargeting = isCrossTargeting;
            TargetFrameworks = targetFrameworks;
            _configuredProjectByTargetFramework = configuredProjectByTargetFramework;
            ActiveTargetFramework = activeTargetFramework;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public IEnumerable<ConfiguredProject> InnerConfiguredProjects => _configuredProjectByTargetFramework.Values;

        public ITargetFramework? GetProjectFramework(ProjectConfiguration projectConfiguration)
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

        public ConfiguredProject? GetInnerConfiguredProject(ITargetFramework target)
        {
            return _configuredProjectByTargetFramework.FirstOrDefault((x, t) => t.Equals(x.Key), target).Value;
        }
    }
}
