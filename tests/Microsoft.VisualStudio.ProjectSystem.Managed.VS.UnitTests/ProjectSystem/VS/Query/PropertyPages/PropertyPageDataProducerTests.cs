// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class PropertyPageDataProducerTests
    {
        [Fact]
        public void WhenPropertyValuesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreatePropertyPagePropertiesAvailableStatus();

            var propertyPage = (PropertyPageValue)PropertyPageDataProducer.CreatePropertyPageValue(
                IEntityRuntimeModelFactory.Create(),
                new EntityIdentity(key: "A", value: "B"),
                IPropertyPageQueryCacheFactory.Create(),
                new Rule { Name = "MyRule", DisplayName = "My Rule Display Name", Order = 42, PageTemplate = "generic" },
                properties);

            Assert.Equal(expected: "MyRule", actual: propertyPage.Name);
            Assert.Equal(expected: "My Rule Display Name", actual: propertyPage.DisplayName);
            Assert.Equal(expected: 42, actual: propertyPage.Order);
            Assert.Equal(expected: "generic", actual: propertyPage.Kind);
        }

        [Fact]
        public void WhenCreatingAModel_ProviderStateConsistsOfCacheAndRule()
        {
            var properties = PropertiesAvailableStatusFactory.CreatePropertyPagePropertiesAvailableStatus();

            var propertyPage = (IEntityValueFromProvider)PropertyPageDataProducer.CreatePropertyPageValue(
                IEntityRuntimeModelFactory.Create(),
                new EntityIdentity(key: "A", value: "B"),
                IPropertyPageQueryCacheFactory.Create(),
                new Rule { Name = "MyRule", DisplayName = "My Rule Display Name", Order = 42, PageTemplate = "generic" },
                properties);

            Assert.IsType<(IPropertyPageQueryCache, Rule)>(propertyPage.ProviderState);
        }

        [Fact]
        public void WhenCreatingFromAParentAndRule_TheRuleNameIsTheEntityId()
        {
            var properties = PropertiesAvailableStatusFactory.CreatePropertyPagePropertiesAvailableStatus();

            var propertyPage = (PropertyPageValue)PropertyPageDataProducer.CreatePropertyPageValue(
                IEntityWithIdFactory.Create(key: "ParentKey", value: "ParentValue"),
                IPropertyPageQueryCacheFactory.Create(),
                new Rule { Name = "MyRule", DisplayName = "My Rule Display Name", Order = 42, PageTemplate = "generic" },
                properties);

            Assert.Equal(expected: "MyRule", actual: propertyPage.Id[ProjectModelIdentityKeys.PropertyPageName]);
        }
    }
}
