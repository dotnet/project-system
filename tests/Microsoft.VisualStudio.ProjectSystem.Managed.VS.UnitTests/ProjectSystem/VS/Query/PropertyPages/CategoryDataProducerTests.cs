// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class CategoryDataProducerTests
    {
        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(includeAllProperties: true);

            var context = IQueryExecutionContextFactory.Create();
            var parentEntity = IEntityWithIdFactory.Create(key: "Parent", value: "KeyValue");
            var rule = new Rule();
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "CategoryName" };
            var order = 42;

            var result = (CategorySnapshot)CategoryDataProducer.CreateCategoryValue(context, parentEntity, rule, category, order, properties);

            Assert.Equal(expected: "CategoryDisplayName", actual: result.DisplayName);
            Assert.Equal(expected: "CategoryName", actual: result.Name);
            Assert.Equal(expected: 42, actual: result.Order);
        }

        [Fact]
        public void WhenCategoryValueCreated_TheCategoryIsTheProviderState()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(includeAllProperties: true);

            var context = IQueryExecutionContextFactory.Create();
            var parentEntity = IEntityWithIdFactory.Create(key: "Parent", value: "KeyValue");
            var rule = new Rule();
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "CategoryName" };
            var order = 42;

            var result = (CategorySnapshot)CategoryDataProducer.CreateCategoryValue(context, parentEntity, rule, category, order, properties);

            Assert.Equal(expected: category, actual: ((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingACategory_TheIdIsTheCategoryName()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(includeAllProperties: true);

            var context = IQueryExecutionContextFactory.Create();
            var parentEntity = IEntityWithIdFactory.Create(key: "Parent", value: "KeyValue");
            var rule = new Rule();
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "MyCategoryName" };
            var order = 42;

            var result = (CategorySnapshot)CategoryDataProducer.CreateCategoryValue(context, parentEntity, rule, category, order, properties);

            Assert.True(result.Id.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string? name));
            Assert.Equal(expected: "MyCategoryName", actual: name);
        }

        [Fact]
        public void WhenCreatingCategoriesFromARule_OneEntityIsCreatedPerCategory()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(includeAllProperties: true);

            var context = IQueryExecutionContextFactory.Create();
            var parentEntity = IEntityWithIdFactory.Create(key: "A", value: "B");
            var rule = new Rule
            {
                Categories =
                {
                    new()
                    {
                        Name = "Alpha",
                        DisplayName = "First category"

                    },
                    new()
                    {
                        Name = "Beta",
                        DisplayName = "Second category"
                    }
                }
            };

            var result = CategoryDataProducer.CreateCategoryValues(context, parentEntity, rule, properties);

            Assert.Collection(result, new Action<IEntityValue>[]
            {
                entity => { assertEqual(entity, expectedName: "Alpha", expectedDisplayName: "First category"); },
                entity => { assertEqual(entity, expectedName: "Beta", expectedDisplayName: "Second category"); }
            });

            static void assertEqual(IEntityValue entity, string expectedName, string expectedDisplayName)
            {
                var categoryEntity = (CategorySnapshot)entity;
                Assert.Equal(expectedName, categoryEntity.Name);
                Assert.Equal(expectedDisplayName, categoryEntity.DisplayName);
            }
        }
    }
}
