// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseProjectConfigurationDimensionProvider"/> class.
        /// </summary>
        /// <param name="projectXmlAccessor">Lock service for the project file.</param>
        /// <param name="dimensionName">Name of the dimension.</param>
        /// <param name="propertyName">Name of the project property containing the dimension values.</param>
        public BaseProjectConfigurationDimensionProvider(IProjectXmlAccessor projectXmlAccessor, string dimensionName, string propertyName)
        {
            Requires.NotNull(projectXmlAccessor, nameof(projectXmlAccessor));
            _projectXmlAccessor = projectXmlAccessor;

            _dimensionName = dimensionName;
            _propertyName = propertyName;
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
                return BuildUtilities.GetPropertyValues(propertyValue);
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
    }
}
