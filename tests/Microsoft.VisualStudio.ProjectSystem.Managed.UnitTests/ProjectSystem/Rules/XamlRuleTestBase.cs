// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public abstract class XamlRuleTestBase
    {
        protected const string MSBuildNamespace = "http://schemas.microsoft.com/build/2009/properties";

        protected static IEnumerable<string> GetRules(string suffix, bool recursive = false)
        {
            // Not all rules are embedded as manifests so we have to read the xaml files from the file system.
            string rulesPath = Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "Rules");

            if (!string.IsNullOrEmpty(suffix))
            {
                rulesPath = Path.Combine(rulesPath, suffix);
            }

            Assert.True(Directory.Exists(rulesPath), "Couldn't find XAML rules folder: " + rulesPath);

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var filePath in Directory.EnumerateFiles(rulesPath, "*.xaml", searchOption))
            {
                XElement rule = LoadXamlRule(filePath);

                // Ignore XAML documents for non-Rule types (such as ProjectSchemaDefinitions)
                if (rule.Name.LocalName != "Rule")
                    continue;

                yield return filePath;
            }
        }

        /// <summary>Projects a XAML rule file's path into the form used by unit tests theories.</summary>
        protected static IEnumerable<object[]> Project(IEnumerable<string> filePaths)
        {
            // we return the rule name separately mainly to get a readable display in Test Explorer so failures can be diagnosed more easily
            return from filePath in filePaths
                   select new object[] { Path.GetFileNameWithoutExtension(filePath), filePath };
        }

        protected static XElement LoadXamlRule(string filePath)
        {
            return LoadXamlRule(filePath, out _);
        }

        protected static XElement LoadXamlRule(string filePath, out XmlNamespaceManager namespaceManager)
        {
            var settings = new XmlReaderSettings { XmlResolver = null };
            using var fileStream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(fileStream, settings);
            var root = XDocument.Load(reader).Root;

            namespaceManager = new XmlNamespaceManager(reader.NameTable);
            namespaceManager.AddNamespace("msb", MSBuildNamespace);

            return root;
        }

        protected static IEnumerable<XElement> GetProperties(XElement rule)
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

        protected static XElement? GetDataSource(XElement property)
        {
            foreach (var child in property.Elements())
            {
                if (child.Name.LocalName.EndsWith("DataSource", StringComparison.Ordinal))
                    return child.Elements().First();
            }

            return null;
        }

        protected static IEnumerable<XElement> GetVisibleProperties(XElement rule)
        {
            foreach (var property in GetProperties(rule))
            {
                if (!string.Equals(property.Attribute("Visible")?.Value, "False", StringComparison.OrdinalIgnoreCase))
                {
                    yield return property;
                }
            }
        }
    }
}
