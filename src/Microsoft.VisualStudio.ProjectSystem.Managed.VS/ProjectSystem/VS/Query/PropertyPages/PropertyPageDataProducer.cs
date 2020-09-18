// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal static class PropertyPageDataProducer
    {
        public static IEntityValue CreatePropertyPageValue(IEntityValue entity, IPropertyPageQueryCache context, Rule rule, IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(rule, nameof(rule));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.PropertyPageName, rule.Name)
                });

            return CreatePropertyPageValue(entity.EntityRuntime, identity, context, rule, requestedProperties);
        }

        public static IEntityValue CreatePropertyPageValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, IPropertyPageQueryCache context, Rule rule, IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(rule, nameof(rule));
            var newPropertyPage = new PropertyPageValue(runtimeModel, id, new PropertyPagePropertiesAvailableStatus());

            if (requestedProperties.Name)
            {
                newPropertyPage.Name = rule.Name;
            }

            if (requestedProperties.DisplayName)
            {
                newPropertyPage.DisplayName = rule.DisplayName;
            }

            if (requestedProperties.Order)
            {
                newPropertyPage.Order = rule.Order;
            }

            if (requestedProperties.Kind)
            {
                newPropertyPage.Kind = rule.PageTemplate;
            }

            ((IEntityValueFromProvider)newPropertyPage).ProviderState = (context, rule);

            return newPropertyPage;
        }

        public static async Task<IEntityValue?> CreatePropertyPageValueAsync(
            IEntityRuntimeModel runtimeModel,
            EntityIdentity id,
            IProjectService2 projectService,
            string projectPath,
            string propertyPageName,
            IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            if (projectService.GetLoadedProject(projectPath) is UnconfiguredProject project
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && projectCatalog.GetSchema(propertyPageName) is Rule rule
                && !rule.PropertyPagesHidden)
            {
                var propertyPageQueryCache = new PropertyPageQueryCache(project);
                IEntityValue propertyPageValue = CreatePropertyPageValue(runtimeModel, id, propertyPageQueryCache, rule, requestedProperties);
                return propertyPageValue;
            }

            return null;
        }

        public static async Task<IEnumerable<IEntityValue>> CreatePropertyPageValuesAsync(
            IEntityValue parent,
            UnconfiguredProject project,
            IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
            {
                return createPropertyPageValuesAsync();
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createPropertyPageValuesAsync()
            {
                var propertyPageQueryContext = new PropertyPageQueryCache(project);
                foreach (string schemaName in projectCatalog.GetProjectLevelPropertyPagesSchemas())
                {
                    if (projectCatalog.GetSchema(schemaName) is Rule rule
                        && !rule.PropertyPagesHidden)
                    {
                        IEntityValue propertyPageValue = CreatePropertyPageValue(parent, propertyPageQueryContext, rule, requestedProperties);
                        yield return propertyPageValue;
                    }
                }
            }
        }
    }
}
