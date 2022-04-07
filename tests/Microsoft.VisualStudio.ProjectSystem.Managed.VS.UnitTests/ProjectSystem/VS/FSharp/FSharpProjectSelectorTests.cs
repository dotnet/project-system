// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.FSharp
{
    public class FSharpProjectSelectorTests
    {
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
