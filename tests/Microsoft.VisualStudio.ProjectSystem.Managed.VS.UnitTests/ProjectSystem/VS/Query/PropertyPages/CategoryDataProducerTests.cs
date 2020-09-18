// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
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

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "CategoryName" };
            var order = 42;

            var result = (CategoryValue)CategoryDataProducer.CreateCategoryValue(entityRuntime, id, category, order, properties);

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

            var entityRuntime = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "A", value: "B");
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "CategoryName" };
            var order = 42;

            var result = (CategoryValue)CategoryDataProducer.CreateCategoryValue(entityRuntime, id, category, order, properties);

            Assert.Equal(expected: category, actual: ((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingACategory_TheIdIsTheCategoryName()
        {
            var properties = PropertiesAvailableStatusFactory.CreateCategoryPropertiesAvailableStatus(
                includeDisplayName: true,
                includeName: true,
                includeOrder: true);

            var parentEntity = IEntityWithIdFactory.Create(key: "A", value: "B");
            var category = new Category { DisplayName = "CategoryDisplayName", Name = "MyCategoryName" };
            var order = 42;

            var result = (CategoryValue)CategoryDataProducer.CreateCategoryValue(parentEntity, category, order, properties);

            Assert.True(result.Id.TryGetValue(ProjectModelIdentityKeys.CategoryName, out string name));
            Assert.Equal(expected: "MyCategoryName", actual: name);
        }
    }
}
