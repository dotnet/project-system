// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class PreBuildEventValueProviderTests
    {
        private static PreBuildEventValueProvider.PreBuildEventHelper systemUnderTest =
            new PreBuildEventValueProvider.PreBuildEventHelper();

        [Fact]
        public static void GetPropertyTest_AllTargetsPresent()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;prebuild output&quot;"" />
  </Target>

  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>

</Project>
".AsProjectRootElement();
            var actual = systemUnderTest.GetProperty(root);
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static void GetPropertyTest_PreBuildTargetPresent()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;prebuild output&quot;"" />
  </Target>

</Project>
".AsProjectRootElement();
            var actual = systemUnderTest.GetProperty(root);
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static void GetPropertyTest_NoTargetsPresent()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

</Project>
".AsProjectRootElement();
            var actual = systemUnderTest.GetProperty(root);
            Assert.Null(actual);
        }

        [Fact]
        public static void GetPropertyTest_ExistingProperties()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PreBuildEvent>echo $(ProjectDir)</PreBuildEvent>
  </PropertyGroup>

</Project>".AsProjectRootElement();
            var actual = systemUnderTest.GetProperty(root);
            Assert.Equal(@"echo $(ProjectDir)", actual);
        }

        [Fact]
        public static void SetPropertyTest_NoTargetsPresent()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build output""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_TargetPresent()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_TargetPresent_NoTasks()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
  </Target>
  <Target Name=""PreBuild1"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_TargetPresent_MultipleTasks()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_DoNotRemoveTarget_EmptyString()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>
".AsProjectRootElement();
            systemUnderTest.SetProperty(string.Empty, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_RemoveTarget_EmptyString()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(string.Empty, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_RemoveTarget_WhitespaceCharacter()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty("       ", root);
            var stringWriter = new System.IO.StringWriter();
            root.Save(stringWriter);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_RemoveTarget_TabCharacter()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty("\t\t\t", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_RemoveTarget_NewlineCharacter()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>
".AsProjectRootElement();
            systemUnderTest.SetProperty("\r\n", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""&#xD;&#xA;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_TargetNameCollision()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"">
  </Target>
</Project>
".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"">
  </Target>
  <Target Name=""PreBuild1"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_TargetNameCollision02()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"">
  </Target>
  <Target Name=""PreBuild1"">
  </Target>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"">
  </Target>
  <Target Name=""PreBuild1"">
  </Target>
  <Target Name=""PreBuild2"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyTest_ExistingProperties()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PreBuildEvent>echo $(ProjectDir)</PreBuildEvent>
    <PostBuildEvent>echo $(ProjectDir)</PostBuildEvent>
  </PropertyGroup>
</Project>".AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PreBuildEvent>echo ""post build $(OutDir)""</PreBuildEvent>
    <PostBuildEvent>echo $(ProjectDir)</PostBuildEvent>
  </PropertyGroup>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }
    }
}
