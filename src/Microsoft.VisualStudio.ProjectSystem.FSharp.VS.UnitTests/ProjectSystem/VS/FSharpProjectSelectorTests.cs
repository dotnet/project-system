// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class FSharpProjectSelectorTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new FSharpProjectSelector();
        }

        [Theory]
        [InlineData(@"<Project Sdk = ""FSharp.SDK""> </Project>", ProjectType.FSharp)]
        [InlineData(@"<Project ToolsVersion=""15.0""> </Project>", ProjectType.LegacyFSharp)]
        [InlineData(@"<Project Sdk = ""FSharp.SDK"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> </Project>", ProjectType.FSharp)]
        [InlineData(@"<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> </Project>", ProjectType.LegacyFSharp)]
        [InlineData(@"<Project> <Import Project=""Sdk.props"" Sdk=""FSharp.Sdk"" /> </Project>", ProjectType.FSharp)]
        [InlineData(@"<Project> <Import Project=""Sdk.props"" /> </Project>", ProjectType.LegacyFSharp)]
        [InlineData(@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> <Import Project=""Sdk.props"" Sdk=""FSharp.Sdk"" /> </Project>", ProjectType.FSharp)]
        [InlineData(@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""> <Import Project=""Sdk.props"" /> </Project>", ProjectType.LegacyFSharp)]

        public void GetProjectFactoryGuid(string projectFile, string expectedGuid)
        {
            var doc = XDocument.Parse(projectFile);
            FSharpProjectSelector.GetProjectFactoryGuid(doc, out var resultGuid);

            Assert.Equal(expectedGuid, resultGuid.ToString(), ignoreCase: true);
        }
    }
}
