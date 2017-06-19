// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class PostBuildEventValueProviderTests
    {
        private static PostBuildEventValueProvider.PostBuildEventHelper systemUnderTest =
            new PostBuildEventValueProvider.PostBuildEventHelper();

        private static IProjectProperties emptyProjectProperties =
            IProjectPropertiesFactory.MockWithProperty(string.Empty).Object;

        [Fact]
        public static async Task GetPropertyTest_AllTargetsPresentAsync()
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
            var actual = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Equal(@"echo ""post build output""", actual);
        }

        [Fact]
        public static async Task GetPropertyTest_PostBuildTargetPresentAsync()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>

</Project>
".AsProjectRootElement();
            var actual = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Equal(@"echo ""post build output""", actual);
        }

        [Fact]
        public static async Task GetPropertyTest_PostBuildTargetPresent_LowerCaseAsync()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""postbuild"" AfterTargets=""postbuildevent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>

</Project>
".AsProjectRootElement();
            var actual = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Equal(@"echo ""post build output""", actual);
        }

        [Fact]
        public static async Task GetPropertyTest_NoTargetsPresentAsync()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

</Project>
".AsProjectRootElement();
            var actual = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Null(actual);
        }

        [Fact]
        public static async Task GetPropertyTest_ExistingPropertiesAsync()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PostBuildEvent>echo $(ProjectDir)</PostBuildEvent>
  </PropertyGroup>

</Project>
".AsProjectRootElement();

            var expected = "echo $(ProjectDir)";
            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue("PostBuildEvent", expected);
            var actual = await systemUnderTest.GetPropertyAsync(root, projectProperties);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task GetPropertyTest_WrongTargetNameAsync()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PoostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>
".AsProjectRootElement();
            var result = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Null(result);
        }

        [Fact]
        public static async Task SetPropertyTest_NoTargetsPresent()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build output""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetPresent()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetPresent_LowerCase()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""postbuild"" AfterTargets=""postbuildevent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""postbuild"" AfterTargets=""postbuildevent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetPresent_NoTasks()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
  </Target>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetPresent_NoTasks_Removal()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
  </Target>
  <Target Name=""PostBuild1"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
  </Target>
  <Target Name=""PostBuild1"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command="""" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetPresent_MultipleTasks()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_DoNotRemoveTarget_EmptyString()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(string.Empty, emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command="""" />
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_RemoveTarget_EmptyString()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(string.Empty, emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_RemoveTarget_WhitespaceCharacter()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync("       ", emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_RemoveTarget_TabCharacter()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync("\t\t\t", emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_DoNotRemoveTarget_NewlineCharacter()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync("\r\n", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""&#xD;&#xA;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetNameCollision()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"">
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"">
  </Target>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetNameCollision02()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"">
  </Target>
  <Target Name=""PostBuild1"">
  </Target>
</Project>
".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PostBuild"">
  </Target>
  <Target Name=""PostBuild1"">
  </Target>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_TargetNameCollision_LowerCase()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""postBuild"">
  </Target>
  <Target Name=""postBuild1"">
  </Target>
</Project>
".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""postBuild"">
  </Target>
  <Target Name=""postBuild1"">
  </Target>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_ExistingProperties()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PreBuildEvent>echo $(ProjectDir)</PreBuildEvent>
    <PostBuildEvent>echo $(ProjectDir)</PostBuildEvent>
  </PropertyGroup>
</Project>".AsProjectRootElement();

            var postbuildEventProjectProperties =
                IProjectPropertiesFactory.MockWithPropertyAndValue("PostBuildEvent", "echo $(ProjectDir)").Object;
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", postbuildEventProjectProperties, root);

            var expected = @"echo ""post build $(OutDir)""";
            var actual = await postbuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PostBuildEvent");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_WrongTargetName()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PoostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PoostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build output&quot;"" />
  </Target>
  <Target Name=""PostBuild"" AfterTargets=""PostBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }
    }
}
