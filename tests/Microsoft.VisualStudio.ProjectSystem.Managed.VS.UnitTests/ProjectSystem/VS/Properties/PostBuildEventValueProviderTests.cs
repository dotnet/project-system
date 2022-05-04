// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public static class PostBuildEventValueProviderTests
    {
        private static readonly PostBuildEventValueProvider.PostBuildEventHelper systemUnderTest =
            new();

        [Fact]
        public static void GetPropertyAsync_AllTargetsPresent()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>

                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;prebuild output&quot;" />
                  </Target>

                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>

                </Project>
                """.AsProjectRootElement();
            var actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(@"echo ""post build output""", actual);
        }

        [Fact]
        public static void GetPropertyAsync_PostBuildTargetPresent()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>

                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>

                </Project>
                """.AsProjectRootElement();
            var actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(@"echo ""post build output""", actual);
        }

        [Fact]
        public static void GetPropertyAsync_PostBuildTargetPresent_LowerCase()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>

                  <Target Name="postbuild" AfterTargets="postbuildevent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>

                </Project>
                """.AsProjectRootElement();
            var actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(@"echo ""post build output""", actual);
        }

        [Fact]
        public static void GetPropertyAsync_NoTargetsPresent()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>

                </Project>
                """.AsProjectRootElement();
            var actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Null(actual);
        }

        [Fact]
        public static async Task GetPropertyAsync_ExistingProperties()
        {
            var expected = "echo $(ProjectDir)";
            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue("PostBuildEvent", expected);
            var (success, actual) = await systemUnderTest.TryGetUnevaluatedPropertyValueAsync(projectProperties);
            Assert.True(success);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetPropertyAsync_WrongTargetName()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PoostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            var result = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Null(result);
        }

        [Fact]
        public static void GetPropertyAsync_WrongExec()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Commmand="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            var result = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Null(result);
        }

        [Fact]
        public static void SetPropertyAsync_NoTargetsPresent()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build output""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetPresent()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetPresent_LowerCase()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="postbuild" AfterTargets="postbuildevent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="postbuild" AfterTargets="postbuildevent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetPresent_NoTasks()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                  </Target>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetPresent_NoTasks_Removal()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                  </Target>
                  <Target Name="PostBuild1" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                  </Target>
                  <Target Name="PostBuild1" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetPresent_MultipleTasks()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_DoNotRemoveTarget_EmptyString()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(string.Empty, root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="" />
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_RemoveTarget_EmptyString()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(string.Empty, root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_RemoveTarget_WhitespaceCharacter()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty("       ", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_RemoveTarget_TabCharacter()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty("\t\t\t", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_DoNotRemoveTarget_NewlineCharacter()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty("\r\n", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="&#xD;&#xA;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetNameCollision()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild">
                  </Target>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetNameCollision02()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild">
                  </Target>
                  <Target Name="PostBuild1">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild">
                  </Target>
                  <Target Name="PostBuild1">
                  </Target>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void SetPropertyAsync_TargetNameCollision_LowerCase()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="postBuild">
                  </Target>
                  <Target Name="postBuild1">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="postBuild">
                  </Target>
                  <Target Name="postBuild1">
                  </Target>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyAsync_ExistingProperties()
        {
            var postbuildEventProjectProperties =
                IProjectPropertiesFactory.MockWithPropertyAndValue("PostBuildEvent", "echo $(ProjectDir)").Object;
            var success = await systemUnderTest.TrySetPropertyAsync(@"echo ""post build $(OutDir)""", postbuildEventProjectProperties);
            Assert.True(success);

            var expected = @"echo ""post build $(OutDir)""";
            var actual = await postbuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PostBuildEvent");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyAsync_RemoveExistingProperties()
        {
            var postbuildEventProjectProperties =
                IProjectPropertiesFactory.CreateWithPropertyAndValue("PostBuildEvent", "echo $(ProjectDir)");
            var success = await systemUnderTest.TrySetPropertyAsync(" ", postbuildEventProjectProperties);
            Assert.True(success);

            var result = await postbuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PostBuildEvent");
            Assert.Null(result);
        }

        [Fact]
        public static void SetPropertyAsync_WrongTargetName()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PoostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""post build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PoostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build output&quot;" />
                  </Target>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo &quot;post build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void EscapeValue_Read_CheckEscaped()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo %25DATE%" />
                  </Target>
                </Project>
                """.AsProjectRootElement();

            const string expected = "echo %DATE%";
            string? actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void EscapeValue_Read_CheckNotDoubleEscaped()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo %2525DATE%" />
                  </Target>
                </Project>
                """.AsProjectRootElement();

            const string expected = "echo %25DATE%";
            string? actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void EscapeValue_Write_CheckEscaped()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                </Project>
                """.AsProjectRootElement();

            const string expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo %25DATE%25" />
                  </Target>
                </Project>
                """;

            systemUnderTest.SetProperty("echo %DATE%", root);
            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void EscapeValue_Write_CheckNotDoubleEscaped()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                </Project>
                """.AsProjectRootElement();

            const string expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
                    <Exec Command="echo %2525DATE%25" />
                  </Target>
                </Project>
                """;

            systemUnderTest.SetProperty("echo %25DATE%", root);
            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }
    }
}
