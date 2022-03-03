// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectCatalogSnapshotFactory
    {
        public static IProjectCatalogSnapshot Create()
        {
            return Mock.Of<IProjectCatalogSnapshot>();
        }

        public static IProjectCatalogSnapshot CreateWithDefaultMapping(IImmutableList<IItemType> itemTypes)
        {
            var projectCatalogSnapshot = new Mock<IProjectCatalogSnapshot>();

            var ruleNameToRule = new Dictionary<string, Rule>();
            foreach (IItemType itemType in itemTypes)
            {
                ruleNameToRule.Add(itemType.Name, new Rule { DataSource = new DataSource { ItemType = itemType.Name } });
            }

            var propertyPageCatalog = new Mock<IPropertyPagesCatalog>();
            propertyPageCatalog.Setup(o => o.GetSchema(It.IsAny<string>())).Returns<string>(n => ruleNameToRule[n]);

            var namedCatalogs = ImmutableDictionary<string, IPropertyPagesCatalog>.Empty.Add("File", propertyPageCatalog.Object);

            projectCatalogSnapshot.SetupGet(o => o.NamedCatalogs).Returns(namedCatalogs);

            return projectCatalogSnapshot.Object;
        }
    }
}
