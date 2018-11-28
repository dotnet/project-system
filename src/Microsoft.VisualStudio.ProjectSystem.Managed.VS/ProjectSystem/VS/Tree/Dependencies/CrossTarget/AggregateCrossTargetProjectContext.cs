// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal sealed class AggregateCrossTargetProjectContext
    {
        private readonly ImmutableDictionary<string, ConfiguredProject> _configuredProjectsByTargetFramework;
        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        public bool IsCrossTargeting { get; }
        public ImmutableArray<ITargetFramework> TargetFrameworks { get; }
        public ITargetFramework ActiveTargetFramework { get; }

        public AggregateCrossTargetProjectContext(
            bool isCrossTargeting,
            ImmutableArray<ITargetFramework> targetFrameworks,
            ImmutableDictionary<string, ConfiguredProject> configuredProjectsByTargetFramework,
            ITargetFramework activeTargetFramework,
            ITargetFrameworkProvider targetFrameworkProvider)
        {
            Requires.Argument(!targetFrameworks.IsDefaultOrEmpty, nameof(targetFrameworks), "Must contain at least one item.");
            Requires.NotNullOrEmpty(configuredProjectsByTargetFramework, nameof(configuredProjectsByTargetFramework));
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));
            Requires.Argument(targetFrameworks.Contains(activeTargetFramework), nameof(targetFrameworks), "Must contain 'activeTargetFramework'.");

            IsCrossTargeting = isCrossTargeting;
            TargetFrameworks = targetFrameworks;
            _configuredProjectsByTargetFramework = configuredProjectsByTargetFramework;
            ActiveTargetFramework = activeTargetFramework;
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public IEnumerable<ConfiguredProject> InnerConfiguredProjects => _configuredProjectsByTargetFramework.Values;

        public ITargetFramework GetProjectFramework(ProjectConfiguration projectConfiguration)
        {
            if (projectConfiguration.IsCrossTargeting())
            {
                string targetFrameworkMoniker = projectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty];

                return _targetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);
            }
            else
            {
                return TargetFrameworks.Length > 1 ? null : TargetFrameworks[0];
            }
        }

        public ConfiguredProject GetInnerConfiguredProject(ITargetFramework target)
        {
            return _configuredProjectsByTargetFramework.FirstOrDefault(x => target.Equals(x.Key)).Value;
        }

        /// <summary>
        /// Returns true if this cross-targeting aggregate project context has the same set of target frameworks and active target framework as the given active and known configurations.
        /// </summary>
        public bool HasMatchingTargetFrameworks(ProjectConfiguration activeProjectConfiguration,
                                                IReadOnlyCollection<ProjectConfiguration> knownProjectConfigurations)
        {
            Assumes.True(IsCrossTargeting);
            Assumes.True(activeProjectConfiguration.IsCrossTargeting());
            Assumes.True(knownProjectConfigurations.All(c => c.IsCrossTargeting()));

            ITargetFramework activeTargetFramework = _targetFrameworkProvider.GetTargetFramework(activeProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty]);
            if (!ActiveTargetFramework.Equals(activeTargetFramework))
            {
                // Active target framework is different.
                return false;
            }

            var targetFrameworkMonikers = knownProjectConfigurations
                .Select(c => c.Dimensions[ConfigurationGeneral.TargetFrameworkProperty])
                .Distinct()
                .ToList();

            if (targetFrameworkMonikers.Count != TargetFrameworks.Length)
            {
                // Different number of target frameworks.
                return false;
            }

            foreach (string targetFrameworkMoniker in targetFrameworkMonikers)
            {
                ITargetFramework targetFramework = _targetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);

                if (!TargetFrameworks.Contains(targetFramework))
                {
                    // Differing TargetFramework
                    return false;
                }
            }

            return true;
        }
    }
}
