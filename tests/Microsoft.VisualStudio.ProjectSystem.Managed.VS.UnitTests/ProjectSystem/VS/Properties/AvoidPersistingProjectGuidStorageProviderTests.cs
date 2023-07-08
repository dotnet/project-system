// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class AvoidPersistingProjectGuidStorageProviderTests
    {
        [Theory]
        [InlineData(
            """
            <Project/>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                    <ProjectGuids>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuids>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <ItemGroup>
                    <ProjectGuid Include="{C26D43ED-ED18-46F9-8950-0B1A7232746E}" />
                </ItemGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <Target Name="Target">
                    <PropertyGroup>
                       <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                    </PropertyGroup>
                </Target>
            </Project>
            """
            )]
        public async Task GetProjectGuidAsync_WhenNoProjectGuidProperty_ReturnsEmpty(string projectXml)
        {
            var provider = CreateInstance(projectXml);

            var result = await provider.GetProjectGuidAsync();

            Assert.Equal(Guid.Empty, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Not a guid")]
        [InlineData("{42C4D8D7-3011-4EBA-AA8F-94AE42EFE399-Bar}")]
        [InlineData("{42C4D8D7-3011-4EBA- AA8F-94AE42EFE399}")]
        [InlineData("{ 0xFFFFFFFFFFFFFFFFFF,0xdddd,0xdddd,{ 0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd} }")]   // Overflow
        public async Task GetProjectGuidAsync_WhenInvalidProjectGuidProperty_ReturnsEmpty(string guid)
        {
            var projectXml =
                $"""
                <Project>
                    <PropertyGroup>
                        <ProjectGuid>{guid}</ProjectGuid>
                    </PropertyGroup>
                </Project>
                """;

            var provider = CreateInstance(projectXml);

            var result = await provider.GetProjectGuidAsync();

            Assert.Equal(Guid.Empty, result);
        }

        [Theory]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                    <projectguid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</projectguid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                </PropertyGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]

        [InlineData(
            """
            <Project>
                <ItemGroup>
                </ItemGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <ItemGroup>
                </ItemGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <ItemGroup>
                </ItemGroup>
                <PropertyGroup>
                   <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                   <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                 <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                 </PropertyGroup>
                 <PropertyGroup>
                    <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid> 
                 </PropertyGroup>
             </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup> 
                   <ProjectGuid Condition="'$(FalseCondition)' == 'true'">{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        public async Task GetProjectGuidAsync_ReturnsFirstProjectGuidIgnoringConditions(string projectXml)
        {
            var provider = CreateInstance(projectXml);

            var result = await provider.GetProjectGuidAsync();

            Assert.Equal(new Guid("C26D43ED-ED18-46F9-8950-0B1A7232746E"), result);
        }

        [Theory]
        [InlineData("c26d43eded1846f989500b1a7232746e")]
        [InlineData("C26D43EDED1846F989500B1A7232746E")]
        [InlineData("c26d43ed-ed18-46f9-8950-0b1a7232746e")]
        [InlineData("C26D43ED-ED18-46F9-8950-0B1A7232746E")]
        [InlineData("{c26d43ed-ed18-46f9-8950-0b1a7232746e}")]
        [InlineData("{c26d43ed-ed18-46f9-8950-0b1a7232746e} ")]
        [InlineData("{C26D43ED-ED18-46F9-8950-0B1A7232746E}")]
        [InlineData("(c26d43ed-ed18-46f9-8950-0b1a7232746e)")]
        [InlineData("(C26D43ED-ED18-46F9-8950-0B1A7232746E)")]
        [InlineData("{0xc26d43ed,0xed18,0x46f9,{0x89,0x50,0x0b,0x1a,0x72,0x32,0x74,0x6e}}")]
        [InlineData("{0XC26D43ED,0XED18,0X46F9,{0X89,0X50,0X0B,0X1A,0X72,0X32,0X74,0X6E}}")]
        [InlineData(" C26D43ED-ED18-46F9-8950-0B1A7232746E")]
        [InlineData("C26D43ED-ED18-46F9-8950-0B1A7232746E ")]
        [InlineData("C26D43EDED1846F989500B1%417232746E")] // With escaped characters
        public async Task GetProjectGuidAsync_ReturnsProjectGuid(string guid)
        {
            var projectXml =
                $"""
                <Project>
                    <PropertyGroup>
                        <ProjectGuid>{guid}</ProjectGuid>
                    </PropertyGroup>
                </Project>
                """;

            var provider = CreateInstance(projectXml);

            var result = await provider.GetProjectGuidAsync();

            Assert.Equal(new Guid("C26D43ED-ED18-46F9-8950-0B1A7232746E"), result);
        }

        [Theory]
        [InlineData(
            """
            <Project>
                 <PropertyGroup>
                     <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                 </PropertyGroup>
             </Project>
            """,
            """
            <Project>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                    <projectguid>{D110509C-066B-434E-B456-15B71F0DA330}</projectguid>
                </PropertyGroup>
            </Project>
            """,
            """
            <Project>
                <PropertyGroup>
                    <projectguid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</projectguid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                </PropertyGroup>
                <PropertyGroup>
                    <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """,
            """
            <Project>
                <PropertyGroup>
                </PropertyGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                 <ItemGroup>
                 </ItemGroup>
                 <PropertyGroup>
                     <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                 </PropertyGroup>
             </Project>
            """,
            """
            <Project>
                <ItemGroup>
                </ItemGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                 <ItemGroup>
                 </ItemGroup>
                 <PropertyGroup>
                     <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                     <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                 </PropertyGroup>
             </Project>
            """,
            """
            <Project>
                <ItemGroup>
                </ItemGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                    <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                </PropertyGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid> 
                </PropertyGroup>
            </Project>
            """,
            """
            <Project>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
                <PropertyGroup>
                    <ProjectGuid>{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid> 
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                 <PropertyGroup> 
                    <ProjectGuid Condition="'$(FalseCondition)' == 'true'">{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                 </PropertyGroup>
             </Project>
            """,
            """
            <Project>
                <PropertyGroup> 
                   <ProjectGuid Condition="'$(FalseCondition)' == 'true'">{C26D43ED-ED18-46F9-8950-0B1A7232746E}</ProjectGuid>
                </PropertyGroup>
            </Project>
            """
            )]
        public async Task SetProjectGuidAsync_SetsFirstProjectGuidIgnoringConditions(string input, string expected)
        {
            var result = ProjectRootElementFactory.Create(input);
            var provider = CreateInstance(result);

            await provider.SetProjectGuidAsync(new Guid("{C26D43ED-ED18-46F9-8950-0B1A7232746E}"));

            MSBuildAssert.AssertProjectXml(expected, result);
        }

        [Theory]
        [InlineData("c26d43eded1846f989500b1a7232746e")]
        [InlineData("C26D43EDED1846F989500B1A7232746E")]
        [InlineData("c26d43ed-ed18-46f9-8950-0b1a7232746e")]
        [InlineData("C26D43ED-ED18-46F9-8950-0B1A7232746E")]
        [InlineData("{c26d43ed-ed18-46f9-8950-0b1a7232746e}")]
        [InlineData("{c26d43ed-ed18-46f9-8950-0b1a7232746e} ")]
        [InlineData("{C26D43ED-ED18-46F9-8950-0B1A7232746E}")]
        [InlineData("(c26d43ed-ed18-46f9-8950-0b1a7232746e)")]
        [InlineData("(C26D43ED-ED18-46F9-8950-0B1A7232746E)")]
        [InlineData("{0xc26d43ed,0xed18,0x46f9,{0x89,0x50,0x0b,0x1a,0x72,0x32,0x74,0x6e}}")]
        [InlineData("{0XC26D43ED,0XED18,0X46F9,{0X89,0X50,0X0B,0X1A,0X72,0X32,0X74,0X6E}}")]
        [InlineData(" C26D43ED-ED18-46F9-8950-0B1A7232746E")]
        [InlineData("C26D43ED-ED18-46F9-8950-0B1A7232746E ")]
        [InlineData("C26D43EDED1846F989500B1%417232746E")] // With escaped characters
        public async Task SetProjectGuidAsync_WhenProjectGuidPropertyAlreadyHasSameGuid_DoesNotSet(string guid)
        {
            var projectXml =
                $"""
                <Project>
                    <PropertyGroup>
                        <ProjectGuid>{guid}</ProjectGuid>
                    </PropertyGroup>
                </Project>
                """;

            var result = ProjectRootElementFactory.Create(projectXml);
            var provider = CreateInstance(result);

            await provider.SetProjectGuidAsync(new Guid("{C26D43ED-ED18-46F9-8950-0B1A7232746E}"));

            MSBuildAssert.AssertProjectXml(projectXml, result);
        }

        [Theory]
        [InlineData(
            """
            <Project/>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <PropertyGroup>
                    <ProjectGuids>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuids>
                </PropertyGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <ItemGroup>
                    <ProjectGuid Include="{D110509C-066B-434E-B456-15B71F0DA330}" />
                </ItemGroup>
            </Project>
            """
            )]
        [InlineData(
            """
            <Project>
                <Target Name="Target">
                    <PropertyGroup>
                       <ProjectGuid>{D110509C-066B-434E-B456-15B71F0DA330}</ProjectGuid>
                    </PropertyGroup>
                </Target>
            </Project>
            """
            )]
        public async Task SetProjectGuidAsync_WhenNoProjectGuidProperty_DoesNotSet(string projectXml)
        {
            var result = ProjectRootElementFactory.Create(projectXml);
            var provider = CreateInstance(result);

            await provider.SetProjectGuidAsync(new Guid("{C26D43ED-ED18-46F9-8950-0B1A7232746E}"));

            MSBuildAssert.AssertProjectXml(projectXml, result);
        }

        private static AvoidPersistingProjectGuidStorageProvider CreateInstance(string projectXml)
        {
            return CreateInstance(ProjectRootElementFactory.Create(projectXml));
        }

        private static AvoidPersistingProjectGuidStorageProvider CreateInstance(ProjectRootElement projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return new AvoidPersistingProjectGuidStorageProvider(projectAccessor, UnconfiguredProjectFactory.Create());
        }
    }
}
