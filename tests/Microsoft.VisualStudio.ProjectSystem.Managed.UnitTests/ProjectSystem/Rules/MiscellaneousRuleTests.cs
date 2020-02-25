// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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

                if (visibleAttribute != null && visibleAttribute.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Null(element.Attribute("DisplayName"));
                    Assert.Null(element.Attribute("Description"));
                    Assert.Null(element.Attribute("Category"));
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDisplayedRules))]
        public void MarkedRuleFileShouldntBeLocalized(string ruleName, string fullPath)
        {
            // Rule files marked with NO_TRANSLATE do not show up in the UI, and therefore shouldn't be localized.

            var fileIsMarked = File.ReadLines(fullPath).Any(line => line.Contains("NO_TRANSLATE"));

            if (!fileIsMarked)
                return;

            // Since we have determined the file is marked with NO_TRANSLATE, let's make sure we don't have unncessary localization attributes.
            XElement rule = LoadXamlRule(fullPath);
            foreach(XElement element in rule.Elements())
            {
                var visibleAttribute = element.Attribute("Visible");
                if (visibleAttribute != null)
                {
                    Assert.Equal("false", visibleAttribute?.Value, StringComparer.OrdinalIgnoreCase);
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
