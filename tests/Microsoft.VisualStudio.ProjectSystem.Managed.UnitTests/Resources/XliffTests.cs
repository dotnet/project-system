// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Resources
{
    public sealed class XliffTests
    {
        private const string XliffNamespace = "urn:oasis:names:tc:xliff:document:1.2";

        private static readonly Regex s_acceleratorPattern = new(@"&\w");

        [Fact]
        public void ResourceStringsDoNotContainMultipleAcceleratorMnemonics()
        {
            foreach (string path in GetXlfFiles())
            {
                var settings = new XmlReaderSettings { XmlResolver = null };
                using var fileStream = File.OpenRead(path);
                using var reader = XmlReader.Create(fileStream, settings);
                var root = XDocument.Load(reader).Root;

                var namespaceManager = new XmlNamespaceManager(reader.NameTable);
                namespaceManager.AddNamespace("x", XliffNamespace);

                var targets = root.XPathSelectElements(@"/x:xliff/x:file/x:body/x:trans-unit/x:target", namespaceManager);

                foreach (var target in targets)
                {
                    var matches = s_acceleratorPattern.Matches(target.Value);

                    if (matches.Count > 1)
                    {
                        throw new Xunit.Sdk.XunitException($"Translated string in {Path.GetFileName(path)} contains multiple accelerator mnemonics: {target.Value}");
                    }
                }
            }

            static IEnumerable<string> GetXlfFiles()
            {
                var root = RepoUtil.FindRepoRootPath();

                return Directory.EnumerateFiles(root, "*.xlf", SearchOption.AllDirectories);
            }
        }
    }
}
