// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Base project configuration dimension provider
    /// </summary>
    internal abstract class BaseProjectConfigurationDimensionProvider : IProjectConfigurationDimensionsProviderInternal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseProjectConfigurationDimensionProvider"/> class.
        /// </summary>
        /// <param name="projectAccessor">Lock service for the project file.</param>
        /// <param name="dimensionName">Name of the dimension.</param>
        /// <param name="propertyName">Name of the project property containing the dimension values.</param>
        /// <param name="dimensionDefaultValue">The default value of the dimension, for example "AnyCPU".</param>
        protected BaseProjectConfigurationDimensionProvider(IProjectAccessor projectAccessor, string dimensionName, string propertyName, string? dimensionDefaultValue = null)
        {
            Requires.NotNull(projectAccessor, nameof(projectAccessor));

            ProjectAccessor = projectAccessor;
            DimensionName = dimensionName;
            PropertyName = propertyName;
            DimensionDefaultValue = dimensionDefaultValue;
        }

        public string DimensionName { get; }
        public string PropertyName { get; }
        public string? DimensionDefaultValue { get; }
        public IProjectAccessor ProjectAccessor { get; }

        /// <summary>
        /// Gets the property values for the dimension.
        /// </summary>
        /// <param name="project">Unconfigured project.</param>
        /// <returns>Collection of values for the dimension.</returns>
        /// <remarks>
        /// From <see cref="IProjectConfigurationDimensionsProvider"/>.
        /// </remarks>
        private async Task<ImmutableArray<string>> GetOrderedPropertyValuesAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            ConfiguredProject? configuredProject = await project.GetSuggestedConfiguredProjectAsync();

            Assumes.NotNull(configuredProject);

            // Need evaluated property to get inherited properties defined in props or targets.
            return await ProjectAccessor.OpenProjectForReadAsync(configuredProject, GetOrderedPropertyValues);
        }

        /// <summary>
        /// Gets the property values for the dimension.
        /// </summary>
        /// <param name="project"><see cref="Project"/>.</param>
        /// <returns>Collection of values for the dimension.</returns>
        private ImmutableArray<string> GetOrderedPropertyValues(Project project)
        {
            Requires.NotNull(project, nameof(project));

            string? propertyValue = project.GetProperty(PropertyName)?.EvaluatedValue;

            if (Strings.IsNullOrEmpty(propertyValue))
            {
                return ImmutableArray<string>.Empty;
            }
            else
            {
                return BuildUtilities.GetPropertyValues(propertyValue).ToImmutableArray();
            }
        }

        /// <summary>
        /// Gets the defaults values for project configuration dimensions for the given unconfigured project.
        /// </summary>
        /// <param name="project">Unconfigured project.</param>
        /// <returns>Collection of key/value pairs for the defaults values for the configuration dimensions of this provider for given project.</returns>
        /// <remarks>
        /// From <see cref="IProjectConfigurationDimensionsProvider"/>.
        /// The interface expects a collection of key/value pairs containing one or more dimensions along with a single values for each
        /// dimension. In this implementation each provider is representing a single dimension.
        /// </remarks>
        public virtual async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultValuesForDimensionsAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            ImmutableArray<string> values = await GetOrderedPropertyValuesAsync(project);
            if (values.IsEmpty)
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }
            else
            {
                // First value is the default one.
                return new[] { new KeyValuePair<string, string>(DimensionName, values[0]) };
            }
        }

        /// <summary>
        /// Gets the project configuration dimension and values represented by this provider for the given unconfigured project.
        /// </summary>
        /// <param name="project">Unconfigured project.</param>
        /// <returns>Collection of key/value pairs for the current values for the configuration dimensions of this provider for given project.</returns>
        /// <remarks>
        /// From <see cref="IProjectConfigurationDimensionsProvider"/>.
        /// The interface expects a collection of key/value pairs containing one or more dimensions along with the values for each
        /// dimension. In this implementation each provider is representing a single dimension with one or more values.
        /// </remarks>
        public virtual async Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetProjectConfigurationDimensionsAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            ImmutableArray<string> values = await GetOrderedPropertyValuesAsync(project);
            if (values.IsEmpty)
            {
                return Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>();
            }
            else
            {
                return new[] { new KeyValuePair<string, IEnumerable<string>>(DimensionName, values) };
            }
        }

        public IEnumerable<string> GetBestGuessDimensionNames(ImmutableArray<ProjectPropertyElement> properties)
        {
            if (FindDimensionProperty(properties) is not null)
                return new string[] { DimensionName };

            return Array.Empty<string>();
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject project)
        {
            string? defaultValue = await FindDefaultValueFromDimensionPropertyAsync(project) ?? DimensionDefaultValue;
            if (defaultValue is not null)
                return new[] { new KeyValuePair<string, string>(DimensionName, defaultValue) };

            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// Modifies the project when there's a configuration change.
        /// </summary>
        /// <param name="args">Information about the configuration dimension value change.</param>
        /// <returns>A task for the async operation.</returns>
        public Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            if (StringComparers.ConfigurationDimensionNames.Equals(args.DimensionName, DimensionName))
            {
                if (args.Stage == ChangeEventStage.Before)
                {
                    switch (args.Change)
                    {
                        case ConfigurationDimensionChange.Add:
                            return UpdateUnderLockAsync(args.Project, (msbuildProject, evaluatedPropertyValue) =>
                                BuildUtilities.AppendPropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, args.DimensionValue));

                        case ConfigurationDimensionChange.Delete:
                            return UpdateUnderLockAsync(args.Project, (msbuildProject, evaluatedPropertyValue) =>
                                BuildUtilities.RemovePropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, args.DimensionValue));

                        case ConfigurationDimensionChange.Rename:
                            // Need to wait until the core rename changes happen before renaming the property.
                            break;
                    }
                }
                else if (args.Stage == ChangeEventStage.After)
                {
                    // Only change that needs to be handled here is renaming configurations which needs to happen after all
                    // of the core changes to rename existing conditions have executed.
                    if (args.Change == ConfigurationDimensionChange.Rename)
                    {
                        return UpdateUnderLockAsync(args.Project, (msbuildProject, evaluatedPropertyValue) =>
                            BuildUtilities.RenamePropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, args.OldDimensionValue, args.DimensionValue));
                    }
                }
            }

            return Task.CompletedTask;

            async Task UpdateUnderLockAsync(UnconfiguredProject project, Action<ProjectRootElement, string> action)
            {
                ConfiguredProject? configuredProject = await project.GetSuggestedConfiguredProjectAsync();

                Assumes.NotNull(configuredProject);

                await ProjectAccessor.OpenProjectForUpgradeableReadAsync(configuredProject, evaluatedProject =>
                {
                    string evaluatedPropertyValue = evaluatedProject.GetPropertyValue(PropertyName);

                    return ProjectAccessor.OpenProjectXmlForWriteAsync(project, msbuildProject =>
                    {
                        action(msbuildProject, evaluatedPropertyValue);
                    });
                });
            }
        }

        public virtual Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetProjectConfigurationDimensionsAsync(Project project)
        {
            Requires.NotNull(project, nameof(project));

            ImmutableArray<string> values = GetOrderedPropertyValues(project);
            if (values.IsEmpty)
            {
                return TaskResult.EmptyEnumerable<KeyValuePair<string, IEnumerable<string>>>();
            }
            else
            {
                IEnumerable<KeyValuePair<string, IEnumerable<string>>> dimensionValues = new[] { new KeyValuePair<string, IEnumerable<string>>(DimensionName, values) };
                return Task.FromResult(dimensionValues);
            }
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject project, string solutionConfiguration)
        {
            return GetBestGuessDefaultValuesForDimensionsAsync(project);
        }

        private async Task<string?> FindDefaultValueFromDimensionPropertyAsync(UnconfiguredProject project)
        {
            string? values = await FindDimensionPropertyAsync(project);
            if (Strings.IsNullOrEmpty(values))
                return null;

            foreach (string defaultValue in BuildUtilities.GetPropertyValues(values))
            {
                // If this property is derived from another property, skip it and just
                // pull default from next known values. This is better than picking a 
                // default that is not actually one of the known configs.
                if (defaultValue.IndexOf("$(", StringComparisons.PropertyValues) == -1)
                    return defaultValue;
            }

            return null;
        }

        private Task<string?> FindDimensionPropertyAsync(UnconfiguredProject project)
        {
            return ProjectAccessor.OpenProjectXmlForReadAsync(
                project,
                projectXml => FindDimensionProperty(projectXml)?.GetUnescapedValue());
        }

        private ProjectPropertyElement? FindDimensionProperty(ProjectRootElement projectXml)
        {
            IEnumerable<ProjectPropertyElement> properties = projectXml.PropertyGroups
                                                                       .SelectMany(group => group.Properties);

            return FindDimensionProperty(properties);
        }

        private ProjectPropertyElement? FindDimensionProperty(IEnumerable<ProjectPropertyElement> properties)
        {
            // NOTE: We try to somewhat mimic evaluation, but it doesn't have to be exact; its just a guess
            // at what "might" be the default configuration, not what it actually is.
            return properties.Reverse()
                             .FirstOrDefault(
                                p => StringComparers.PropertyNames.Equals(PropertyName, p.Name) &&
                                BuildUtilities.HasWellKnownConditionsThatAlwaysEvaluateToTrue(p));
        }
    }
}
