// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class UIPropertyDataProducerTests
    {
        [Fact]
        public void WhenCreatingFromAParentAndProperty_ThePropertyNameIsTheEntityId()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus();

            var context = IQueryExecutionContextFactory.Create();
            var parentEntity = IEntityWithIdFactory.Create(key: "parent", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty { Name = "MyProperty" };
            var order = 42;
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, parentEntity, cache, QueryProjectPropertiesContext.ProjectFile, property, order, properties);

            Assert.Equal(expected: "MyProperty", actual: result.Id[ProjectModelIdentityKeys.UIPropertyName]);
        }

        [Fact]
        public void WhenPropertiesAreRequested_PropertyValuesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeAllProperties: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Name = "A",
                DisplayName = "Page A",
                Description = "This is the description for Page A",
                HelpUrl = "https://mypage",
                Category = "general",
                Visible = false,
                DataSource = new DataSource { HasConfigurationCondition = false }
            };
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "A", actual: result.Name);
            Assert.Equal(expected: "Page A", actual: result.DisplayName);
            Assert.Equal(expected: "This is the description for Page A", actual: result.Description);
            Assert.True(result.ConfigurationIndependent);
            Assert.Equal(expected: "general", actual: result.CategoryName);
            Assert.False(result.IsVisible);
            Assert.Equal(expected: 42, actual: result.Order);
            Assert.Equal(expected: "string", actual: result.Type);
        }

        [Fact]
        public void WhenTheEntityIsCreated_TheProviderStateIsTheExpectedType()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus();

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Name = "A"
            };
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.Add(property);
            rule.EndInit();

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.IsType<PropertyProviderState>(((IEntityValueFromProvider)result).ProviderState);
        }

        [Fact]
        public void WhenCreatingPropertiesFromARule_OneEntityIsCreatedPerProperty()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeAllProperties: true);

            var context = IQueryExecutionContextFactory.Create();
            var parentEntity = IEntityWithIdFactory.Create(key: "Parent", value: "ParentRule");
            var cache = IProjectStateFactory.Create();
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.AddRange(new[]
            {
                new TestProperty { Name = "Alpha" },
                new TestProperty { Name = "Beta" },
                new TestProperty { Name = "Gamma" },
            });
            rule.EndInit();

            var result = UIPropertyDataProducer.CreateUIPropertyValues(context, parentEntity, cache, QueryProjectPropertiesContext.ProjectFile, rule, properties);

            Assert.Collection(result, new Action<IEntityValue>[]
            {
                entity => { assertEqual(entity, expectedName: "Alpha"); },
                entity => { assertEqual(entity, expectedName: "Beta"); },
                entity => { assertEqual(entity, expectedName: "Gamma"); }
            });

            static void assertEqual(IEntityValue entity, string expectedName)
            {
                var propertyEntity = (UIPropertySnapshot)entity;
                Assert.Equal(expectedName, propertyEntity.Name);
            }
        }

        [Fact]
        public void WhenAPropertyHasNoSearchTerms_AnEmptyStringIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new List<NameValuePair>()
            };

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "", actual: result.SearchTerms);
        }

        [Fact]
        public void WhenAPropertyHasAnEmptyStringOfSearchTerms_AnEmptyStringIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "SearchTerms", Value = "" }
                }
            };

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "", actual: result.SearchTerms);
        }

        [Fact]
        public void WhenAPropertyHasSearchTerms_ThenTheSearchTermsAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeSearchTerms: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "SearchTerms", Value = "Alpha;Beta;Gamma" }
                }
            };

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "Alpha;Beta;Gamma", actual: result.SearchTerms);
        }

        [Fact]
        public void WhenAPropertyHasNoDependencies_AnEmptyStringIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new List<NameValuePair>()
            };
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "", actual: result.DependsOn);
        }

        [Fact]
        public void WhenAPropertyHasAnEmptyStringOfDependencies_AnEmptyStringIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "DependsOn", Value = "" }
                }
            };
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "", actual: result.DependsOn);
        }

        [Fact]
        public void WhenAPropertyHasDependencies_TheDependenciesAreReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeDependsOn: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "DependsOn", Value = "Alpha;Beta;Gamma" }
                }
            };
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "Alpha;Beta;Gamma", actual: result.DependsOn);
        }

        [Fact]
        public void WhenAPropertyHasNoVisibilityCondition_AnEmptyStringIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeVisibilityCondition: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();

            var property = new TestProperty
            {
                Metadata = new List<NameValuePair>()
            };
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: string.Empty, actual: result.VisibilityCondition);
        }

        

        [Fact]
        public void WhenAPropertyHasAVisibilityCondition_ItIsReturned()
        {
            var properties = PropertiesAvailableStatusFactory.CreateUIPropertyPropertiesAvailableStatus(includeVisibilityCondition: true);

            var context = IQueryExecutionContextFactory.Create();
            var id = new EntityIdentity(key: "PropertyName", value: "A");
            var cache = IProjectStateFactory.Create();
            var property = new TestProperty
            {
                Metadata = new()
                {
                    new() { Name = "VisibilityCondition", Value = "true or false"}
                }
            };
            InitializeFakeRuleForProperty(property);

            var result = (UIPropertySnapshot)UIPropertyDataProducer.CreateUIPropertyValue(context, id, cache, QueryProjectPropertiesContext.ProjectFile, property, order: 42, requestedProperties: properties);

            Assert.Equal(expected: "true or false", actual: result.VisibilityCondition);
        }

        /// <remarks>
        /// The only way to set the <see cref="BaseProperty.ContainingRule" /> for a property
        /// is to actually create a rule, add the property to the rule, and go through the
        /// initialization for the rule.
        /// </remarks>
        private static void InitializeFakeRuleForProperty(TestProperty property)
        {
            var rule = new Rule();
            rule.BeginInit();
            rule.Properties.Add(property);
            rule.EndInit();
        }

        private class TestProperty : BaseProperty
        {
        }
    }
}
