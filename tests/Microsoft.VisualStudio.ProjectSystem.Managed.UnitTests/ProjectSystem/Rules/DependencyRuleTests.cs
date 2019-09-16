// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class DependencyRuleTests : XamlRuleTestBase
    {
        [Theory]
        [MemberData(nameof(GetResolvedDependenciesRules))]
        public void VisibleEditableResolvedDependenciesMustHaveDataSource(string ruleName, string fullPath)
        {
            // Resolved rules get their data from design time targets. Any editable properties need a
            // property-level data source that specifies the storage for that property as the project file
            // so that changes made in the properties pane are reflected in the project file and vice versa.

            XElement rule = LoadXamlRule(fullPath);

            var itemType = rule
                .Element(XName.Get("Rule.DataSource", MSBuildNamespace))
                ?.Element(XName.Get("DataSource", MSBuildNamespace))
                ?.Attribute("ItemType")?.Value;

            Assert.NotNull(itemType);

            foreach (var property in GetProperties(rule))
            {
                // Properties are visible and non-readonly by default
                string visibleValue = property.Attribute("Visible")?.Value ?? "true";
                string readOnlyValue = property.Attribute("ReadOnly")?.Value ?? "false";

                Assert.True(bool.TryParse(visibleValue, out bool visible));
                Assert.True(bool.TryParse(readOnlyValue, out bool readOnly));

                if (!visible || readOnly)
                {
                    continue;
                }

                var dataSourceElementName = $"{property.Name.LocalName}.DataSource";

                var dataSource = property
                    .Element(XName.Get(dataSourceElementName, MSBuildNamespace))
                    ?.Element(XName.Get("DataSource", MSBuildNamespace));

                if (dataSource == null)
                {
                    throw new Xunit.Sdk.XunitException($"Resolved dependency rule {ruleName} has visible, non-readonly property {property.Attribute("Name")} with no {dataSourceElementName} value.");
                }

                Assert.Equal("False",        dataSource.Attribute("HasConfigurationCondition")?.Value, StringComparer.OrdinalIgnoreCase);
                Assert.Equal("ProjectFile",  dataSource.Attribute("Persistence")?.Value,               StringComparer.Ordinal);
                Assert.Equal("AfterContext", dataSource.Attribute("SourceOfDefaultValue")?.Value,      StringComparer.Ordinal);
                Assert.Equal(itemType,       dataSource.Attribute("ItemType")?.Value,                  StringComparer.Ordinal);
            }
        }

        [Theory]
        [MemberData(nameof(GetResolvedDependenciesRules))]
        public void ResolvedDependenciesRulesMustHaveOriginalItemSpecProperty(string ruleName, string fullPath)
        {
            // All resolved dependency items have a corresponding 'original' item spec, which contains
            // the value of the item produced by evaluation.

            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:StringProperty[@Name=""OriginalItemSpec""]", namespaceManager);

            Assert.NotNull(property);
            Assert.Equal(3, property.Attributes().Count());
            Assert.Equal("OriginalItemSpec", property.Attribute("Name")?.Value, StringComparer.Ordinal);
            Assert.Equal("False", property.Attribute("Visible")?.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("True", property.Attribute("ReadOnly")?.Value, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        public void DependenciesRulesMustHaveVisibleProperty(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:BoolProperty[@Name=""Visible""]", namespaceManager);

            Assert.NotNull(property);
            Assert.Equal(3, property.Attributes().Count());
            Assert.Equal("Visible", property.Attribute("Name")?.Value, StringComparer.Ordinal);
            Assert.Equal("False", property.Attribute("Visible")?.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("True", property.Attribute("ReadOnly")?.Value, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        public void DependenciesRulesMustHaveIsImplicitlyDefinedProperty(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:StringProperty[@Name=""IsImplicitlyDefined""]", namespaceManager);

            Assert.NotNull(property);
            Assert.Equal(3, property.Attributes().Count());
            Assert.Equal("IsImplicitlyDefined", property.Attribute("Name")?.Value, StringComparer.Ordinal);
            Assert.Equal("False", property.Attribute("Visible")?.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("True", property.Attribute("ReadOnly")?.Value, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetUnresolvedDependenciesRules))]
        public void UnresolvedDependenciesRulesMustNotHaveOriginalItemSpec(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:StringProperty[@Name=""OriginalItemSpec""]", namespaceManager);

            Assert.Null(property);
        }

        public static IEnumerable<object[]> GetUnresolvedDependenciesRules()
        {
            return Project(GetRules("Dependencies")
                .Where(fileName => fileName.IndexOf("Resolved", StringComparisons.Paths) == -1));
        }

        public static IEnumerable<object[]> GetResolvedDependenciesRules()
        {
            return Project(GetRules("Dependencies")
                .Where(fileName => fileName.IndexOf("Resolved", StringComparisons.Paths) != -1));
        }

        public static IEnumerable<object[]> GetDependenciesRules()
        {
            return Project(GetRules("Dependencies"));
        }
    }
}
