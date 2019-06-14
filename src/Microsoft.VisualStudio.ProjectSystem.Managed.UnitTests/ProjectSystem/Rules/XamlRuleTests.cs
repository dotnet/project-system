// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Xunit;

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

            XmlDocument none = LoadXamlRule(noneFile);
            XmlDocument rule = LoadXamlRule(fullPath);

            // First fix up the Name as we know they'll differ.
            rule.DocumentElement.Attributes["Name"].Value = "None";

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

            XmlDocument none = LoadXamlRule(noneFile);
            XmlDocument rule = LoadXamlRule(fullPath);

            // First fix up the Name and DisplayName as we know they'll differ.
            rule.DocumentElement.Attributes["Name"].Value = "None";
            rule.DocumentElement.Attributes["DisplayName"].Value = "General";

            AssertXmlEqual(none, rule);
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void NonVisiblePropertiesShouldntBeLocalized(string ruleName, string fullPath)
        {
            XmlDocument rule = LoadXamlRule(fullPath);

            foreach (XmlNode node in rule.DocumentElement.ChildNodes)
            {
                if (node is XmlElement element && element.HasAttribute("Visible") && element.Attributes["Visible"].Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.False(element.HasAttribute("DisplayName"));
                    Assert.False(element.HasAttribute("Description"));
                    Assert.False(element.HasAttribute("Category"));
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllRules))]
        public void VisiblePropertiesMustHaveDisplayName(string ruleName, string fullPath)
        {
            // The "DisplayName" property is localised, while "Name" is not.
            // Visible properties without a "DisplayName" will appear in English in all locales.

            XElement rule = LoadXamlRuleX(fullPath).Root;

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
            XElement rule = LoadXamlRuleX(fullPath).Root;

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
            XElement rule = LoadXamlRuleX(fullPath).Root;

            // Ignore XAML documents for other types such as ProjectSchemaDefinitions
            if (rule.Name.LocalName != "Rule")
                return;

            string name = rule.Attribute("Name")?.Value;

            Assert.NotNull(name);
            Assert.NotEqual("", name);
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

            XmlDocument rule = LoadXamlRule(fullPath);

            foreach (XmlNode node in rule.DocumentElement.ChildNodes)
            {
                if (node is XmlElement element)
                {
                    // No need to define categories if they're not going to be used
                    Assert.False(element.Name.Equals("Rule.Categories", StringComparison.OrdinalIgnoreCase));
                    Assert.False(element.HasAttribute("DisplayName"));
                    Assert.False(element.HasAttribute("Description"));
                    Assert.False(element.HasAttribute("Category"));
                }
            }
        }


        [Theory]
        [MemberData(nameof(GetBrowseObjectItemRules))]
        public void HidePropertyPagesForBrowseObjectRules(string ruleName, string fullPath)
        {
            XmlDocument rule = LoadXamlRule(fullPath);

            Assert.True(rule.DocumentElement.HasAttribute("PropertyPagesHidden"), "No PropertyPagesHidden attribute found on rule.");

            var value = rule.DocumentElement.Attributes["PropertyPagesHidden"].Value;
            Assert.Equal("true", value, ignoreCase: true);
        }

        [Theory]
        [MemberData(nameof(GetAllItemRules))]
        public void RuleNameMatchesFileName(string ruleName, string fullPath)
        {
            XmlDocument rule = LoadXamlRule(fullPath);

            // If a rule is split between File and BrowseObject we need to trim the BrowseObject part off
            if (ruleName.IndexOf('.') > -1)
            {
                ruleName = ruleName.Substring(0, ruleName.IndexOf('.'));
            }

            Assert.Equal(ruleName, rule.DocumentElement.Attributes["Name"].Value);
        }

        [Theory]
        [MemberData(nameof(GetAllItemRules))]
        public void ItemTypesMustMatchFileNameRoot(string ruleName, string fullPath)
        {
            // If a rule is split between File and BrowseObject we need to trim the BrowseObject part off
            if (ruleName.IndexOf('.') > -1)
            {
                ruleName = ruleName.Substring(0, ruleName.IndexOf('.'));
            }

            XmlDocument rule = LoadXamlRule(fullPath);
            CheckItemName(rule.DocumentElement);

            void CheckItemName(XmlNode node)
            {
                var itemTypeAtt = node.Attributes["ItemType"];
                if (itemTypeAtt != null)
                {
                    Assert.Equal(ruleName, itemTypeAtt.Value);
                }
                foreach (XmlNode child in node.ChildNodes)
                {
                    CheckItemName(child);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetFileItemRules))]
        public void FileRulesShouldntHaveFileInfo(string ruleName, string fullPath)
        {
            XmlDocument rule = LoadXamlRule(fullPath);
            foreach (XmlNode node in rule.DocumentElement.ChildNodes)
            {
                // special case - Folder's Identity property is used by dependencies node
                if (!ruleName.Equals("Folder", StringComparison.Ordinal))
                {
                    Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("Identity", StringComparison.OrdinalIgnoreCase));
                }
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("FileNameAndExtension", StringComparison.OrdinalIgnoreCase));
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("URL", StringComparison.OrdinalIgnoreCase));
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("Extension", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static IEnumerable<object[]> GetBrowseObjectItemRules()
        {
            return GetRules("Items", false, true);
        }

        public static IEnumerable<object[]> GetFileItemRules()
        {
            return GetRules("Items", true, false);
        }

        public static IEnumerable<object[]> GetAllItemRules()
        {
            return GetRules("Items", true, true);
        }

        public static IEnumerable<object[]> GetAllRules()
        {
            return GetRules("", true, true);
        }

        public static IEnumerable<object[]> GetRules(string suffix, bool file, bool browseObject)
        {
            // Not all rules are embedded as manifests so we have to read the xaml files from the file system.
            // Instead of hardcoding knowledge of unit test paths and build locations, we just traverse up to the artifacts folder and start from there
            string artifactsPath = typeof(XamlRuleTests).Assembly.Location;
            while (!Path.GetFileName(artifactsPath).Equals("artifacts", StringComparisons.Paths))
            {
                artifactsPath = Path.GetDirectoryName(artifactsPath);
            }

            string rulesPath = Path.Combine(artifactsPath, "..", "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "Rules");
            Assert.True(Directory.Exists(rulesPath), "Couldn't find XAML rules folder: " + rulesPath);

            if (!string.IsNullOrEmpty(suffix))
            {
                rulesPath = Path.Combine(rulesPath, suffix);
            }
            foreach (var fileName in Directory.EnumerateFiles(rulesPath, "*.xaml"))
            {
                if (fileName.EndsWith(".BrowseObject.xaml", StringComparisons.Paths) && !browseObject)
                {
                    continue;
                }
                // Special case for Folder because it is File and BrowseObject context (for now) but naming convention is like File
                if ((!fileName.EndsWith(".BrowseObject.xaml", StringComparisons.Paths) && !file) ||
                    (fileName.EndsWith("Folder.xaml", StringComparisons.Paths) && browseObject))
                {
                    continue;
                }

                // we return the rule name separately mainly to get a readable display in Test Explorer so failures can be diagnosed more easily
                yield return new object[] { Path.GetFileNameWithoutExtension(fileName), fileName };
            }
        }

        private static XmlDocument LoadXamlRule(string fullPath)
        {
            var rule = new XmlDocument() { XmlResolver = null };
            var settings = new XmlReaderSettings { XmlResolver = null };
            using (var fileStream = File.OpenRead(fullPath))
            using (var reader = XmlReader.Create(fileStream, settings))
            {
                rule.Load(reader);
            }

            return rule;
        }

        private static XDocument LoadXamlRuleX(string fullPath)
        {
            var settings = new XmlReaderSettings { XmlResolver = null };
            using var fileStream = File.OpenRead(fullPath);
            using var reader = XmlReader.Create(fileStream, settings);
            return XDocument.Load(reader);
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

        private void AssertXmlEqual(XmlDocument left, XmlDocument right)
        {
            AssertXmlEqual(left.DocumentElement, right.DocumentElement);
        }

        private void AssertXmlEqual(XmlElement left, XmlElement right)
        {
            Assert.Equal(left.Name, right.Name, ignoreCase: true);

            var leftAttributes = left.Attributes.OfType<XmlAttribute>().OrderBy(a => a.Name).ToArray();
            var rightAttributes = right.Attributes.OfType<XmlAttribute>().OrderBy(a => a.Name).ToArray();
            Assert.Equal(leftAttributes.Length, rightAttributes.Length);

            for (int i = 0; i < leftAttributes.Length; i++)
            {
                AssertAttributesEqual(leftAttributes[i], rightAttributes[i]);
            }

            var leftChildNodes = left.ChildNodes.OfType<XmlElement>().OrderBy(a => a.Name).ToArray();
            var rightChildNodes = right.ChildNodes.OfType<XmlElement>().OrderBy(a => a.Name).ToArray();
            Assert.Equal(leftChildNodes.Length, rightChildNodes.Length);

            for (int i = 0; i < leftChildNodes.Length; i++)
            {
                AssertXmlEqual(leftChildNodes[i], rightChildNodes[i]);
            }
        }

        private void AssertAttributesEqual(XmlAttribute left, XmlAttribute right)
        {
            Assert.Equal(left.Name, right.Name, ignoreCase: true);

            // ignore ItemType as we know they'll be different
            if (left.Name.Equals("ItemType", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Assert.Equal(left.Value, right.Value, ignoreCase: true);
        }
    }
}
