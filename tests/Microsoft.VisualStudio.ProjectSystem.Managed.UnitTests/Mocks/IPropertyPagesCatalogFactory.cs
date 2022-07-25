// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

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
                                properties: category.Select(property => CreateProperty(property.PropertyName, property.Value, property.SetValues))));
            }

            return catalog;
        }

        private static IProperty CreateProperty(string name, object value, List<object>? setValues = null)
        {
            var property = new Mock<IProperty>();
            property.SetupGet(o => o.Name)
                    .Returns(name);

            property.Setup(o => o.GetValueAsync())
                    .ReturnsAsync(value);

            property.As<IEvaluatedProperty>().Setup(p => p.GetEvaluatedValueAtEndAsync()).ReturnsAsync(value.ToString());
            property.As<IEvaluatedProperty>().Setup(p => p.GetEvaluatedValueAsync()).ReturnsAsync(value.ToString());

            if (setValues is not null)
            {
                property.Setup(p => p.SetValueAsync(It.IsAny<object>()))
                        .Callback<object>(setValues.Add)
                        .ReturnsAsync(() => { });
            }

            return property.Object;
        }
    }
}
