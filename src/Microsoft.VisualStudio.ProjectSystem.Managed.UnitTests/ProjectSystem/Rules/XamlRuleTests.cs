// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Microsoft.VisualStudio.Utilities;

using Xunit;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public class XamlRuleTests
    {
        [Theory]
        [MemberData(nameof(GetFileItemRules))]
        public void FileRulesShouldMatchNone(string ruleName, string fullPath)
        {
            // Special case for Folder rule which hasn't been split yet, but is in the Items folder. But also its completely different.
            if (ruleName.Equals("Folder", StringComparison.Ordinal))
            {
                return;
            }
            // No need to check None against None
            if (ruleName.Equals("None", StringComparison.Ordinal))
            {
                return;
            }

            string noneFile = Path.Combine(fullPath, "..", "None.xaml");

            XElement none = LoadXamlRule(noneFile);
            XElement rule = LoadXamlRule(fullPath);

            // First fix up the Name as we know they'll differ.
            rule.Attribute("Name").Value = "None";

            AssertXmlEqual(none, rule);
        }

        [Theory]
        [MemberData(nameof(GetBrowseObjectItemRules))]
        public void BrowseObjectRulesShouldMatchNone(string ruleName, string fullPath)
        {
            // Special case for Folder rule which hasn't been split yet, but is in the Items folder. But also its completely different.
            if (ruleName.Equals("Folder", StringComparison.Ordinal))
            {
                return;
            }
            // No need to check None against None
            if (ruleName.Equals("None", StringComparison.Ordinal))
            {
                return;
            }

            string noneFile = Path.Combine(fullPath, "..", "None.BrowseObject.xaml");

            XElement none = LoadXamlRule(noneFile);
            XElement rule = LoadXamlRule(fullPath);

            // First fix up the Name and DisplayName as we know they'll differ.
            rule.Attribute("Name").Value = "None";
            rule.Attribute("DisplayName").Value = "General";

            AssertXmlEqual(none, rule);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        [MemberData(nameof(GetItemRules))]
        [MemberData(nameof(GetMiscellaneousRules))]
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
        [MemberData(nameof(GetDependenciesRules))]
        [MemberData(nameof(GetBrowseObjectItemRules))]
        [MemberData(nameof(GetMiscellaneousRules))]
        public void VisiblePropertiesMustHaveDisplayName(string ruleName, string fullPath)
        {
            // The "DisplayName" property is localised, while "Name" is not.
            // Visible properties without a "DisplayName" will appear in English in all locales.

            XElement rule = LoadXamlRule(fullPath);

            // Ignore XAML documents for other types such as ProjectSchemaDefinitions
            if (rule.Name.LocalName != "Rule")
                return;

            foreach (var property in GetProperties(rule))
            {
                // Properties are visible by default
                string visibleValue = property.Attribute("Visible")?.Value ?? "true";

                Assert.True(bool.TryParse(visibleValue, out bool visible));

                if (!visible)
                    continue;

                string displayName = property.Attribute("DisplayName")?.Value;

                Assert.NotNull(displayName);
                Assert.NotEqual("", displayName);
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void PropertyDescriptionMustEndWithFullStop(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            // Ignore XAML documents for other types such as ProjectSchemaDefinitions
            if (rule.Name.LocalName != "Rule")
                return;

            foreach (var property in GetProperties(rule))
            {
                string description = property.Attribute("Description")?.Value;

                if (description != null)
                {
                    Assert.EndsWith(".", description);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void RuleMustHaveAName(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            // Ignore XAML documents for other types such as ProjectSchemaDefinitions
            if (rule.Name.LocalName != "Rule")
                return;

            string name = rule.Attribute("Name")?.Value;

            Assert.NotNull(name);
            Assert.NotEqual("", name);
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void TargetResultsDataSourcesMustSpecifyTheTarget(string ruleName, string fullPath)
        {
            var dataSource = LoadDataSourceElement(fullPath);

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
            var dataSource = LoadDataSourceElement(fullPath);

            var sourceType = dataSource?.Attribute("SourceType");
            var itemType = dataSource?.Attribute("ItemType");

            if (sourceType != null)
            {
                if (sourceType.Value == "Item")
                {
                    // An item type must be specified
                    Assert.NotNull(itemType);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetFileItemRules))]
        public void FileRulesShouldntBeLocalized(string ruleName, string fullPath)
        {
            // Special case for Folder rule which hasn't been split yet, but is in the Items folder
            if (ruleName.Equals("Folder", StringComparison.Ordinal))
            {
                return;
            }

            XElement rule = LoadXamlRule(fullPath);

            foreach (XElement element in rule.Elements())
            {
                // No need to define categories if they're not going to be used
                Assert.False(element.Name.LocalName.Equals("Rule.Categories", StringComparison.OrdinalIgnoreCase));
                Assert.Null(element.Attribute("DisplayName"));
                Assert.Null(element.Attribute("Description"));
                Assert.Null(element.Attribute("Category"));
            }
        }

        [Theory]
        [MemberData(nameof(GetBrowseObjectItemRules))]
        public void HidePropertyPagesForBrowseObjectRules(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);
            XAttribute attribute = rule.Attribute("PropertyPagesHidden");

            Assert.NotNull(attribute);
            Assert.Equal("true", attribute.Value, ignoreCase: true);
        }

        [Theory]
        [MemberData(nameof(GetItemRules))]
        public void RuleNameMatchesFileName(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            // If a rule is split between File and BrowseObject we need to trim the BrowseObject part off
            if (ruleName.IndexOf('.') > -1)
            {
                ruleName = ruleName.Substring(0, ruleName.IndexOf('.'));
            }

            Assert.Equal(ruleName, rule.Attribute("Name").Value);
        }

        [Theory]
        [MemberData(nameof(GetItemRules))]
        public void ItemTypesMustMatchFileNameRoot(string ruleName, string fullPath)
        {
            // If a rule is split between File and BrowseObject we need to trim the BrowseObject part off
            if (ruleName.IndexOf('.') > -1)
            {
                ruleName = ruleName.Substring(0, ruleName.IndexOf('.'));
            }

            XElement rule = LoadXamlRule(fullPath);

            foreach (var element in rule.Descendants())
            {
                var attribute = element.Attribute("ItemType");

                if (attribute != null)
                {
                    Assert.Equal(ruleName, attribute.Value);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetFileItemRules))]
        public void FileRulesShouldntHaveFileInfo(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            foreach (XElement element in rule.Elements())
            {
                var nameAttribute = element.Attribute("Name");
                if (nameAttribute == null)
                {
                    continue;
                }

                var isStringProperty = element.Name.LocalName.Equals("StringProperty", StringComparison.Ordinal);

                if (!isStringProperty)
                {
                    continue;
                }

                var name = nameAttribute.Value;

                // special case - Folder's Identity property is used by dependencies node
                if (!ruleName.Equals("Folder", StringComparison.Ordinal))
                {
                    Assert.False(name.Equals("Identity", StringComparison.OrdinalIgnoreCase));
                }

                Assert.False(name.Equals("FileNameAndExtension", StringComparison.OrdinalIgnoreCase));
                Assert.False(name.Equals("URL", StringComparison.OrdinalIgnoreCase));
                Assert.False(name.Equals("Extension", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static IEnumerable<object[]> GetBrowseObjectItemRules()
        {
            // Special case for Folder because it is both File and BrowseObject context (for now), but is named like a File.
            return Project(GetRules("Items")
                .Where(fileName => fileName.EndsWith(".BrowseObject.xaml", StringComparisons.Paths) ||
                                   fileName.Equals("Folder.xaml", StringComparisons.Paths)));
        }

        public static IEnumerable<object[]> GetFileItemRules()
        {
            return Project(GetRules("Items")
                .Where(fileName => !fileName.EndsWith(".BrowseObject.xaml", StringComparisons.Paths)));
        }

        public static IEnumerable<object[]> GetItemRules()
        {
            return Project(GetRules("Items"));
        }

        public static IEnumerable<object[]> GetDependenciesRules()
        {
            return Project(GetRules("Dependencies"));
        }

        public static IEnumerable<object[]> GetMiscellaneousRules()
        {
            return Project(GetRules(""));
        }

        public static IEnumerable<object[]> GetAllRules()
        {
            return GetMiscellaneousRules()
                .Concat(GetItemRules())
                .Concat(GetDependenciesRules());
        }

        private static IEnumerable<string> GetRules(string suffix)
        {
            // Not all rules are embedded as manifests so we have to read the xaml files from the file system.
            string rulesPath = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "Rules");

            if (!string.IsNullOrEmpty(suffix))
            {
                rulesPath = Path.Combine(rulesPath, suffix);
            }

            Assert.True(Directory.Exists(rulesPath), "Couldn't find XAML rules folder: " + rulesPath);

            foreach (var fileName in Directory.EnumerateFiles(rulesPath, "*.xaml"))
            {
                yield return fileName;
            }
        }

        /// <summary>Projects a XAML file name into the form used by unit tests theories.</summary>
        private static IEnumerable<object[]> Project(IEnumerable<string> fileNames)
        {
            // we return the rule name separately mainly to get a readable display in Test Explorer so failures can be diagnosed more easily
            return from fileName in fileNames
                   select new object[] { Path.GetFileNameWithoutExtension(fileName), fileName };
        }

        private static XElement LoadXamlRule(string fullPath)
        {
            var settings = new XmlReaderSettings { XmlResolver = null };
            using var fileStream = File.OpenRead(fullPath);
            using var reader = XmlReader.Create(fileStream, settings);
            return XDocument.Load(reader).Root;
        }

        private static XElement LoadDataSourceElement(string filePath)
        {
            var settings = new XmlReaderSettings { XmlResolver = null };
            using var fileStream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(fileStream, settings);
            var root = XDocument.Load(reader).Root;

            // Ignore XAML documents for other types such as ProjectSchemaDefinitions
            if (root?.Name.LocalName != "Rule")
            {
                return null;
            }

            var namespaceManager = new XmlNamespaceManager(reader.NameTable);
            namespaceManager.AddNamespace("msb", "http://schemas.microsoft.com/build/2009/properties");

            return root.XPathSelectElement(@"/msb:Rule/msb:Rule.DataSource/msb:DataSource", namespaceManager);
        }

        private static IEnumerable<XElement> GetProperties(XElement rule)
        {
            foreach (var child in rule.Elements())
            {
                if (child.Name.LocalName.EndsWith("Property", StringComparison.Ordinal) &&
                    child.Name.LocalName.IndexOf('.') == -1)
                {
                    yield return child;
                }
            }
        }

        private static void AssertXmlEqual(XElement left, XElement right)
        {
            Assert.Equal(left.Name.LocalName, right.Name.LocalName, ignoreCase: true);

            var leftAttributes = left.Attributes().OrderBy(a => a.Name.LocalName).ToArray();
            var rightAttributes = right.Attributes().OrderBy(a => a.Name.LocalName).ToArray();
            Assert.Equal(leftAttributes.Length, rightAttributes.Length);

            for (int i = 0; i < leftAttributes.Length; i++)
            {
                AssertAttributesEqual(leftAttributes[i], rightAttributes[i]);
            }

            var leftChildNodes = left.Elements().OrderBy(a => a.Name.LocalName).ToArray();
            var rightChildNodes = right.Elements().OrderBy(a => a.Name.LocalName).ToArray();
            Assert.Equal(leftChildNodes.Length, rightChildNodes.Length);

            for (int i = 0; i < leftChildNodes.Length; i++)
            {
                AssertXmlEqual(leftChildNodes[i], rightChildNodes[i]);
            }
        }

        private static void AssertAttributesEqual(XAttribute left, XAttribute right)
        {
            Assert.Equal(left.Name.LocalName, right.Name.LocalName, ignoreCase: true);

            // ignore ItemType as we know they'll be different
            if (left.Name.LocalName.Equals("ItemType", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Assert.Equal(left.Value, right.Value, ignoreCase: true);
        }
    }
}
