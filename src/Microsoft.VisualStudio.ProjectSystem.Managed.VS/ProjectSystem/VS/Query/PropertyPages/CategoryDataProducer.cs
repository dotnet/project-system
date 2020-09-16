// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles the creation of <see cref="ICategory"/> instances and populating the requested members.
    /// </summary>
    internal abstract class CategoryDataProducer : QueryDataProducerBase<IEntityValue>
    {
        public CategoryDataProducer(ICategoryPropertiesAvailableStatus properties)
        {
            Requires.NotNull(properties, nameof(properties));
            Properties = properties;
        }

        protected ICategoryPropertiesAvailableStatus Properties { get; }

        protected IEntityValue CreateCategoryValue(IEntityValue entity, Category category, int order)
        {
            Requires.NotNull(entity, nameof(entity));
            Requires.NotNull(category, nameof(category));

            var identity = new EntityIdentity(
                ((IEntityWithId)entity).Id,
                new KeyValuePair<string, string>[]
                {
                    new(ProjectModelIdentityKeys.CategoryName, category.Name)
                });

            return CreateCategoryValue(entity.EntityRuntime, identity, category, order);
        }

        protected IEntityValue CreateCategoryValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, Category category, int order)
        {
            Requires.NotNull(category, nameof(category));
            var newCategory = new CategoryValue(runtimeModel, id, new CategoryPropertiesAvailableStatus());

            if (Properties.DisplayName)
            {
                newCategory.DisplayName = category.DisplayName;
            }

            if (Properties.Name)
            {
                newCategory.Name = category.Name;
            }

            if (Properties.Order)
            {
                newCategory.Order = order;
            }

            ((IEntityValueFromProvider)newCategory).ProviderState = category;

            return newCategory;
        }
    }
}
