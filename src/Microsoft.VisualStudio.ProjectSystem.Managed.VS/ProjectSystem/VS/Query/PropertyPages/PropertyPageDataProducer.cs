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
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="IPropertyPage"/> instances and populating the requested members.
    /// </summary>
    internal static class PropertyPageDataProducer
    {
        public static IEntityValue CreatePropertyPageValue(IQueryExecutionContext executionContext, IEntityValue parent, IPropertyPageQueryCache cache, QueryProjectPropertiesContext context, Rule rule, IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(rule, nameof(rule));

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                createKeys());

            return CreatePropertyPageValue(executionContext, identity, cache, context, rule, requestedProperties);

            IEnumerable<KeyValuePair<string, string>> createKeys()
            {
                yield return new(ProjectModelIdentityKeys.PropertyPageName, rule.Name);

                if (context.ItemType is not null)
                {
                    yield return new(ProjectModelIdentityKeys.SourceItemType, context.ItemType);
                }

                if (context.ItemName is not null)
                {
                    yield return new(ProjectModelIdentityKeys.SourceItemName, context.ItemName);
                }
            }
        }

        public static IEntityValue CreatePropertyPageValue(IQueryExecutionContext executionContext, EntityIdentity id, IPropertyPageQueryCache cache, QueryProjectPropertiesContext context, Rule rule, IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(rule, nameof(rule));
            var newPropertyPage = new PropertyPageValue(executionContext.EntityRuntime, id, new PropertyPagePropertiesAvailableStatus());

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

            ((IEntityValueFromProvider)newPropertyPage).ProviderState = new ContextAndRuleProviderState(cache, context, rule);

            return newPropertyPage;
        }

        public static async Task<IEntityValue?> CreatePropertyPageValueAsync(
            IQueryExecutionContext executionContext,
            EntityIdentity id,
            IProjectService2 projectService,
            IPropertyPageQueryCacheProvider queryCacheProvider,
            QueryProjectPropertiesContext context,
            string propertyPageName,
            IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            if (projectService.GetLoadedProject(context.File) is UnconfiguredProject project)
            {
                project.GetQueryDataVersion(out string versionKey, out long versionNumber);
                executionContext.ReportInputDataVersion(versionKey, versionNumber);

                if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                    && projectCatalog.GetSchema(propertyPageName) is Rule rule
                    && !rule.PropertyPagesHidden)
                {
                    IPropertyPageQueryCache propertyPageQueryCache = queryCacheProvider.CreateCache(project);
                    IEntityValue propertyPageValue = CreatePropertyPageValue(executionContext, id, propertyPageQueryCache, context, rule, requestedProperties);
                    return propertyPageValue;
                }
            }

            return null;
        }

        public static async Task<IEnumerable<IEntityValue>> CreatePropertyPageValuesAsync(
            IQueryExecutionContext executionContext,
            IEntityValue parent,
            UnconfiguredProject project,
            IPropertyPageQueryCacheProvider queryCacheProvider,
            IPropertyPagePropertiesAvailableStatus requestedProperties)
        {
            if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
            {
                return createPropertyPageValuesAsync();
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createPropertyPageValuesAsync()
            {
                IPropertyPageQueryCache propertyPageQueryCache = queryCacheProvider.CreateCache(project);
                QueryProjectPropertiesContext context = new QueryProjectPropertiesContext(isProjectFile: true, project.FullPath, itemType: null, itemName: null);
                foreach (string schemaName in projectCatalog.GetProjectLevelPropertyPagesSchemas())
                {
                    if (projectCatalog.GetSchema(schemaName) is Rule rule
                        && !rule.PropertyPagesHidden)
                    {
                        IEntityValue propertyPageValue = CreatePropertyPageValue(executionContext, parent, propertyPageQueryCache, context, rule, requestedProperties: requestedProperties);
                        yield return propertyPageValue;
                    }
                }
            }
        }
    }
}
