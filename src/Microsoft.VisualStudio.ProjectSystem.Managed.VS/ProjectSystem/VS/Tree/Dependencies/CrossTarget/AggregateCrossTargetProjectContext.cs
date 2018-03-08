// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    ///     Creates and handles releasing a collection of <see cref="ITargetedProjectContext"/> instances for a given cross targeting project.
    /// </summary>
    internal sealed class AggregateCrossTargetProjectContext
    {
        private readonly ImmutableDictionary<ITargetFramework, ITargetedProjectContext> _configuredProjectContextsByTargetFramework;
        private readonly ImmutableDictionary<string, ConfiguredProject> _configuredProjectsByTargetFramework;
        private readonly ITargetFramework _activeTargetFramework;
        private HashSet<ITargetedProjectContext> _disposedConfiguredProjectContexts;

        public AggregateCrossTargetProjectContext(
            bool isCrossTargeting,
            ImmutableDictionary<ITargetFramework, ITargetedProjectContext> configuredProjectContextsByTargetFramework,
            ImmutableDictionary<string, ConfiguredProject> configuredProjectsByTargetFramework,
            ITargetFramework activeTargetFramework,
            ITargetFrameworkProvider targetFrameworkProvider)
        {
            Requires.NotNullOrEmpty(configuredProjectContextsByTargetFramework, nameof(configuredProjectContextsByTargetFramework));
            Requires.NotNullOrEmpty(configuredProjectsByTargetFramework, nameof(configuredProjectsByTargetFramework));
            Requires.NotNull(activeTargetFramework, nameof(activeTargetFramework));
            Requires.Argument(configuredProjectContextsByTargetFramework.ContainsKey(activeTargetFramework),
                              nameof(configuredProjectContextsByTargetFramework), "Missing ICrossTargetProjectContext for activeTargetFramework");

            IsCrossTargeting = isCrossTargeting;
            _configuredProjectContextsByTargetFramework = configuredProjectContextsByTargetFramework;
            _configuredProjectsByTargetFramework = configuredProjectsByTargetFramework;
            _activeTargetFramework = activeTargetFramework;
            _disposedConfiguredProjectContexts = new HashSet<ITargetedProjectContext>();
            TargetFrameworkProvider = targetFrameworkProvider;
        }

        private ITargetFrameworkProvider TargetFrameworkProvider { get; }

        public IEnumerable<ITargetedProjectContext> InnerProjectContexts => _configuredProjectContextsByTargetFramework.Values;

        public ImmutableArray<ITargetedProjectContext> DisposedInnerProjectContexts => _disposedConfiguredProjectContexts.ToImmutableArray();

        public IEnumerable<ConfiguredProject> InnerConfiguredProjects => _configuredProjectsByTargetFramework.Values;

        public ITargetedProjectContext ActiveProjectContext => _configuredProjectContextsByTargetFramework[_activeTargetFramework];

        public bool IsCrossTargeting { get; }

        public void SetProjectFilePathAndDisplayName(string projectFilePath, string displayName)
        {
            // Update the project file path and display name for all the inner project contexts.
            foreach (var innerProjectContextKvp in _configuredProjectContextsByTargetFramework)
            {
                var targetFramework = innerProjectContextKvp.Key;
                var innerProjectContext = innerProjectContextKvp.Value;

                // For cross targeting projects, we ensure that the display name is unique per every target framework.
                innerProjectContext.DisplayName = IsCrossTargeting ? $"{displayName}({targetFramework})" : displayName;
                innerProjectContext.ProjectFilePath = projectFilePath;
            }
        }

        public ITargetedProjectContext GetInnerProjectContext(ProjectConfiguration projectConfiguration, out bool isActiveConfiguration)
        {
            if (projectConfiguration.IsCrossTargeting())
            {
                var targetFrameworkMoniker =
                    projectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty];
                var targetFramework = TargetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);

                isActiveConfiguration = _activeTargetFramework.Equals(targetFramework);

                return _configuredProjectContextsByTargetFramework.TryGetValue(targetFramework, out ITargetedProjectContext projectContext)
                    ? projectContext
                    : null;
            }
            else
            {
                isActiveConfiguration = true;
                if (_configuredProjectContextsByTargetFramework.Count > 1)
                {
                    return null;
                }

                return InnerProjectContexts.Single();
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
                                                IEnumerable<ProjectConfiguration> knownProjectConfigurations)
        {
            Assumes.True(IsCrossTargeting);
            Assumes.True(activeProjectConfiguration.IsCrossTargeting());
            Assumes.True(knownProjectConfigurations.All(c => c.IsCrossTargeting()));

            var activeTargetFramework = TargetFrameworkProvider.GetTargetFramework(activeProjectConfiguration.Dimensions[ConfigurationGeneral.TargetFrameworkProperty]);
            if (!_activeTargetFramework.Equals(activeTargetFramework))
            {
                // Active target framework is different.
                return false;
            }

            var targetFrameworks = knownProjectConfigurations.Select(
                c => c.Dimensions[ConfigurationGeneral.TargetFrameworkProperty]).ToImmutableHashSet();
            if (targetFrameworks.Count != _configuredProjectContextsByTargetFramework.Count)
            {
                // Different number of target frameworks.
                return false;
            }

            foreach (var targetFrameworkMoniker in targetFrameworks)
            {
                var targetFramework = TargetFrameworkProvider.GetTargetFramework(targetFrameworkMoniker);
                if (!_configuredProjectContextsByTargetFramework.ContainsKey(targetFramework))
                {
                    // Differing TargetFramework
                    return false;
                }
            }

            return true;
        }

        public void Dispose(Func<ITargetedProjectContext, bool> shouldDisposeInnerContext)
        {
            // Dispose the inner project contexts.
            var disposedContexts = new List<ITargetedProjectContext>();
            foreach (var innerProjectContext in InnerProjectContexts)
            {
                if (shouldDisposeInnerContext(innerProjectContext))
                {
                    innerProjectContext.Dispose();
                    disposedContexts.Add(innerProjectContext);
                }
            }

            lock (_disposedConfiguredProjectContexts)
            {
                _disposedConfiguredProjectContexts.AddRange(disposedContexts);
            }
        }
    }
}
