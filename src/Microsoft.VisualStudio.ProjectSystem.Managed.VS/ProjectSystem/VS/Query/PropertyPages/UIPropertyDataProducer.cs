// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal static class UIPropertyDataProducer
    {
        public static IEntityValue CreateUIPropertyValue(IEntityValue parent, IPropertyPageQueryCache cache, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(property, nameof(property));

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.UIPropertyName, property.Name)
                });

            return CreateUIPropertyValue(parent.EntityRuntime, identity, cache, property, order, requestedProperties);
        }

        public static IEntityValue CreateUIPropertyValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, IPropertyPageQueryCache cache, BaseProperty property, int order, IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(property, nameof(property));
            var newUIProperty = new UIPropertyValue(runtimeModel, id, new UIPropertyPropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newUIProperty.Name = property.Name;
            }

            if (requestedProperties.DisplayName)
            {
                newUIProperty.DisplayName = property.DisplayName;
            }

            if (requestedProperties.Description)
            {
                newUIProperty.Description = property.Description;
            }

            if (requestedProperties.ConfigurationIndependent)
            {
                bool hasConfigurationCondition = property.DataSource?.HasConfigurationCondition ?? property.ContainingRule.DataSource?.HasConfigurationCondition ?? false;
                newUIProperty.ConfigurationIndependent = !hasConfigurationCondition;
            }

            if (requestedProperties.HelpUrl)
            {
                newUIProperty.HelpUrl = property.HelpUrl;
            }

            if (requestedProperties.CategoryName)
            {
                newUIProperty.CategoryName = property.Category;
            }

            if (requestedProperties.Order)
            {
                newUIProperty.Order = order;
            }

            if (requestedProperties.Type)
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

            if (requestedProperties.SearchTerms)
            {
                // TODO: extract search terms from property metadata.
                newUIProperty.SearchTerms = ImmutableList<string>.Empty;
            }

            ((IEntityValueFromProvider)newUIProperty).ProviderState = (cache, property.ContainingRule, property.Name);

            return newUIProperty;
        }

        public static IEnumerable<IEntityValue> CreateUIPropertyValues(IEntityValue parent, IPropertyPageQueryCache cache, Rule rule, IUIPropertyPropertiesAvailableStatus properties)
        {
            foreach ((int index, BaseProperty property) in rule.Properties.WithIndices())
            {
                if (property.Visible)
                {
                    IEntityValue propertyValue = CreateUIPropertyValue(parent, cache, property, index, properties);
                    yield return propertyValue;
                }
            }
        }

        public static async Task<IEntityValue?> CreateUIPropertyValueAsync(
            IEntityRuntimeModel runtimeModel,
            EntityIdentity requestId,
            IProjectService2 projectService,
            string path,
            string propertyPageName,
            string propertyName,
            IUIPropertyPropertiesAvailableStatus requestedProperties)
        {
            if (projectService.GetLoadedProject(path) is UnconfiguredProject project
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && projectCatalog.GetSchema(propertyPageName) is Rule rule
                && rule.TryGetPropertyAndIndex(propertyName, out BaseProperty? property, out int index)
                && property.Visible)
            {
                var context = new PropertyPageQueryCache(project);
                IEntityValue propertyValue = CreateUIPropertyValue(runtimeModel, requestId, context, property, index, requestedProperties);
                return propertyValue;
            }

            return null;
        }
    }
}
