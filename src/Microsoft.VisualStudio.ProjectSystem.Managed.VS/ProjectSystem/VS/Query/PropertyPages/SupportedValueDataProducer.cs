// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="ISupportedValue"/> instances and populating the requested members.
    /// </summary>
    internal abstract class SupportedValueDataProducer : QueryDataProducerBase<IEntityValue>
    {
        protected SupportedValueDataProducer(ISupportedValuePropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected ISupportedValuePropertiesAvailableStatus Properties { get; }

        protected IEntityValue CreateSupportedValue(IEntityRuntimeModel runtimeModel, ProjectSystem.Properties.IEnumValue enumValue)
        {
            var newSupportedValue = new SupportedValueValue(runtimeModel, new SupportedValuePropertiesAvailableStatus());

            if (Properties.Value)
            {
                newSupportedValue.DisplayName = enumValue.DisplayName;
            }

            if (Properties.DisplayName)
            {
                newSupportedValue.Value = enumValue.Name;
            }

            return newSupportedValue;
        }
    }
}
