// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal abstract class UIPropertyDataProducer : QueryDataProducerBase<IEntityValue>
    {
        protected UIPropertyDataProducer(IUIPropertyPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected IUIPropertyPropertiesAvailableStatus Properties { get; }

        protected async Task<IEntityValue> CreateUIPropertyValueAsync(IEntityValue entity, PropertyPageQueryCache context, BaseProperty property, int order)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(property, nameof(property));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                new KeyValuePair<string, string>[]
                {
                        new(ProjectModelIdentityKeys.UIPropertyName, property.Name)
                });

            return await CreateUIPropertyValueAsync(entity.EntityRuntime, identity, context, property, order);
        }

        protected Task<IEntityValue> CreateUIPropertyValueAsync(IEntityRuntimeModel runtimeModel, EntityIdentity id, PropertyPageQueryCache context, BaseProperty property, int order)
        {
            Requires.NotNull(property, nameof(property));
            var newUIProperty = new UIPropertyValue(runtimeModel, id, new UIPropertyPropertiesAvailableStatus());

            if (Properties.Name)
            {
                newUIProperty.Name = property.Name;
            }

            if (Properties.DisplayName)
            {
                newUIProperty.DisplayName = property.DisplayName;
            }

            if (Properties.Description)
            {
                newUIProperty.Description = property.Description;
            }

            if (Properties.ConfigurationIndependent)
            {
                bool hasConfigurationCondition = property.DataSource?.HasConfigurationCondition ?? property.ContainingRule.DataSource?.HasConfigurationCondition ?? false;
                newUIProperty.ConfigurationIndependent = !hasConfigurationCondition;
            }

            if (Properties.HelpUrl)
            {
                newUIProperty.HelpUrl = property.HelpUrl;
            }

            if (Properties.CategoryName)
            {
                newUIProperty.CategoryName = property.Category;
            }

            if (Properties.Order)
            {
                newUIProperty.Order = order;
            }

            if (Properties.Type)
            {
                newUIProperty.Type = property switch
                {
                    IntProperty => "int",
                    BoolProperty => "bool",
                    EnumProperty => "enum",
                    DynamicEnumProperty => "enum",
                    StringListProperty => "list",
                    _ => "string"
                };
            }

            if (Properties.SearchTerms)
            {
                // TODO: extract search terms from property metadata.
            }

            ((IEntityValueFromProvider)newUIProperty).ProviderState = (context, property.ContainingRule, property.Name);

            return Task.FromResult<IEntityValue>(newUIProperty);
        }
    }
}
