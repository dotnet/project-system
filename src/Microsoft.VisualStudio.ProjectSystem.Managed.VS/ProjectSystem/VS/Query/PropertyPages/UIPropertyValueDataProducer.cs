// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IUIPropertyValue"/> instances and populating the requested members.
    /// </summary>
    internal abstract class UIPropertyValueDataProducer : QueryDataProducerBase<IEntityValue>
    {
        protected UIPropertyValueDataProducer(IUIPropertyValuePropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected IUIPropertyValuePropertiesAvailableStatus Properties { get; }

        protected async Task<IEntityValue> CreateUIPropertyValueValueAsync(IEntityValue entity, ProjectConfiguration configuration, ProjectSystem.Properties.IProperty property)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(property, nameof(property));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                Enumerable.Empty<KeyValuePair<string, string>>());

            return await CreateUIPropertyValueValueAsync(entity.EntityRuntime, identity, configuration, property);
        }

        protected async Task<IEntityValue> CreateUIPropertyValueValueAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, ProjectConfiguration configuration, ProjectSystem.Properties.IProperty property)
        {
            Requires.NotNull(property, nameof(property));

            var newUIPropertyValue = new UIPropertyValueValue(runtimeModel, id, new UIPropertyValuePropertiesAvailableStatus());

            if (Properties.UnevaluatedValue)
            {
                if (property is IEvaluatedProperty evaluatedProperty)
                {
                    newUIPropertyValue.UnevaluatedValue = await evaluatedProperty.GetUnevaluatedValueAsync();
                }
                else
                {
                    newUIPropertyValue.UnevaluatedValue = await property.GetValueAsync() as string;
                }
            }

            if (Properties.EvaluatedValue)
            {
                newUIPropertyValue.EvaluatedValue = property switch
                {
                    IBoolProperty boolProperty => await boolProperty.GetValueAsBoolAsync(),
                    IStringProperty stringProperty => await stringProperty.GetValueAsStringAsync(),
                    IIntProperty intProperty => await intProperty.GetValueAsIntAsync(),
                    IEnumProperty enumProperty => (await enumProperty.GetValueAsIEnumValueAsync())?.Name,
                    IStringListProperty stringListProperty => await stringListProperty.GetValueAsStringCollectionAsync(),
                    _ => await property.GetValueAsync()
                };
            }

            ((IEntityValueFromProvider)newUIPropertyValue).ProviderState = (configuration, property);

            return newUIPropertyValue;
        }
    }
}
