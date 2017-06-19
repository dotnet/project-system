// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
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

        private static IProjectProperties emptyProjectProperties =
            IProjectPropertiesFactory.MockWithProperty(string.Empty).Object;


        [Fact]
        public static async Task GetPropertyTest_AllTargetsPresent()
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
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static async Task GetPropertyTest_PreBuildTargetPresent()
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
            var actual = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static async Task GetPropertyTest_PreBuildTargetPresent_LowerCase()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""prebuild"" BeforeTargets=""prebuildevent"">
    <Exec Command=""echo &quot;prebuild output&quot;"" />
  </Target>

</Project>
".AsProjectRootElement();
            var actual = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static async Task GetPropertyTest_NoTargetsPresent()
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
        public static async Task GetPropertyTest_ExistingProperties()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PreBuildEvent>echo $(ProjectDir)</PreBuildEvent>
  </PropertyGroup>

</Project>".AsProjectRootElement();
            var expected = "echo $(ProjectDir)";
            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue("PreBuildEvent", expected);
            var actual = await systemUnderTest.GetPropertyAsync(root, projectProperties);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task GetPropertyTest_WrongTargetName()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""PreeBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;prebuild output&quot;"" />
  </Target>

</Project>
".AsProjectRootElement();
            var result = await systemUnderTest.GetPropertyAsync(root, emptyProjectProperties);
            Assert.Null(result);
        }

        [Fact]
        public static async Task GetPropertyTest_WrongExec()
        {
            var root = @"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <Target Name=""PreBuild"" AfterTargets=""PreBuildEvent"">
    <Exec Commmand=""echo &quot;prebuild output&quot;"" />
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
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build output""", emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_TargetPresent()
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
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_TargetPresent_LowerCase()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""prebuild"" BeforeTargets=""prebuildevent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""prebuild"" BeforeTargets=""prebuildevent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
  </Target>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
  </Target>
  <Target Name=""PreBuild1"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"", emptyProjectProperties, root);

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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_DoNotRemoveTarget_EmptyString()
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
            await systemUnderTest.SetPropertyAsync(string.Empty, emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command="""" />
    <Exec Command=""echo &quot;pre build output&quot;"" />
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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync("       ", emptyProjectProperties, root);
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
        public static async Task SetPropertyTest_RemoveTarget_TabCharacter()
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
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>
".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync("\r\n", emptyProjectProperties, root);

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
        public static async Task SetPropertyTest_TargetNameCollision()
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
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"">
  </Target>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
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
  <Target Name=""PreBuild"">
  </Target>
  <Target Name=""PreBuild1"">
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreBuild"">
  </Target>
  <Target Name=""PreBuild1"">
  </Target>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
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
  <Target Name=""prebuild"">
  </Target>
  <Target Name=""prebuild1"">
  </Target>
</Project>".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""pre build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""prebuild"">
  </Target>
  <Target Name=""prebuild1"">
  </Target>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build $(OutDir)&quot;"" />
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

            var prebuildEventProjectProperties =
                IProjectPropertiesFactory.MockWithPropertyAndValue("PreBuildEvent", "echo $(ProjectDir)").Object;
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", prebuildEventProjectProperties, root);

            var expected = @"echo ""post build $(OutDir)""";
            var actual = await prebuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PreBuildEvent");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyTest_RemoveExistingProperties()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <PreBuildEvent>echo $(ProjectDir)</PreBuildEvent>
  </PropertyGroup>
</Project>".AsProjectRootElement();

            var prebuildEventProjectProperties =
                IProjectPropertiesFactory.CreateWithPropertyAndValue("PreBuildEvent", "echo $(ProjectDir)");
            await systemUnderTest.SetPropertyAsync(" ", prebuildEventProjectProperties, root);

            var result = await prebuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PreBuildEvent");
            Assert.Null(result);
        }

        [Fact]
        public static async Task SetPropertyTest_WrongTargetName()
        {
            var root = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreeBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
</Project>
".AsProjectRootElement();
            await systemUnderTest.SetPropertyAsync(@"echo ""post build $(OutDir)""", emptyProjectProperties, root);

            var expected = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
  <Target Name=""PreeBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;pre build output&quot;"" />
  </Target>
  <Target Name=""PreBuild"" BeforeTargets=""PreBuildEvent"">
    <Exec Command=""echo &quot;post build $(OutDir)&quot;"" />
  </Target>
</Project>";

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }
    }
}
