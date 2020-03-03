// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class MiscellaneousRuleTests : XamlRuleTestBase
    {
        [Fact]
        public void LocalizationCheck()
        {
            string designTimePath = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "DesignTimeTargets", "Microsoft.Managed.DesignTime.targets");

            XElement? root = LoadXamlRule(designTimePath, out XmlNamespaceManager? namespaceManager);

            var propertyPageSchemas = root.XPathSelectElements(@"/project:Project/project:ItemGroup/project:PropertyPageSchema", namespaceManager);

            // To have a view of how things are right now, I used some lists to view how things are handled.
            List<string> allFiles = new List<string>();
            List<string> browseObjects = new List<string>();
            List<string> needsToBeNeutral = new List<string>();
            List<string> alreadyNeutral = new List<string>();

            foreach (XElement element in propertyPageSchemas)
            {
                // Want to have more clarity on this part of the code.
                //element.Element("Foo");
                //string g = element.Element(XName.Get("Context", "project")).Value;

                string contextValue = element.XPathSelectElement("./project:Context", namespaceManager).Value;
                string resourceDirectory = element.Attribute("Include").Value;

                Regex rx = new Regex(@"\).*\.xaml", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Match match = rx.Match(resourceDirectory);
                allFiles.Add(match.Value.Substring(1));

                if (contextValue.Contains("BrowseObject"))
                {
                    // Browse Objects are localizable, so they should not have a Resource Directory set as neutral.                    
                    Assert.DoesNotContain("Neutral", resourceDirectory);
                    browseObjects.Add(match.Value.Substring(1));
                }
                else
                {
                    // Context does not have Browse Object -> file should not be localizable -> the Resource Directory should be neutral.
                    if (resourceDirectory.Contains("Neutral"))
                    {
                        // It is already neutral, yay!
                        alreadyNeutral.Add(match.Value.Substring(1));
                    }
                    else
                    {
                        // It needs to be neutral...
                        needsToBeNeutral.Add(match.Value.Substring(1));
                    }
                }
            }

            // Set a breakpoint and debug it to view results!
        }

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
