// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class MiscellaneousRuleTests : XamlRuleTestBase
    {
        private bool LocalizationIsClean = true;
        public Dictionary<string, bool> XamlRules = new Dictionary<string, bool>();
        public Dictionary<string, bool> XamlRulesCopy;
        private readonly ITestOutputHelper _output;

        public MiscellaneousRuleTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void VerifyLocalizationInProjectFile()
        {
            string projectFile = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "Microsoft.VisualStudio.ProjectSystem.Managed.csproj");

            XElement? root = LoadXamlRule(projectFile);
            var XamlPropertyRules = root.XPathSelectElements(@"/Project/ItemGroup/XamlPropertyRule");
            Regex rx = new Regex(@"(\\)(?!.*\\)(.+\.xaml)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (XElement element in XamlPropertyRules)
            {
                string rulePath = element.Attribute("Include").Value;
                string? xlfInput = element.XPathSelectElement("./XlfInput")?.Value;
                
                Match match = rx.Match(rulePath);
                string ruleName = match.Value.Substring(1);

                if (xlfInput?.Contains("false") ?? false)
                {
                    // The rule has its XlfInput element set to false.
                    // Let's make sure this rule is not localized.
                    XamlRules.Add(ruleName, false);
                }
                else
                {
                    // The rule has no XlfInput element (true by default).
                    // Let's make sure this rule is localized.
                    XamlRules.Add(ruleName, true);
                }
            }

            // ProjectItemsSchemas should be localized.
            var projectItemsSchemas = root.XPathSelectElements(@"/Project/ItemGroup/XamlPropertyProjectItemsSchema");

            foreach (XElement element in projectItemsSchemas)
            {
                string rulePath = element.Attribute("Include").Value;
                Match match = rx.Match(rulePath);
                string ruleName = match.Value.Substring(1);

                XamlRules.Add(ruleName, true);
            }

            // This copy will help when a rule is present in two different DesignTime.targets files.
            XamlRulesCopy = new Dictionary<string, bool>(XamlRules);

            // We can continue by looking at the DesignTime.targets files.
            VerifyLocalizationInDesignTimeTargets();
        }

        private void VerifyLocalizationInDesignTimeTargets()
        {
            VerifyLocalizationInProjectFile();
            // For each DesignTime.targets file, check that the rule files with BrowseObject context are localized.

            string managedDesignTimeTargets = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "DesignTimeTargets", "Microsoft.Managed.DesignTime.targets");
            string cSharpDesignTimeTargets = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "DesignTimeTargets", "Microsoft.CSharp.DesignTime.targets");
            string fSharpDesignTimeTargets = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "DesignTimeTargets", "Microsoft.FSharp.DesignTime.targets");
            string visualBasicDesignTimeTargets = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "DesignTimeTargets", "Microsoft.VisualBasic.DesignTime.targets");

            BrowseObjectsShouldBeLocalized(managedDesignTimeTargets);
            BrowseObjectsShouldBeLocalized(cSharpDesignTimeTargets);
            BrowseObjectsShouldBeLocalized(fSharpDesignTimeTargets);
            BrowseObjectsShouldBeLocalized(visualBasicDesignTimeTargets);

            if (XamlRules.Any())
            {
                _output.WriteLine($"There are more rules in the project file than in the DesignTime.targets files. Rules left: {XamlRules.Values}");
                LocalizationIsClean = false;
            }

            Assert.True(LocalizationIsClean, "See test output for details.");
        }

        private void BrowseObjectsShouldBeLocalized(string designTimeTargetFile)
        {
            XElement? root = LoadXamlRule(designTimeTargetFile, out XmlNamespaceManager? namespaceManager);
            var propertyPageSchemas = root.XPathSelectElements(@"/project:Project/project:ItemGroup/project:PropertyPageSchema", namespaceManager);

            foreach (XElement element in propertyPageSchemas)
            {
                string? context = element.XPathSelectElement("./project:Context", namespaceManager)?.Value;
                string resourceDirectory = element.Attribute("Include").Value;

                Regex rx = new Regex(@"\).*\.xaml", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Match match = rx.Match(resourceDirectory);
                string ruleName = match.Value.Substring(1);
                bool alreadySeen = false;

                // To do: How to know the full path. Some are under Rules, Rules/Items, Rules/PropertyPages
                //string fullPath = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "Rules", ruleName);

                if (!XamlRules.ContainsKey(ruleName))
                {
                    if (XamlRulesCopy.ContainsKey(ruleName))
                    {
                        // We've already seen this rule file before.
                        alreadySeen = true;
                    }
                    else
                    {
                        _output.WriteLine($"Rule {ruleName} exists in {designTimeTargetFile} file but not in the project file.");
                        LocalizationIsClean = false;
                        continue;
                    }
                }

                // Get boolean value from dictionary for current rule file to know if the rule should be localized.
                bool valueInDictionary;

                if (alreadySeen)
                {
                    XamlRulesCopy.TryGetValue(ruleName, out valueInDictionary);
                }
                else
                {
                    XamlRules.TryGetValue(ruleName, out valueInDictionary);
                }

                if ( (ruleName.Contains("PropertyPage")
                        || ruleName.Contains("ProjectDebugger")
                        || ruleName.Contains("ProjectItemsSchema"))
                        || (context?.Contains("BrowseObject") ?? false ))
                {
                    // The files specified in the condition do not contain BrowseObject context, but need to be localized.
                    // Browse Objects are localizable, so they should not have a Resource Directory set as neutral.                    
                    Assert.DoesNotContain("Neutral", resourceDirectory);

                    // Ensure project file and DesignTime.targets file are consistent.
                    if (!valueInDictionary)
                    {
                        _output.WriteLine($"Rule {ruleName} is set to not be localized in the project file but set to be localized in the {designTimeTargetFile} file.");
                        LocalizationIsClean = false;
                    }

                    // Verify that these files have localized attributes.
                    //VerifyRuleIsLocalized(fullPath, ruleName);
                }
                else
                {
                    // These files do not show in the UI, so there is no need to be localized nor have localized attributes in their actual rule file.
                    Assert.Contains("Neutral", resourceDirectory);

                    // Ensure project file and DesignTime.targets file are consistent.
                    if (valueInDictionary)
                        throw new Xunit.Sdk.XunitException($"Rule {ruleName} is set to be localized in the project file but set to not be localized in the {designTimeTargetFile} file.");

                    // Verify that these files don't have localized attributes.
                    //VerifyRuleIsNotLocalized(fullPath, ruleName);
                }
                if (!alreadySeen)
                    XamlRules.Remove(ruleName);
            }
        }

        private void VerifyRuleIsNotLocalized(string fullPath, string ruleName)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (XElement element in rule.Elements())
            {
                if (element.Attribute("Visible")?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _output.WriteLine($"{ruleName} {element} contains an attribute that is visible. Make sure visibility is set to false.");
                    LocalizationIsClean = false;
                }

                if (element.Attribute("DisplayName") != null)
                {
                    _output.WriteLine($"{ruleName} {element} contains a DisplayName attribute."); 
                    LocalizationIsClean = false;
                }

                if (element.Attribute("Description") != null)
                {
                    _output.WriteLine($"{ruleName} {element} contains a Description attribute."); 
                    LocalizationIsClean = false;
                }

                if (element.Attribute("Category") != null)
                {
                    _output.WriteLine($"{ruleName} {element} contains a Category attribute."); 
                    LocalizationIsClean = false;
                }
            }
        }

        private void VerifyRuleIsLocalized(string fullPath, string ruleName)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (XElement element in rule.Elements())
            {
                if (element.Attribute("Visible")?.Value.IsNullOrEmpty() == false)
                {
                    _output.WriteLine($"{ruleName} {element} is not visible.");
                    LocalizationIsClean = false;
                }

                if (element.Attribute("DisplayName") == null)
                {
                    _output.WriteLine($"{ruleName} {element} does not contains a DisplayName attribute.");
                    LocalizationIsClean = false;
                }

                if (element.Attribute("Description") == null)
                {
                    _output.WriteLine($"{ruleName} {element} does not contains a Description attribute.");
                    LocalizationIsClean = false;
                }

                if (element.Attribute("Category") == null)
                {
                    _output.WriteLine($"{ruleName} {element} does not contains a Category attribute.");
                    LocalizationIsClean = false;
                }
            }
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
