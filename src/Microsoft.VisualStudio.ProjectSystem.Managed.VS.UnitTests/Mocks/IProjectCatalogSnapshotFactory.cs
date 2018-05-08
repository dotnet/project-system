// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectCatalogSnapshotFactory
    {
        public static IProjectCatalogSnapshot Create()
        {
            return Mock.Of<IProjectCatalogSnapshot>();
        }

        public static IProjectCatalogSnapshot ImplementRulesWithItemTypes(
                                                    IDictionary<string, string> rulesAndItemTypes,
                                                    MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mockSnapshot = new Mock<IProjectCatalogSnapshot>(behavior);
            var mockPropertyPageCatalog = new Mock<IPropertyPagesCatalog>(behavior);

            foreach (var kvp in rulesAndItemTypes)
            {
                var ruleName = kvp.Key;
                var itemType = kvp.Value;

                var rule = new Rule
                {
                    DataSource = new DataSource { ItemType = itemType }
                };

                mockPropertyPageCatalog.Setup(x => x.GetSchema(ruleName)).Returns(rule);
            }

            mockSnapshot.Setup(x => x.NamedCatalogs).Returns(
                ImmutableStringDictionary<IPropertyPagesCatalog>
                    .EmptyOrdinal
                    .Add(PropertyPageContexts.Project, mockPropertyPageCatalog.Object));

            return mockSnapshot.Object;
        }
    }
}
