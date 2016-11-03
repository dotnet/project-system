// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Provides "TargetFramework" project configuration dimension and values.
    /// </summary>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsInferredFromUsage)]
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

        private string GetProperty(ProjectRootElement projectRoot, string propertyName)
        {
            return projectRoot.Properties
                .Where(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                .Select(p => ProjectCollection.Unescape(p.Value)) // it was escaped on the way in, so unescape on the way out.
                .FirstOrDefault();
        }

        private async Task<ImmutableArray<string>> GetOrderedTargetFrameworksAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            using (var access = await _projectLockService.ReadLockAsync())
            {
                var projectRoot = await access.GetProjectXmlAsync(project.FullPath).ConfigureAwait(false);

                // If the project already defines a specific "TargetFramework" to target, then this is not a cross-targeting project and we don't need a target framework dimension.
                var targetFrameworkProperty = GetProperty(projectRoot, TargetFrameworkPropertyName);
                if (!string.IsNullOrEmpty(targetFrameworkProperty))
                {
                    return ImmutableArray<string>.Empty;
                }

                // Read the "TargetFrameworks" property from the project file.
                // TODO: https://github.com/dotnet/roslyn-project-system/issues/547
                //       We should read the "TargetFrameworks" properties from msbuild project evaluation at unconfigured project level, but there doesn't seem to be a way to do so.
                var targetFrameworksProperty = GetProperty(projectRoot, ConfigurationGeneral.TargetFrameworksProperty);
                if (string.IsNullOrEmpty(targetFrameworksProperty))
                {
                    return ImmutableArray<string>.Empty;
                }

                return ParseTargetFrameworks(targetFrameworksProperty);
            }
        }

        internal static ImmutableArray<string> ParseTargetFrameworks(string targetFrameworksProperty)
        {
            // TargetFrameworks contains semicolon delimited list of frameworks, for example "net45;netcoreapp1.0;netstandard1.4"
            var targetFrameworksValue = targetFrameworksProperty.Split(';').Select(f => f.Trim());

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
