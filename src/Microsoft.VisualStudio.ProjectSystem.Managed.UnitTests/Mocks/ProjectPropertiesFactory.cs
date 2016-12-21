// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectPropertiesFactory
    {
        public static ProjectProperties CreateEmpty()
        {
            return Create(IUnconfiguredProjectFactory.Create());
        }

        public static ProjectProperties Create(UnconfiguredProject unconfiguredProject, params PropertyPageData[] data)
        {
            var catalog = CreateCatalog(CreateCatalogLookup(data));
            IPropertyPagesCatalogProvider propertyPagesCatalogProvider = CreateCatalogProvider(
                    new Dictionary<string, IPropertyPagesCatalog>
                    {
                        { "Project", catalog }
                    },
                    catalog
                );

            IAdditionalRuleDefinitionsService ruleService = Mock.Of<IAdditionalRuleDefinitionsService>();

            IConfiguredProjectServices configuredProjectServices = Mock.Of<IConfiguredProjectServices>(o =>
                o.PropertyPagesCatalog == propertyPagesCatalogProvider &&
                o.AdditionalRuleDefinitions == ruleService);            

            var cfg = new StandardProjectConfiguration("Debug|" + "AnyCPU", Empty.PropertiesMap.SetItem("Configuration", "Debug").SetItem("Platform", "AnyCPU"));
            ConfiguredProject configuredProject = Mock.Of<ConfiguredProject>(o =>
                o.UnconfiguredProject == unconfiguredProject &&
                o.Services == configuredProjectServices &&
                o.ProjectConfiguration == cfg);

            return new ProjectProperties(configuredProject);
        }

        private static Dictionary<string, IRule> CreateCatalogLookup(PropertyPageData[] data)
        {
            Dictionary<string, IRule> catalog = new Dictionary<string, IRule>();

            foreach (var category in data.GroupBy(p => p.Category))
            {
                catalog.Add(category.Key, 
                            CreateRule(
                                    category.Select(property => CreateProperty(property.PropertyName, property.Value))));
            }

            return catalog;
        }

        private static IPropertyPagesCatalogProvider CreateCatalogProvider(Dictionary<string, IPropertyPagesCatalog> catalogsByContext, IPropertyPagesCatalog catalog)
        {
            var catalogProvider = new Mock<IPropertyPagesCatalogProvider>();
            catalogProvider
                .Setup(o => o.GetCatalogsAsync(CancellationToken.None))
                .ReturnsAsync(catalogsByContext.ToImmutableDictionary());

            catalogProvider
                .Setup(o => o.GetMemoryOnlyCatalog(It.IsAny<string>()))
                .Returns(catalog);

            return catalogProvider.Object;
        }

        private static IPropertyPagesCatalog CreateCatalog(Dictionary<string, IRule> rulesBySchemaName)
        {
            var catalog = new Mock<IPropertyPagesCatalog>();
            catalog.Setup(o => o.BindToContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string schemaName, string file, string itemType, string itemName) => {

                       rulesBySchemaName.TryGetValue(schemaName, out IRule rule);
                       return rule;
                });

            return catalog.Object;
        }

        private static IRule CreateRule(IEnumerable<IProperty> properties)
        {
            var rule = new Mock<IRule>();
            rule.Setup(o => o.GetProperty(It.IsAny<string>()))
                .Returns((string propertyName) => {

                    return properties.FirstOrDefault(p => p.Name == propertyName);
                });

            return rule.Object;
        }

        private static IProperty CreateProperty(string name, object value)
        {
            var property = new Mock<IProperty>();
            property.SetupGet(o => o.Name)
                    .Returns(name);

            property.Setup(o => o.GetValueAsync())
                    .ReturnsAsync(value);

            property.As<IEvaluatedProperty>().Setup(p => p.GetEvaluatedValueAtEndAsync()).ReturnsAsync(value.ToString());
            property.As<IEvaluatedProperty>().Setup(p => p.GetEvaluatedValueAsync()).ReturnsAsync(value.ToString());

            return property.Object;
        }

    }
}
