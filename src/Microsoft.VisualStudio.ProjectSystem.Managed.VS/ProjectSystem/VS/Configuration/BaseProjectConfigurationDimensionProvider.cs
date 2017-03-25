// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    /// <summary>
    /// Base project configuration dimension provider
    /// </summary>
    internal abstract class BaseProjectConfigurationDimensionProvider : IProjectConfigurationDimensionsProvider2
    {
        protected readonly string _dimensionName;
        protected readonly string _propertyName;
        protected readonly IProjectXmlAccessor _projectXmlAccessor;
        protected readonly ITelemetryService _telemetryService;
        protected readonly bool _valueContainsPii;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseProjectConfigurationDimensionProvider"/> class.
        /// </summary>
        /// <param name="projectXmlAccessor">Lock service for the project file.</param>
        /// <param name="telemetryService">Telemetry service.</param>
        /// <param name="dimensionName">Name of the dimension.</param>
        /// <param name="propertyName">Name of the project property containing the dimension values.</param>
        /// <param name="valueContainsPii">Value indicating wherther the dimension values contain pii and should be hashed for telemetry.</param>
        public BaseProjectConfigurationDimensionProvider(IProjectXmlAccessor projectXmlAccessor, ITelemetryService telemetryService, string dimensionName, string propertyName, bool valueContainsPii)
        {
            Requires.NotNull(projectXmlAccessor, nameof(projectXmlAccessor));
            _projectXmlAccessor = projectXmlAccessor;

            Requires.NotNull(projectXmlAccessor, nameof(telemetryService));
            _telemetryService = telemetryService;

            _dimensionName = dimensionName;
            _propertyName = propertyName;
            _valueContainsPii = valueContainsPii;
        }

        /// <summary>
        /// Gets the property values for the dimension.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project.</param>
        /// <returns>Collection of values for the dimension.</returns>
        /// <remarks>
        /// From <see cref="IProjectConfigurationDimensionsProvider"/>.
        /// </remarks>
        protected virtual async Task<ImmutableArray<string>> GetOrderedPropertyValuesAsync(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            string propertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(true);
            if (propertyValue == null || string.IsNullOrEmpty(propertyValue))
            {
                return ImmutableArray<string>.Empty;
            }
            else
            {
                return MsBuildUtilities.GetPropertyValues(propertyValue);
            }
        }

        /// <summary>
        /// Gets the defaults values for project configuration dimensions for the given unconfigured project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project.</param>
        /// <returns>Collection of key/value pairs for the defaults values for the configuration dimensions of this provider for given project.</returns>
        /// <remarks>
        /// From <see cref="IProjectConfigurationDimensionsProvider"/>.
        /// The interface expectes a collection of key/value pairs containing one or more dimensions along with a single values for each
        /// dimension. In this implementation each provider is representing a single dimension.
        /// </remarks>
        public virtual async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultValuesForDimensionsAsync(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            var values = await GetOrderedPropertyValuesAsync(unconfiguredProject).ConfigureAwait(false);
            if (values.IsEmpty)
            {
                return ImmutableArray<KeyValuePair<string, string>>.Empty;
            }
            else
            {
                // First value is the default one.
                var defaultValues = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
                defaultValues.Add(new KeyValuePair<string, string>(_dimensionName, values.First()));
                return defaultValues.ToImmutable();
            }
        }

        /// <summary>
        /// Gets the project configuration dimension and values represented by this provider for the given unconfigured project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project.</param>
        /// <returns>Collection of key/value pairs for the current values for the configuration dimensions of this provider for given project.</returns>
        /// <remarks>
        /// From <see cref="IProjectConfigurationDimensionsProvider"/>.
        /// The interface expectes a collection of key/value pairs containing one or more dimensions along with the values for each
        /// dimension. In this implementation each provider is representing a single dimension with one or more values.
        /// </remarks>
        public virtual async Task<IEnumerable<KeyValuePair<string, IEnumerable<string>>>> GetProjectConfigurationDimensionsAsync(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            var values = await GetOrderedPropertyValuesAsync(unconfiguredProject).ConfigureAwait(false);
            if (values.IsEmpty)
            {
                return ImmutableArray<KeyValuePair<string, IEnumerable<string>>>.Empty;
            }
            else
            {
                var dimensionValues = ImmutableArray.CreateBuilder<KeyValuePair<string, IEnumerable<string>>>();
                dimensionValues.Add(new KeyValuePair<string, IEnumerable<string>>(_dimensionName, values));
                return dimensionValues.ToImmutable();
            }
        }

        /// <summary>
        /// Modifies the project when there's a configuration change.
        /// </summary>
        /// <param name="args">Information about the configuration dimension value change.</param>
        /// <returns>A task for the async operation.</returns>
        /// From <see cref="IProjectConfigurationDimensionsProvider2"/>.
        public abstract Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args);

        /// <summary>
        /// Gets the property value for the dimension property of the specified project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project.</param>
        /// <returns>Value of the dimension property.</returns>
        /// <remarks>
        /// This needs to get the evaluated property in order to get inherited properties defines in props or targets.
        /// </remarks>
        protected async Task<string> GetPropertyValue(UnconfiguredProject unconfiguredProject)
        {
            return await _projectXmlAccessor.GetEvaluatedPropertyValue(unconfiguredProject, _propertyName).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a dimension value to the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="dimensionValue">Name of the new dimension value.</param>
        /// <returns>A task for the async operation.</returns>
        protected async Task OnDimensionValueAddedAsync(UnconfiguredProject unconfiguredProject, string dimensionValue)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await _projectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                MsBuildUtilities.AppendPropertyValue(msbuildProject, evaluatedPropertyValue, _propertyName, dimensionValue);
            }).ConfigureAwait(false);

            _telemetryService.PostProperty($"DimensionChanged/{_dimensionName}/Add", "Value", HashValueIfNeeded(dimensionValue), unconfiguredProject);
        }

        /// <summary>
        /// Removes a dimension value from the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="dimensionValue">Name of the deleted dimension value.</param>
        /// <returns>A task for the async operation.</returns>
        protected async Task OnDimensionValueRemovedAsync(UnconfiguredProject unconfiguredProject, string dimensionValue)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await _projectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                MsBuildUtilities.RemovePropertyValue(msbuildProject, evaluatedPropertyValue, _propertyName, dimensionValue);
            }).ConfigureAwait(false);

            _telemetryService.PostProperty($"DimensionChanged/{_dimensionName}/Remove", "Value", HashValueIfNeeded(dimensionValue), unconfiguredProject);
        }


        /// <summary>
        /// Renames an existing dimension value in the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="oldName">Original name of the dimension value.</param>
        /// <param name="newName">New name of the dimension value.</param>
        /// <returns>A task for the async operation.</returns>
        protected async Task OnDimensionValueRenamedAsync(UnconfiguredProject unconfiguredProject, string oldName, string newName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await _projectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                MsBuildUtilities.RenamePropertyValue(msbuildProject, evaluatedPropertyValue, _propertyName, oldName, newName);
            }).ConfigureAwait(false);

            List<(string propertyName, string propertyValue)> properties = new List<(string propertyName, string propertyValue)>()
            {
                ("OldValue", HashValueIfNeeded(oldName)),
                ("NewValue", HashValueIfNeeded(newName)),
            };

            _telemetryService.PostProperties($"DimensionChanged/{_dimensionName}/Rename", properties, unconfiguredProject);
        }

        private string HashValueIfNeeded(string value)
        {
            return _valueContainsPii ? _telemetryService.HashValue(value) : value;
        }
    }
}
