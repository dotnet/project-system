// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IConfigurationDimension"/> instances and populating the requested
    /// members.
    /// </summary>
    internal abstract class ConfigurationDimensionDataProducer : QueryDataProducerBase<IEntityValue>
    {
        public ConfigurationDimensionDataProducer(IConfigurationDimensionPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected IConfigurationDimensionPropertiesAvailableStatus Properties { get; }

        protected IEntityValue CreateProjectConfigurationDimension(IEntityRuntimeModel runtimeModel, KeyValuePair<string, string> projectConfigurationDimension)
        {
            var newProjectConfigurationDimension = new ConfigurationDimensionValue(runtimeModel, new ConfigurationDimensionPropertiesAvailableStatus());

            if (Properties.Name)
            {
                newProjectConfigurationDimension.Name = projectConfigurationDimension.Key;
            }

            if (Properties.Value)
            {
                newProjectConfigurationDimension.Value = projectConfigurationDimension.Value;
            }

            return newProjectConfigurationDimension;
        }
    }
}
