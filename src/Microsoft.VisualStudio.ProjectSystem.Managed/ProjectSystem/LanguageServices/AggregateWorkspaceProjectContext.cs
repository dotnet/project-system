// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Creates and handles releasing a collection of <see cref="IWorkspaceProjectContext"/> instances for a given cross targeting project.
    /// </summary>
    internal sealed class AggregateWorkspaceProjectContext : IDisposable
    {
        private readonly ImmutableDictionary<string, IWorkspaceProjectContext> _configuredProjectContextsByTargetFrameworks;

        public AggregateWorkspaceProjectContext(ImmutableDictionary<string, IWorkspaceProjectContext> configuredProjectContextsByTargetFrameworks)
        {
            Requires.NotNullOrEmpty(configuredProjectContextsByTargetFrameworks, nameof(configuredProjectContextsByTargetFrameworks));

            _configuredProjectContextsByTargetFrameworks = configuredProjectContextsByTargetFrameworks;
        }

        // IWorkspaceProjectContext implements the VS-only interface IVsLanguageServiceBuildErrorReporter2
        public object HostSpecificErrorReporter => InnerProjectContexts.First();

        public IEnumerable<IWorkspaceProjectContext> InnerProjectContexts => _configuredProjectContextsByTargetFrameworks.Values;

        public void SetProjectFilePathAndDisplayName(string projectFilePath, string displayName)
        {
            var isCrossTargeting = _configuredProjectContextsByTargetFrameworks.Count > 1;

            // Update the project file path and display name for all the inner project contexts.
            foreach (var innerProjectContextKvp in _configuredProjectContextsByTargetFrameworks)
            {
                // For cross targeting projects, we ensure that the display name is unique per every target framework.
                var targetFramework = innerProjectContextKvp.Key;
                var innerProjectContext = innerProjectContextKvp.Value;
                innerProjectContext.DisplayName = isCrossTargeting ? $"{displayName}({targetFramework})" : displayName;

                innerProjectContext.ProjectFilePath = projectFilePath;
            }
        }

        public IWorkspaceProjectContext GetProjectContext(ProjectConfiguration projectConfiguration)
        {
            if (projectConfiguration.IsCrossTargeting())
            {
                var targetFramework = projectConfiguration.Dimensions[TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName];
                return _configuredProjectContextsByTargetFrameworks[targetFramework];
            }
            else
            {
                Requires.Range(_configuredProjectContextsByTargetFrameworks.Count == 1, nameof(_configuredProjectContextsByTargetFrameworks));
                return InnerProjectContexts.Single();
            }
        }

        public void Dispose()
        {
            // Dispose all the inner project contexts.
            foreach (var innerProjectContext in InnerProjectContexts)
            {
                innerProjectContext.Dispose();
            }
        }
    }
}
