// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public static class PreBuildEventValueProviderTests
    {
        private static readonly PreBuildEventValueProvider.PreBuildEventHelper systemUnderTest =
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
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static void GetPropertyAsync_PreBuildTargetPresent()
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
                </Project>
                """.AsProjectRootElement();
            var actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(@"echo ""prebuild output""", actual);
        }

        [Fact]
        public static void GetPropertyAsync_PreBuildTargetPresent_LowerCase()
        {
            var root =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="prebuild" BeforeTargets="prebuildevent">
                    <Exec Command="echo &quot;prebuild output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            var actual = systemUnderTest.TryGetValueFromTarget(root);
            Assert.Equal(@"echo ""prebuild output""", actual);
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
        public static async Task GetPropertyAsync_ExistingPropertiesAsync()
        {
            var expected = "echo $(ProjectDir)";
            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue("PreBuildEvent", expected);
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
                  <Target Name="PreeBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;prebuild output&quot;" />
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
                  <Target Name="PreBuild" AfterTargets="PreBuildEvent">
                    <Exec Commmand="echo &quot;prebuild output&quot;" />
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
            systemUnderTest.SetProperty(@"echo ""pre build output""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
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
                  <Target Name="prebuild" BeforeTargets="prebuildevent">
                    <Exec Command="echo &quot;pre build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="prebuild" BeforeTargets="prebuildevent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                  </Target>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                  </Target>
                  <Target Name="PreBuild1" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                  </Target>
                  <Target Name="PreBuild1" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
                  </Target>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
                    <Exec Command="echo &quot;pre build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="" />
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty("       ", root);
            var stringWriter = new System.IO.StringWriter();
            root.Save(stringWriter);

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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
                  <Target Name="PreBuild">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PreBuild">
                  </Target>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
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
                  <Target Name="PreBuild">
                  </Target>
                  <Target Name="PreBuild1">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="PreBuild">
                  </Target>
                  <Target Name="PreBuild1">
                  </Target>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
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
                  <Target Name="prebuild">
                  </Target>
                  <Target Name="prebuild1">
                  </Target>
                </Project>
                """.AsProjectRootElement();
            systemUnderTest.SetProperty(@"echo ""pre build $(OutDir)""", root);

            var expected =
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>netcoreapp1.1</TargetFramework>
                  </PropertyGroup>
                  <Target Name="prebuild">
                  </Target>
                  <Target Name="prebuild1">
                  </Target>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build $(OutDir)&quot;" />
                  </Target>
                </Project>
                """;

            var actual = root.SaveAndGetChanges();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyAsync_ExistingProperties()
        {
            var prebuildEventProjectProperties =
                IProjectPropertiesFactory.MockWithPropertyAndValue("PreBuildEvent", "echo $(ProjectDir)").Object;
            var success = await systemUnderTest.TrySetPropertyAsync(@"echo ""post build $(OutDir)""", prebuildEventProjectProperties);
            Assert.True(success);

            var expected = @"echo ""post build $(OutDir)""";
            var actual = await prebuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PreBuildEvent");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static async Task SetPropertyAsync_RemoveExistingProperties()
        {
            var prebuildEventProjectProperties =
                IProjectPropertiesFactory.CreateWithPropertyAndValue("PreBuildEvent", "echo $(ProjectDir)");
            var success = await systemUnderTest.TrySetPropertyAsync(" ", prebuildEventProjectProperties);
            Assert.True(success);

            var result = await prebuildEventProjectProperties.GetUnevaluatedPropertyValueAsync("PreBuildEvent");
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
                  <Target Name="PreeBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
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
                  <Target Name="PreeBuild" BeforeTargets="PreBuildEvent">
                    <Exec Command="echo &quot;pre build output&quot;" />
                  </Target>
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
                  <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
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
