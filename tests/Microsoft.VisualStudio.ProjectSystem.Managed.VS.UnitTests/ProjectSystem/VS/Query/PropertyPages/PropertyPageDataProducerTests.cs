﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
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
            var properties = PropertiesAvailableStatusFactory.CreatePropertyPagePropertiesAvailableStatus(includeAllProperties: true);

            var propertyPage = (PropertyPageValue)PropertyPageDataProducer.CreatePropertyPageValue(
                IEntityWithIdFactory.Create(key: "A", value: "B"),
                IPropertyPageQueryCacheFactory.Create(),
                QueryProjectPropertiesContext.ProjectFile,
                new Rule { Name = "MyRule", DisplayName = "My Rule Display Name", Order = 42, PageTemplate = "generic" },
                debugChildRules: new List<Rule>(),
                requestedProperties: properties);

            Assert.Equal(expected: "MyRule", actual: propertyPage.Name);
            Assert.Equal(expected: "My Rule Display Name", actual: propertyPage.DisplayName);
            Assert.Equal(expected: 42, actual: propertyPage.Order);
            Assert.Equal(expected: "generic", actual: propertyPage.Kind);
        }

        [Fact]
        public void WhenCreatingAModel_ProviderStateIsTheCorrectType()
        {
            var properties = PropertiesAvailableStatusFactory.CreatePropertyPagePropertiesAvailableStatus(includeAllProperties: true);

            var propertyPage = (IEntityValueFromProvider)PropertyPageDataProducer.CreatePropertyPageValue(
                IEntityWithIdFactory.Create(key: "A", value: "B"),
                IPropertyPageQueryCacheFactory.Create(),
                QueryProjectPropertiesContext.ProjectFile,
                new Rule { Name = "MyRule", DisplayName = "My Rule Display Name", Order = 42, PageTemplate = "generic" },
                debugChildRules: new List<Rule>(),
                requestedProperties: properties);

            Assert.IsType<PropertyPageProviderState>(propertyPage.ProviderState);
        }

        [Fact]
        public void WhenCreatingFromAParentAndRule_TheRuleNameIsTheEntityId()
        {
            var properties = PropertiesAvailableStatusFactory.CreatePropertyPagePropertiesAvailableStatus(includeAllProperties: true);

            var propertyPage = (PropertyPageValue)PropertyPageDataProducer.CreatePropertyPageValue(
                IEntityWithIdFactory.Create(key: "ParentKey", value: "ParentValue"),
                IPropertyPageQueryCacheFactory.Create(),
                QueryProjectPropertiesContext.ProjectFile,
                new Rule { Name = "MyRule", DisplayName = "My Rule Display Name", Order = 42, PageTemplate = "generic" },
                debugChildRules: new List<Rule>(),
                requestedProperties: properties);

            Assert.Equal(expected: "MyRule", actual: propertyPage.Id[ProjectModelIdentityKeys.PropertyPageName]);
        }
    }
}
