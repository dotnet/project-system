// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
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
    /// Handles the creation of <see cref="ProjectSystem.Query.ProjectModel.ICategory"/> instances and populating the requested members.
    /// </summary>
    internal static class CategoryDataProducer
    {
        public static IEntityValue CreateCategoryValue(IEntityValue parent, Rule rule, Category category, int order, ICategoryPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(category, nameof(category));

            string categoryName = DebugUtilities.GetDebugCategoryNameOrNull(rule, category) ?? category.Name;

            var identity = new EntityIdentity(
                ((IEntityWithId)parent).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.CategoryName, categoryName)
                });

            return CreateCategoryValue(parent.EntityRuntime, identity, rule, category, order, requestedProperties);
        }

        public static IEntityValue CreateCategoryValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, Rule rule, Category category, int order, ICategoryPropertiesAvailableStatus requestedProperties)
        {
            Requires.NotNull(category, nameof(category));
            var newCategory = new CategoryValue(runtimeModel, id, new CategoryPropertiesAvailableStatus());

            if (requestedProperties.DisplayName)
            {
                newCategory.DisplayName = category.DisplayName;
            }

            if (requestedProperties.Name)
            {
                newCategory.Name = DebugUtilities.GetDebugCategoryNameOrNull(rule, category) ?? category.Name;
            }

            if (requestedProperties.Order)
            {
                newCategory.Order = order;
            }

            ((IEntityValueFromProvider)newCategory).ProviderState = category;

            return newCategory;
        }

        public static IEnumerable<IEntityValue> CreateCategoryValues(IEntityValue parent, Rule rule, List<Rule> debugChildRules, ICategoryPropertiesAvailableStatus requestedProperties)
        {
            int index = 0;
            foreach (Category category in rule.EvaluatedCategories)
            {
                IEntityValue categoryValue = CreateCategoryValue(parent, rule, category, index, requestedProperties);
                yield return categoryValue;
                index++;
            }

            foreach (Rule childRule in debugChildRules)
            {
                foreach (Category category in childRule.EvaluatedCategories)
                {
                    IEntityValue categoryValue = CreateCategoryValue(parent, childRule, category, index, requestedProperties);
                    yield return categoryValue;
                    index++;
                }
            }
        }

        public static async Task<IEntityValue?> CreateCategoryValueAsync(
            IEntityRuntimeModel runtimeModel,
            EntityIdentity id,
            IProjectService2 projectService,
            string projectPath,
            string propertyPageName,
            string categoryName,
            ICategoryPropertiesAvailableStatus requestedProperties)
        {
            (propertyPageName, categoryName) = DebugUtilities.ConvertDebugPageAndCategoryToRealPageAndCategory(propertyPageName, categoryName);

            if (projectService.GetLoadedProject(projectPath) is UnconfiguredProject project
                && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                && projectCatalog.GetSchema(propertyPageName) is Rule rule)
            {
                // We need the category's index in order to populate the "Order" field of the query model.
                // This requires that we do a linear traversal of the categories, even though we only care
                // about one.
                //
                // TODO: if the "Order" property hasn't been requested, we can skip the linear traversal in
                // favor of just looking it up by name.
                foreach ((int index, Category category) in rule.EvaluatedCategories.WithIndices())
                {
                    if (StringComparers.CategoryNames.Equals(category.Name, categoryName))
                    {
                        IEntityValue categoryValue = CreateCategoryValue(runtimeModel, id, rule, category, index, requestedProperties);
                        return categoryValue;
                    }
                }
            }

            return null;
        }
    }
}
