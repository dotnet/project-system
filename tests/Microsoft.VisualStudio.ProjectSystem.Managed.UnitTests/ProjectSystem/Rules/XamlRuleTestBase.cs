// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.Utilities;
using Xunit;

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

            foreach (var fileName in Directory.EnumerateFiles(rulesPath, "*.xaml", searchOption))
            {
                XElement rule = LoadXamlRule(fileName);

                // Ignore XAML documents for non-Rule types (such as ProjectSchemaDefinitions)
                if (rule.Name.LocalName != "Rule")
                    continue;

                yield return fileName;
            }
        }

        /// <summary>Projects a XAML file name into the form used by unit tests theories.</summary>
        protected static IEnumerable<object[]> Project(IEnumerable<string> fileNames)
        {
            // we return the rule name separately mainly to get a readable display in Test Explorer so failures can be diagnosed more easily
            return from fileName in fileNames
                   select new object[] { Path.GetFileNameWithoutExtension(fileName), fileName };
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
    }
}
