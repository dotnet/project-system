// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class UIPropertyDataProducerTests
    {
        [Fact]
        public void WhenCreatingFromAParentAndProperty_ThePropertyNameIsTheEntityId()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus();

            var parentEntity = IEntityWithIdFactory.Create(key: "parent", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty { Name = "MyProperty" };
            var order = 42;

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(parentEntity, cache, property, order, properties);

            Assert.Equal(expected: "MyProperty", actual: result.Id[ProjectModelIdentityKeys.UIPropertyName]);
        }

        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeAllProperties: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Name = "A",
                DisplayName = "Page A",
                Description = "This is the description for Page A",
                HelpUrl = "https://mypage",
                Category = "general",
                DataSource = new DataSource { HasConfigurationCondition = false }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Equal(expected: "A", actual: result.Name);
            Assert.Equal(expected: "Page A", actual: result.DisplayName);
            Assert.Equal(expected: "This is the description for Page A", actual: result.Description);
            Assert.True(result.ConfigurationIndependent);
            Assert.Equal(expected: "general", actual: result.CategoryName);
            Assert.Equal(expected: 42, actual: result.Order);
            Assert.Equal(expected: "string", actual: result.Type);
        }

        [Fact]
        public void WhenTheEntityIsCreated_TheProviderStateIsTheExpectedType()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus();

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Name = "A"
            };
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.Add(property);
            rule.EndInit();

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.IsType<(IPropertyPageQueryCache, Rule, string)>(((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingPropertiesFromARule_OneEntityIsCreatedPerProperty()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeAllProperties: true);

            var parentEntity = IEntityWithIdFactory.Create(key: "Parent", value: "ParentRule");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.AddRange(new[]
            {
                new TestProperty { Name = "Alpha" },
                new TestProperty { Name = "Beta" },
                new TestProperty { Name = "Gamma" },
            });
            rule.EndInit();

            var result = UIPropertyDataProducer.CreateUIPropertyValues(parentEntity, cache, rule, properties);

            Assert.Collection(result, new Action<IEntityValue>[]
            {
                entity => { assertEqual(entity, expectedName: "Alpha"); },
                entity => { assertEqual(entity, expectedName: "Beta"); },
                entity => { assertEqual(entity, expectedName: "Gamma"); }
            });

            static void assertEqual(IEntityValue entity, string expectedName)
            {
                var propertyEntity = (UIPropertyValue)entity;
                Assert.Equal(expectedName, propertyEntity.Name);
            }
        }

        [Fact]
        public void WhenAPropertyHasNoSearchTerms_AnEmptyListIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new List<NameValuePair>()
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Empty(result.SearchTerms);
        }

        [Fact]
        public void WhenAPropertyHasAnEmptyListOfSearchTerms_AnEmptyListIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "SearchTerms", Value = "" }
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Empty(result.SearchTerms);
        }

        [Fact]
        public void WhenAPropertyHasOneSearchTerm_OneItemIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "SearchTerms", Value = "Alpha" }
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Collection(result.SearchTerms, new Action<string>[]
            {
                searchTerm => Assert.Equal(expected: "Alpha", actual: searchTerm)
            });
        }

        [Fact]
        public void WhenAPropertyHasMultipleSearchTerms_MultipleItemsAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "SearchTerms", Value = "Alpha;Beta;Gamma" }
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Collection(result.SearchTerms, new Action<string>[]
            {
                searchTerm => Assert.Equal(expected: "Alpha", actual: searchTerm),
                searchTerm => Assert.Equal(expected: "Beta", actual: searchTerm),
                searchTerm => Assert.Equal(expected: "Gamma", actual: searchTerm),
            });
        }

        [Fact]
        public void WhenAPropertyHasNoDependencies_AnEmptyListIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new List<NameValuePair>()
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Empty(result.DependsOn);
        }

        [Fact]
        public void WhenAPropertyHasAnEmptyListOfDependencies_AnEmptyListIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "DependsOn", Value = "" }
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Empty(result.DependsOn);
        }

        [Fact]
        public void WhenAPropertyHasOneDependency_OneItemIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "DependsOn", Value = "SomeOtherProperty" }
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Collection(result.DependsOn, new Action<string>[]
            {
                searchTerm => Assert.Equal(expected: "SomeOtherProperty", actual: searchTerm)
            });
        }

        [Fact]
        public void WhenAPropertyHasMultipleDependencies_MultipleItemsAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "DependsOn", Value = "AlphaProperty;BetaProperty;GammaProperty" }
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Collection(result.DependsOn, new Action<string>[]
            {
                searchTerm => Assert.Equal(expected: "AlphaProperty", actual: searchTerm),
                searchTerm => Assert.Equal(expected: "BetaProperty", actual: searchTerm),
                searchTerm => Assert.Equal(expected: "GammaProperty", actual: searchTerm),
            });
        }

        [Fact]
        public void WhenAPropertyHasNoVisibilityCondition_AnEmptyStringIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeVisibilityCondition: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new List<NameValuePair>()
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Equal(expected: string.Empty, actual: result.VisibilityCondition);
        }

        [Fact]
        public void WhenAPropertyHasAVisibilityCondition_ItIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeVisibilityCondition: true);

            var runtimeModel = IEntityRuntimeModelFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IPropertyPageQueryCacheFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "VisibilityCondition", Value = "true or false"}
                }
            };

            var result = (UIPropertyValue)UIPropertyDataProducer.CreateUIPropertyValue(runtimeModel, id, cache, property, order: 42, properties);

            Assert.Equal(expected: "true or false", actual: result.VisibilityCondition);
        }

        private class TestProperty : BaseProperty
        {
        }
    }
}
