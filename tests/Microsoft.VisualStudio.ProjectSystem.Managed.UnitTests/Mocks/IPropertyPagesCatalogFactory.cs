// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPropertyPagesCatalogFactory
    {
        public static IPropertyPagesCatalog Create(Dictionary<string, IRule> rulesBySchemaName)
        {
            var catalog = new Mock<IPropertyPagesCatalog>();
            catalog.Setup(o => o.BindToContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                   .Returns((string schemaName, string file, string itemType, string itemName) =>
                   {
                       rulesBySchemaName.TryGetValue(schemaName, out IRule rule);
                       return rule;
                   });

            catalog.Setup(o => o.GetPropertyPagesSchemas())
                .Returns(() => rulesBySchemaName.Keys);

            catalog.Setup(o => o.GetSchema(It.IsAny<string>()))
                .Returns((string name) => rulesBySchemaName.TryGetValue(name, out IRule rule) ? rule.Schema : null);

            catalog.Setup(o => o.BindToContext(It.IsAny<string>(), It.IsAny<IProjectPropertiesContext>()))
                .Returns((string name, IProjectPropertiesContext context) => rulesBySchemaName.TryGetValue(name, out IRule rule) ? rule : null);

            return catalog.Object;
        }

        public static IPropertyPagesCatalog Create(params PropertyPageData[] data)
        {
            return Create(CreateCatalogLookup(data));
        }

        private static Dictionary<string, IRule> CreateCatalogLookup(PropertyPageData[] data)
        {
            var catalog = new Dictionary<string, IRule>();

            foreach (var category in data.GroupBy(p => p.Category))
            {
                catalog.Add(category.Key,
                            IRuleFactory.Create(
                                properties: category.Select(CreateProperty)));
            }

            return catalog;
        }

        private static IProperty CreateProperty(PropertyPageData data)
        {
            var property = new Mock<IProperty>();
            property.SetupGet(o => o.Name)
                    .Returns(data.PropertyName);

            property.Setup(o => o.GetValueAsync())
                    .ReturnsAsync(data.Value);

            property.As<IEvaluatedProperty>().Setup(p => p.GetEvaluatedValueAtEndAsync()).ReturnsAsync(data.Value.ToString());
            property.As<IEvaluatedProperty>().Setup(p => p.GetEvaluatedValueAsync()).ReturnsAsync(data.Value.ToString());

            if (data.SetValues != null)
            {
                property.Setup(p => p.SetValueAsync(It.IsAny<object>()))
                        .Callback<object>(data.SetValues.Add)
                        .ReturnsAsync(() => { });
            }

            if (data.PropertyType == typeof(IStringProperty))
            {
                property.As<IStringProperty>()
                        .Setup(p => p.GetValueAsStringAsync())
                        .ReturnsAsync(data.Value.ToString());
            } 
            else if (data.PropertyType == typeof(IStringListProperty))
            {
                property.As<IStringListProperty>()
                        .Setup(p => p.GetValueAsStringCollectionAsync())
                        .ReturnsAsync(data.Value.ToString().Split(';').ToList().AsReadOnly());
            }

            return property.Object;
        }
    }
}
