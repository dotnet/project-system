// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class TargetFrameworkProjectConfigurationDimensionProvider : IProjectConfigurationDimensionsProvider
    {
        private const string TargetFrameworkPropertyName = "TargetFramework";
        private const string DefaultTargetFrameworkValue = "netcoreapp1.0";
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
            return ImmutableArray.Create(new KeyValuePair<string, string>(TargetFrameworkPropertyName, targetFrameworks.FirstOrDefault()));
        }

        public async Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetProjectConfigurationDimensionsAsync(UnconfiguredProject project)
        {
            var targetFrameworks = await GetOrderedTargetFrameworksAsync(project).ConfigureAwait(false);
            if (targetFrameworks.IsEmpty)
            {
                return ImmutableArray<KeyValuePair<string, IEnumerable<string>>>.Empty;
            }

            return ImmutableArray.Create(new KeyValuePair<string, IEnumerable<string>>(TargetFrameworkPropertyName, targetFrameworks));
        }

        private async Task<ImmutableArray<string>> GetOrderedTargetFrameworksAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            using (var access = await _projectLockService.ReadLockAsync())
            {
                // Read the "TargetFrameworks" properties from msbuild project evaluation.
                var configuredProject = await project.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
                var msbuildProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(false);
                var targetFrameworksStrings = msbuildProject.Properties.Where(p => p.Name.Equals(ConfigurationGeneral.TargetFrameworksProperty, StringComparison.OrdinalIgnoreCase));

                if (!targetFrameworksStrings.Any())
                {
                    return ImmutableArray<string>.Empty;
                }

                // We need to ensure that we return the target frameworks in specified order.
                var targetFrameworks = ImmutableArray.CreateBuilder<string>();
                foreach (var frameworksString in targetFrameworksStrings)
                {
                    // TargetFrameworks contains semicolon delimited list of frameworks, for example "net45;netcoreapp1.0;netstandard1.4"
                    foreach (var framework in frameworksString.EvaluatedValue.Split(';').Select(f => f.Trim()))
                    {
                        if (!string.IsNullOrEmpty(framework))
                        {
                            // CONSIDER: Do we need to do additional TFM validation here?
                            targetFrameworks.Add(framework);
                        }
                    }
                }

                return targetFrameworks.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
            }
        }
    }
}
