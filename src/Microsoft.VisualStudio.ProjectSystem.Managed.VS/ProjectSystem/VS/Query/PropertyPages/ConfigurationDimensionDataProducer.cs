// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IConfigurationDimensionSnapshot"/> instances and populating the requested
    /// members.
    /// </summary>
    internal static class ConfigurationDimensionDataProducer
    {
        public static IEntityValue CreateProjectConfigurationDimension(
            IEntityRuntimeModel runtimeModel,
            KeyValuePair<string, string> projectConfigurationDimension,
            IConfigurationDimensionPropertiesAvailableStatus requestedProperties)
        {
            var newProjectConfigurationDimension = new ConfigurationDimensionSnapshot(runtimeModel, new ConfigurationDimensionPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newProjectConfigurationDimension.Name = projectConfigurationDimension.Key;
            }

            if (requestedProperties.Value)
            {
                newProjectConfigurationDimension.Value = projectConfigurationDimension.Value;
            }

            return newProjectConfigurationDimension;
        }

        public static IEnumerable<IEntityValue> CreateProjectConfigurationDimensions(IEntityValue parent, ProjectConfiguration configuration, ProjectSystem.Properties.IProperty property, IConfigurationDimensionPropertiesAvailableStatus requestedProperties)
        {
            // If the property is configuration-independent then report no dimensions;
            // the parent property value applies to all configurations.
            if (!property.DataSource.HasConfigurationCondition)
            {
                yield break;
            }

            foreach (KeyValuePair<string, string> dimension in configuration.Dimensions)
            {
                IEntityValue projectConfigurationDimension = CreateProjectConfigurationDimension(parent.EntityRuntime, dimension, requestedProperties);
                yield return projectConfigurationDimension;
            }
        }
    }
}
