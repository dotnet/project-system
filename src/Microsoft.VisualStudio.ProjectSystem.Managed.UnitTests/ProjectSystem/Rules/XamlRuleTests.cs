using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public class XamlRuleTests
    {
        [Theory]
        [MemberData(nameof(GetBrowseObjectItemRules))]
        public void InvisiblePropertiesShouldntBeLocalized(string ruleName, string fullPath)
        {
            XmlDocument rule = LoadXamlRule(fullPath);

            // TODO:
        }

        [Theory]
        [MemberData(nameof(GetFileItemRules))]
        public void FileRulesShouldntBeLocalized(string ruleName, string fullPath)
        {
            XmlDocument rule = LoadXamlRule(fullPath);
            
            // TODO:
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
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("Identity", StringComparison.OrdinalIgnoreCase));
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("FileNameAndExtension", StringComparison.OrdinalIgnoreCase));
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("URL", StringComparison.OrdinalIgnoreCase));
                Assert.False(node.Name.Equals("StringProperty", StringComparison.Ordinal) && node.Attributes["Name"].Value.Equals("Extension", StringComparison.OrdinalIgnoreCase));
            }
        }

        public static IEnumerable<object[]> GetBrowseObjectItemRules()
        {
            return GetItemRules(false, true);
        }

        public static IEnumerable<object[]> GetFileItemRules()
        {
            return GetItemRules(true, false);
        }

        public static IEnumerable<object[]> GetAllItemRules()
        {
            return GetItemRules(true, true);
        }

        public static IEnumerable<object[]> GetItemRules(bool file, bool browseObject)
        {
            // Not all rules are embedded as manifests so we have to run these using the file system.
            // Instead of hardcoding knowledge of unit test paths and build locations, we just look for the artifacts folder and start from there
            string artifactsPath = typeof(XamlRuleTests).Assembly.Location;
            while (!Path.GetFileName(artifactsPath).Equals("artifacts", StringComparisons.Paths))
            {
                artifactsPath = Path.GetDirectoryName(artifactsPath);
            }
            string rulesPath = Path.Combine(artifactsPath, "..", "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "Rules");
            Assert.True(Directory.Exists(rulesPath), "Couldn't find XAML rules folder: " + rulesPath);

            string itemsPath = Path.Combine(rulesPath, "Items");
            foreach (var fileName in Directory.EnumerateFiles(itemsPath, "*.xaml"))
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

                // we return the rule name separately mainly to get a readable display in Test Explorer so failures can be diagnosed
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
    }
}
