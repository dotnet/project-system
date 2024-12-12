﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.ProjectSystem.Rules;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rules;

public abstract class XamlRuleTestBase
{
    protected const string MSBuildNamespace = "http://schemas.microsoft.com/build/2009/properties";

    protected static IEnumerable<(string Path, Type AssemblyExporterType)> GetRules(string? suffix = null, bool recursive = false)
    {
        // Not all rules are embedded as manifests so we have to read the xaml files from the file system.
        (string, Type)[] projects =
        {
            (Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.Managed", "ProjectSystem", "Rules"), typeof(RuleExporter)),
            (Path.Combine(RepoUtil.FindRepoRootPath(), "src", "Microsoft.VisualStudio.ProjectSystem.VS.Managed", "ProjectSystem", "Rules"), typeof(VSRuleExporter))
        };

        bool foundDirectory = false;

        foreach ((string basePath, Type assemblyExporterType) in projects)
        {
            string path = string.IsNullOrEmpty(suffix) ? basePath : Path.Combine(basePath, suffix);

            if (Directory.Exists(path))
            {
                foundDirectory = true;

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                foreach (var filePath in Directory.EnumerateFiles(path, "*.xaml", searchOption))
                {
                    XElement rule = LoadXamlRule(filePath);

                    // Ignore XAML documents for non-Rule types (such as ProjectSchemaDefinitions)
                    if (rule.Name.LocalName == "Rule")
                    {
                        yield return (filePath, assemblyExporterType);
                    }
                }
            }
        }

        Assert.True(foundDirectory, $"Couldn't find XAML rules folder with suffix '{suffix}'.");
    }

    /// <summary>Projects a XAML rule file's path into the form used by unit tests theories.</summary>
    protected static IEnumerable<object[]> Project(IEnumerable<(string Path, Type AssemblyExporterType)> files)
    {
        // we return the rule name separately mainly to get a readable display in Test Explorer so failures can be diagnosed more easily
        return from file in files
               select new object[] { Path.GetFileNameWithoutExtension(file.Path), file.Path, file.AssemblyExporterType };
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
