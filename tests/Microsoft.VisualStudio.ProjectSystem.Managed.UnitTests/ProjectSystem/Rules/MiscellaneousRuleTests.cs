// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class MiscellaneousRuleTests : XamlRuleTestBase
    {
        [Theory]
        [MemberData(nameof(GetAllDisplayedRules))]
        public void NonVisiblePropertiesShouldntBeLocalized(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (XElement element in rule.Elements())
            {
                var visibleAttribute = element.Attribute("Visible");

                if (visibleAttribute?.Value.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Assert.Null(element.Attribute("DisplayName"));
                    Assert.Null(element.Attribute("Description"));
                    Assert.Null(element.Attribute("Category"));
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDisplayedRules))]
        public void VisiblePropertiesMustHaveDisplayName(string ruleName, string fullPath)
        {
            // The "DisplayName" property is localised, while "Name" is not.
            // Visible properties without a "DisplayName" will appear in English in all locales.

            XElement rule = LoadXamlRule(fullPath);

            foreach (var property in GetProperties(rule))
            {
                // Properties are visible by default
                string visibleValue = property.Attribute("Visible")?.Value ?? "true";

                Assert.True(bool.TryParse(visibleValue, out bool visible));

                if (!visible)
                    continue;

                string? displayName = property.Attribute("DisplayName")?.Value;

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    throw new Xunit.Sdk.XunitException($"Rule {ruleName} has visible property {property.Attribute("Name")} with no DisplayName value.");
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertyDescriptionMustEndWithFullStop(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (var property in GetProperties(rule))
            {
                string? description = property.Attribute("Description")?.Value;

                if (description?.EndsWith(".") == false)
                {
                    throw new Xunit.Sdk.XunitException($"Rule {ruleName} has visible property {property.Attribute("Name")} with description not ending in a period.");
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void RuleMustHaveAName(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            string? name = rule.Attribute("Name")?.Value;

            Assert.NotNull(name);
            Assert.NotEqual("", name);
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void TargetResultsDataSourcesMustSpecifyTheTarget(string ruleName, string fullPath)
        {
            var root = LoadXamlRule(fullPath, out var namespaceManager);

            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            var sourceType = dataSource?.Attribute("SourceType");
            var msBuildTarget = dataSource?.Attribute("MSBuildTarget");

            if (sourceType != null)
            {
                if (sourceType.Value == "TargetResults")
                {
                    // A target must be specified
                    Assert.NotNull(msBuildTarget);
                }
                else
                {
                    // Target must not be specified on other source types
                    Assert.Null(msBuildTarget);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void ItemDataSourcesMustSpecifyTheItemType(string ruleName, string fullPath)
        {
            var root = LoadXamlRule(fullPath, out var namespaceManager);

            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            var sourceType = dataSource?.Attribute("SourceType");
            var itemType = dataSource?.Attribute("ItemType");

            if (sourceType?.Value == "Item")
            {
                // An item type must be specified
                Assert.NotNull(itemType);
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertiesDataSourcesMustMatchItemDataSources(string ruleName, string fullPath)
        {
            var root = LoadXamlRule(fullPath, out var namespaceManager);

            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            var itemType = dataSource?.Attribute("ItemType");
            if (itemType != null)
            {
                foreach (var property in GetProperties(root))
                {
                    var element = GetDataSource(property);

                    var propertyItemType = element?.Attribute("ItemType");
                    if (propertyItemType != null)
                    {
                        Assert.Equal(itemType?.Value, propertyItemType.Value);
                    }
                }
            }
        }

        public static IEnumerable<object[]> GetMiscellaneousRules()
        {
            return Project(GetRules(""));
        }

        public static IEnumerable<object[]> GetAllDisplayedRules()
        {
            return GetMiscellaneousRules()
                .Concat(ItemRuleTests.GetBrowseObjectItemRules())
                .Concat(DependencyRuleTests.GetDependenciesRules());
        }

        public static IEnumerable<object[]> GetAllRules()
        {
            return GetMiscellaneousRules()
                .Concat(ItemRuleTests.GetItemRules())
                .Concat(DependencyRuleTests.GetDependenciesRules());
        }
    }
}
