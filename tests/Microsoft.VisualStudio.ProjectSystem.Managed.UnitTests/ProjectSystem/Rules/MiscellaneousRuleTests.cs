// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class MiscellaneousRuleTests : XamlRuleTestBase
    {
        private static readonly HashSet<string> s_embeddedRuleNames;

        static MiscellaneousRuleTests()
        {
            s_embeddedRuleNames = new(EnumerateEmbeddedTypeNames(), StringComparer.Ordinal);

            static IEnumerable<string> EnumerateEmbeddedTypeNames()
            {
                foreach (var type in typeof(RuleExporter).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
                {
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        foreach (var exportRule in field.GetCustomAttributes<ExportRuleAttribute>())
                        {
                            yield return exportRule.RuleName;
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetEmbeddedRules))]
        public void EmbeddedRulesShouldNotHaveVisibleProperties(string ruleName, string fullPath)
        {
            // We are not currently able to localize embedded rules. Such rules must not have visible properties,
            // as all visible values must be localized.

            XElement rule = LoadXamlRule(fullPath);

            foreach (var property in 
                GetProperties(rule))
            {
                Assert.False(
                    IsVisible(property),
                    $"Property '{Name(property)}' in rule '{ruleName}' should not be visible.");
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDisplayedRules))]
        public void NonVisiblePropertiesShouldntBeLocalized(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (var property in GetProperties(rule))
            {
                if (!IsVisible(property) && Name(property) is not ("SplashScreen" or "MinimumSplashScreenDisplayTime"))
                {
                    AssertAttributeNotPresent(property, "DisplayName");
                    AssertAttributeNotPresent(property, "Description");
                    AssertAttributeNotPresent(property, "Category");
                }
            }

            static void AssertAttributeNotPresent(XElement element, string attributeName)
            {
                Assert.True(
                    element.Attribute(attributeName) is null,
                    userMessage: $"{attributeName} should not be present:\n{element}");
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
                if (IsVisible(property))
                {
                    string? displayName = property.Attribute("DisplayName")?.Value;

                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        throw new Xunit.Sdk.XunitException($"""
                            Rule {ruleName} has visible property {Name(property)} with no DisplayName value.
                            """);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertyDescriptionMustEndWithFullStop(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out XmlNamespaceManager namespaceManager);

            foreach (var property in GetProperties(rule))
            {
                // <Rule>
                //   <StringProperty>
                //     <StringProperty.ValueEditors>
                //       <ValueEditor EditorType="LinkAction">

                var linkActionEditors = property.XPathSelectElements(@"./msb:StringProperty.ValueEditors/msb:ValueEditor[@EditorType=""LinkAction""]", namespaceManager);

                if (linkActionEditors.Any())
                {
                    // LinkAction items use the description in hyperlink or button text.
                    // Neither of these needs to end with a full stop.
                    continue;
                }

                string? description = property.Attribute("Description")?.Value;

                if (description?.EndsWith(".") == false)
                {
                    throw new Xunit.Sdk.XunitException($"""
                        Rule {ruleName} has visible property {property.Attribute("Name")} with description not ending in a period.
                        """);
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

            if (sourceType is not null)
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

            var sourceType = dataSource?.Attribute("SourceType")?.Value;
            var itemType = dataSource?.Attribute("ItemType");

            if (sourceType == "Item")
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

            // Get the top-level data source element
            var dataSource = root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);

            // If the top level defines an ItemType, all properties must specify a matching ItemType.
            var ruleItemType = dataSource?.Attribute("ItemType")?.Value;

            if (ruleItemType is not null)
            {
                foreach (var property in GetProperties(root))
                {
                    var element = GetDataSource(property);

                    var propertyItemType = element?.Attribute("ItemType")?.Value;
                    if (propertyItemType is not null)
                    {
                        Assert.True(
                            StringComparer.Ordinal.Equals(ruleItemType, propertyItemType),
                            $"""Property data source has item type '{propertyItemType}' but the rule data source has item type '{ruleItemType} which does not match'.""");
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertyNamesMustNotContainSpaces(string ruleName, string fullPath)
        {
            // While MSBuild properties may not contain spaces in their names this restriction doesn't necessarily
            // apply when the property in the Rule connects to a non-MSBuild DataSource. Currently none of our
            // properties have names with spaces, so for the moment we'll just keep this test simple.
            
            var root = LoadXamlRule(fullPath);

            foreach (var property in GetProperties(root))
            {
                var name = property.Attribute("Name")?.Value;

                Assert.NotNull(name);
                Assert.False(name.Contains(" "), $"Property name '{name}' in rule '{ruleName}' contains a space. This is likely to lead to a runtime exception when accessing the property");
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
                .Concat(DependencyRuleTests.GetDependenciesRules())
                .Concat(ProjectPropertiesLocalizationRuleTests.GetPropertyPagesRules());
        }

        public static IEnumerable<object[]> GetAllRules()
        {
            return GetMiscellaneousRules()
                .Concat(ItemRuleTests.GetItemRules())
                .Concat(DependencyRuleTests.GetDependenciesRules())
                .Concat(ProjectPropertiesLocalizationRuleTests.GetPropertyPagesRules());
        }

        public static IEnumerable<object[]> GetEmbeddedRules()
        {
            foreach (var rule in GetAllRules())
            {
                string ruleName = (string)rule[0];
                string fullPath = (string)rule[1];

                if (s_embeddedRuleNames.Contains(ruleName))
                {
                    yield return rule;
                }
            }
        }

        private static bool IsVisible(XElement property)
        {
            // Properties are visible by default
            string visibleValue = property.Attribute("Visible")?.Value ?? bool.TrueString;

            Assert.True(bool.TryParse(visibleValue, out bool isVisible));

            return isVisible;
        }

        private static string Name(XElement rule)
        {
            return rule.Attribute("Name")?.Value ?? throw new Xunit.Sdk.XunitException($"Rule must have a name.\n{rule}");
        }
    }
}
