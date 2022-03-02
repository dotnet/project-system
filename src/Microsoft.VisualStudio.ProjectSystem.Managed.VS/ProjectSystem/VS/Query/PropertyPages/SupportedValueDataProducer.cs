// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="ISupportedValue"/> instances and populating the requested members.
    /// </summary>
    internal static class SupportedValueDataProducer
    {
        public static IEntityValue CreateSupportedValue(IEntityRuntimeModel runtimeModel, ProjectSystem.Properties.IEnumValue enumValue, ISupportedValuePropertiesAvailableStatus requestedProperties)
        {
            var newSupportedValue = new SupportedValueValue(runtimeModel, new SupportedValuePropertiesAvailableStatus());

            if (requestedProperties.DisplayName)
            {
                newSupportedValue.DisplayName = enumValue.DisplayName;
            }

            if (requestedProperties.Value)
            {
                newSupportedValue.Value = enumValue.Name;
            }

            return newSupportedValue;
        }

        public static async Task<IEnumerable<IEntityValue>> CreateSupportedValuesAsync(IEntityValue parent, ProjectSystem.Properties.IProperty property, ISupportedValuePropertiesAvailableStatus requestedProperties)
        {
            if (property is ProjectSystem.Properties.IEnumProperty enumProperty)
            {
                ReadOnlyCollection<ProjectSystem.Properties.IEnumValue> enumValues = await enumProperty.GetAdmissibleValuesAsync();

                return createSupportedValues(enumValues);
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createSupportedValues(ReadOnlyCollection<ProjectSystem.Properties.IEnumValue> enumValues)
            {
                foreach (ProjectSystem.Properties.IEnumValue value in enumValues)
                {
                    IEntityValue supportedValue = CreateSupportedValue(parent.EntityRuntime, value, requestedProperties);
                    yield return supportedValue;
                }
            }
        }
    }
}
