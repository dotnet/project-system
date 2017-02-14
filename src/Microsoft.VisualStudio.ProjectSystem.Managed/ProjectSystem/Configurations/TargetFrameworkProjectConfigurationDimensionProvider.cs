// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Provides "TargetFramework" project configuration dimension and values.
    /// </summary>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsInferredFromUsage)]
    internal class TargetFrameworkProjectConfigurationDimensionProvider : IProjectConfigurationDimensionsProvider
    {
        private readonly IProjectLockService _projectLockService;

        [ImportingConstructor]
        public TargetFrameworkProjectConfigurationDimensionProvider(IProjectLockService projectLockService)
        {
            Requires.NotNull(projectLockService, nameof(projectLockService));

            _projectLockService = projectLockService;
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultValuesForDimensionsAsync(UnconfiguredProject project)
        {
            // First target framework is the default one.
            var targetFrameworks = await GetOrderedTargetFrameworksAsync(project).ConfigureAwait(false);
            if (targetFrameworks.IsEmpty)
            {
                return ImmutableArray<KeyValuePair<string, string>>.Empty;
            }

            return ImmutableArray.Create(new KeyValuePair<string, string>(ConfigurationGeneral.TargetFrameworkProperty, targetFrameworks.First()));
        }

        public async Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetProjectConfigurationDimensionsAsync(UnconfiguredProject project)
        {
            var targetFrameworks = await GetOrderedTargetFrameworksAsync(project).ConfigureAwait(false);
            if (targetFrameworks.IsEmpty)
            {
                return ImmutableArray<KeyValuePair<string, IEnumerable<string>>>.Empty;
            }

            return ImmutableArray.Create(new KeyValuePair<string, IEnumerable<string>>(ConfigurationGeneral.TargetFrameworkProperty, targetFrameworks));
        }

        private async Task<ImmutableArray<string>> GetOrderedTargetFrameworksAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            using (var access = await _projectLockService.ReadLockAsync())
            {
                var projectRoot = await access.GetProjectXmlAsync(project.FullPath).ConfigureAwait(false);

                // If the project already defines a specific "TargetFramework" to target, then this is not a cross-targeting project and we don't need a target framework dimension.
                var targetFrameworkProperty = MsBuildUtilities.GetProperty(projectRoot, ConfigurationGeneral.TargetFrameworkProperty);
                if (targetFrameworkProperty != null)
                {
                    return ImmutableArray<string>.Empty;
                }

                // Read the "TargetFrameworks" property from the project file.
                // TODO: https://github.com/dotnet/roslyn-project-system/issues/547
                //       We should read the "TargetFrameworks" properties from msbuild project evaluation at unconfigured project level, but there doesn't seem to be a way to do so.
                return MsBuildUtilities.GetPropertyValues(projectRoot, ConfigurationGeneral.TargetFrameworksProperty);
            }
        }
    }
}
