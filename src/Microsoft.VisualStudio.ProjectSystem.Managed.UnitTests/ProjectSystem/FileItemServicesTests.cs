// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class FileItemServicesTests
    {
        [Fact]
        public void GetLogicalFolderNames_NullAsBasePath_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("basePath", () =>
            {
                FileItemServices.GetLogicalFolderNames((string)null, "fullPath", ImmutableDictionary<string, string>.Empty);
            });
        }

        [Fact]
        public void GetLogicalFolderNames_NullAsFullPath_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("fullPath", () =>
            {
                FileItemServices.GetLogicalFolderNames("basePath", (string)null, ImmutableDictionary<string, string>.Empty);
            });
        }

        [Fact]
        public void GetLogicalFolderNames_NullAsMetadata_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("metadata", () =>
            {
                FileItemServices.GetLogicalFolderNames("basePath", "fullPath", (IImmutableDictionary<string, string>)null);
            });
        }

        [Fact]
        public void GetLogicalFolderNames_EmptyAsBasePath_ThrowsArgument()
        {
            Assert.Throws<ArgumentException>("basePath", () =>
            {
                FileItemServices.GetLogicalFolderNames(string.Empty, "fullPath", ImmutableDictionary<string, string>.Empty);
            });
        }

        [Fact]
        public void GetLogicalFolderNames_EmptyAsFullPath_ThrowsArgument()
        {
            Assert.Throws<ArgumentException>("fullPath", () =>
            {
                FileItemServices.GetLogicalFolderNames("basePath", string.Empty, ImmutableDictionary<string, string>.Empty);
            });
        }

        [Theory] // BasePath                                        FullPath                                 Link                                           Expected
        [InlineData("C:\\Project",                                  "C:\\Project\\Source.cs",                null,                                          null)]
        [InlineData("C:\\Project",                                  "C:\\Project\\Source.cs",                "",                                            null)]
        [InlineData("C:\\Project",                                  "C:\\Project\\Folder\\Source.cs",        "",                                            "Folder")]
        [InlineData("C:\\Project",                                  "C:\\Project\\Folder\\Source.cs",        " ",                                           "Folder")]
        [InlineData("C:\\Project",                                  "C:\\Project\\Source.cs",                "Folder\\Source.cs",                           "Folder")]
        [InlineData("C:\\Project",                                  "C:\\Project\\Source.cs",                "Folder\\SubFolder\\Source.cs",                "Folder", "SubFolder")]
        [InlineData("C:\\Project",                                  "C:\\Project\\Source.cs",                "Folder\\Source.cs ",                          "Folder")]
        [InlineData("C:\\Project",                                  "C:\\Project\\Source.cs",                "Folder\\SubFolder\\Source.cs ",               "Folder", "SubFolder")]
        [InlineData("C:\\Project",                                  "C:\\Source.cs",                         "Folder\\SubFolder\\Source.cs ",               "Folder", "SubFolder")]
        [InlineData("C:\\Project",                                  "C:\\Source.cs",                         "Folder\\SubFolder\\..\\Source.cs ",           "Folder")]
        [InlineData("C:\\Project",                                  "C:\\Source.cs",                         "Folder\\SubFolder\\Child\\..\\Source.cs ",    "Folder", "SubFolder")]
        [InlineData("C:\\Project",                                  "C:\\Source.cs",                         "Folder\\..\\Source.cs ",                       null)]
        [InlineData("C:\\Folder\\Project" ,                         "C:\\Folder\\Project\\Source.cs",        null,                                           null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Source.cs",                 null,                                           null)]
        [InlineData("C:\\Folder\\Project",                          "D:\\Source.cs",                         null,                                           null)]
        [InlineData("C:\\Folder\\Project",                          "\\Source.cs",                           null,                                           null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "..\\Source.cs",                                null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Source.cs",                 "..\\Source.cs",                                null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "\\Source.cs",                                  null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "..\\Folder\\Source.cs",                        null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Source.cs",                 "..\\Folder\\Source.cs",                        null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "\\Folder\\Source.cs",                          null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "Folder\\..\\..\\Source.cs",                    null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "D:\\Folder\\Source.cs",                        null)]
        [InlineData("C:\\Folder\\Project",                          "C:\\Folder\\Project\\Source.cs",        "C:\\Folder\\Project\\Source.cs",               null)]
        public void GetLogicalFolderNames_Returns(string basePath, string fullPath, string link, params string[] expected)
        {
            var metadata = ImmutableDictionary<string, string>.Empty;
            if (link != null)
            {
                metadata = metadata.SetItem(Compile.LinkProperty, link);
            }

            var result = FileItemServices.GetLogicalFolderNames(basePath, fullPath, metadata);

            Assert.Equal(expected, result);
        }
    }
}
