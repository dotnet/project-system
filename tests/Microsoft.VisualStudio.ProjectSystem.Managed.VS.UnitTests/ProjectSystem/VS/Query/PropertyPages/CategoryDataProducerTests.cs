// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class CategoryDataProducerTests
    {
        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(
                includeDisplayName: true,
                includeName: true,
                includeOrder: true);
            var producer = new TestCategoryDataProducer(properties);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "CategoryName" };
            var order = 42;

            var result = (CategoryValue)producer.TestCreateMetadataValue(entityRuntime, id, category, order);

            Assert.Equal(expected: "CategoryDisplayName", actual: result.DisplayName);
            Assert.Equal(expected: "CategoryName", actual: result.Name);
            Assert.Equal(expected: 42, actual: result.Order);
        }

        [Fact]
        public void WhenCategoryValueCreated_TheCategoryIsTheProviderState()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(
                includeDisplayName: true,
                includeName: true,
                includeOrder: true);
            var producer = new TestCategoryDataProducer(properties);

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "CategoryName" };
            var order = 42;

            var result = (CategoryValue)producer.TestCreateMetadataValue(entityRuntime, id, category, order);

            Assert.Equal(expected: category, actual: ((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingACategory_TheIdIsTheCategoryName()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(
                includeDisplayName: true,
                includeName: true,
                includeOrder: true);
            var producer = new TestCategoryDataProducer(properties);

            var entity = IEntityWithIdFactory.Create(key: "A", value: "B");
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "MyCategoryName" };
            var order = 42;

            var result = (CategoryValue)producer.TestCreateCategoryValue(entity, category, order);

            Assert.True(result.Id.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string name));
            Assert.Equal(expected: "MyCategoryName", actual: name);
        }

        private class TestCategoryDataProducer : CategoryDataProducer
        {
            public TestCategoryDataProducer(ICategoryPropertiesAvailableStatus properties)
                : base(properties)
            {
            }
            
            public IEntityValue TestCreateCategoryValue(IEntityValue entity, Category category, int order)
            {
                return CreateCategoryValue(entity, category, order);
            }

            public IEntityValue TestCreateMetadataValue(IEntityRuntimeModel runtimeModel, EntityIdentity id, Category category, int order)
            {
                return CreateCategoryValue(runtimeModel, id, category, order);
            }
        }
    }
}
