// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.VisualStudio.Packaging;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class FSharpProjectSelectorTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            var selector = new FSharpProjectSelector();
        }

        [Theory]
        [InlineData(@"<Project Sdk = ""FSharp.SDK""> </Project>", FSharpProjectSystemPackage.ProjectTypeGuid)]
        [InlineData(@"<Project ToolsVersion=""15.0""> </Project>", FSharpProjectSystemPackage.LegacyProjectTypeGuid)]
        [InlineData(@"<Project Sdk = ""FSharp.SDK"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> </Project>", FSharpProjectSystemPackage.ProjectTypeGuid)]
        [InlineData(@"<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> </Project>", FSharpProjectSystemPackage.LegacyProjectTypeGuid)]
        [InlineData(@"<Project> <Import Project=""Sdk.props"" Sdk=""FSharp.Sdk"" /> </Project>", FSharpProjectSystemPackage.ProjectTypeGuid)]
        [InlineData(@"<Project> <Import Project=""Sdk.props"" /> </Project>", FSharpProjectSystemPackage.LegacyProjectTypeGuid)]
        [InlineData(@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> <Import Project=""Sdk.props"" Sdk=""FSharp.Sdk"" /> </Project>", FSharpProjectSystemPackage.ProjectTypeGuid)]
        [InlineData(@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> <Import Project=""Sdk.props"" /> </Project>", FSharpProjectSystemPackage.LegacyProjectTypeGuid)]

        public void GetProjectFactoryGuid(string projectFile, string expectedGuid)
        {
            var selector = new FSharpProjectSelector();
            XDocument doc = XDocument.Parse(projectFile);
            FSharpProjectSelector.GetProjectFactoryGuid(doc, out var resultGuid);

            Assert.Equal(expectedGuid, resultGuid.ToString(), ignoreCase:true);
        }
    }
}
