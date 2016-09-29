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
        internal const string TargetFrameworkPropertyName = "TargetFramework";
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

            return ImmutableArray.Create(new KeyValuePair<string, string>(TargetFrameworkPropertyName, targetFrameworks.First()));
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
                var configuredProject = await project.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
                var msbuildProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(false);
                
                // Read the "TargetFrameworks" properties from msbuild project evaluation.
                var targetFrameworksProperty = msbuildProject.Properties
                    .Where(p => p.Name.Equals(ConfigurationGeneral.TargetFrameworksProperty, StringComparison.OrdinalIgnoreCase))
                    .LastOrDefault();

                if (targetFrameworksProperty == null)
                {
                    return ImmutableArray<string>.Empty;
                }

                // TargetFrameworks contains semicolon delimited list of frameworks, for example "net45;netcoreapp1.0;netstandard1.4"
                var targetFrameworksValue = targetFrameworksProperty.EvaluatedValue.Split(';').Select(f => f.Trim());

                // We need to ensure that we return the target frameworks in the specified order.
                var targetFrameworksBuilder = ImmutableArray.CreateBuilder<string>();
                foreach (var targetFramework in targetFrameworksValue)
                {
                    if (!string.IsNullOrEmpty(targetFramework))
                    {
                        targetFrameworksBuilder.Add(targetFramework);
                    }
                }

                return targetFrameworksBuilder.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();
            }
        }
    }
}
