// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    public class ConfigurationDimensionProviderTests
    {
        [Theory]
        [InlineData(
// Solution      Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup Condition=""'$(Random)'==''"">
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""'$(Random)'==''"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Platforms></Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Platforms>;</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Platforms>x64</Platforms>
        <Platforms></Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Platforms>x64</Platforms>
    </PropertyGroup>
    <PropertyGroup>
        <Platforms></Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Configuration></Configuration>
        <Platform></Platform>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,           "Release",                     "ARM",               null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == '|' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|ARM' "" />
</Project>
")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_IgnoresEmptyProjectDimensions(string solutionConfiguration, string configuration, string platform, string targetFramework, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configuration, platform, targetFramework, projectXml);
        }

        [Theory]
        [InlineData(
// Solution      Configuration (Expected)       Platform (Expected)      TargetFramework (Expected)
null,            new[] { "Release" } ,          new[] { "x64" },      null,
@"
<Project>
    <PropertyGroup>
        <Platforms>x64</Platforms>
        <Platform>Singular</Platform>
        <Configurations>Release</Configurations>
        <Configuration>Singular</Configuration>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|ARM' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|ARM' "" />
</Project>
")]
        [InlineData(
null,            new[] { "Debug", "Release" } ,           new[] { "AnyCPU", "ARM" },      null,
@"
<Project>
    <PropertyGroup>
        <Platform>Singular</Platform>
        <Configuration>Singular</Configuration>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|ARM' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|ARM' "" />
</Project>
")]
        [InlineData(
null,            new[] { "Debug", "Release" } ,           new[] { "AnyCPU", "ARM" },      null,
@"
<Project>
    <PropertyGroup>
        <Platform>Singular</Platform>
        <Configuration>Singular</Configuration>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "" />    
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|ARM' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|ARM' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'debug|anyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'debug|anyCPU' "" />
    <PropertyGroup Condition="" '$(TargetFramework)' == 'net45' "" />
</Project>
")]
        [InlineData(
null,            new[] { "Singular" } ,           new[] { "Singular" },   null,
@"
<Project>
    <PropertyGroup>
        <Platform>Singular</Platform>
        <Configuration>Singular</Configuration>
    </PropertyGroup>
</Project>
")]
        public async Task MethodsAgreeOnSpecificOrder(string solutionConfiguration, string[] configurations, string[] platforms, string[] targetFrameworks, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configurations[0], platforms[0], targetFrameworks?[0], projectXml);
            
            await VerifyGetProjectConfigurationDimensionsAsync(configurations, platforms, targetFrameworks, projectXml);
        }

        [Theory]
        [InlineData(
// Solution      Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup Condition="""">
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition="""">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup Condition=""true"">
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""true"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
                [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""TRUE"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""'$(OS)' == 'Windows_NT'"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
                [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""'$(OS)' == 'windows_nt'"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""'$(BuildingInsideVisualStudio)' == 'true'"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition="" '$(BuildingInsideVisualStudio)' == 'true' "">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup Condition="" '$(BuildingInsideVisualStudio)' == 'true' "">
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition=""'$(Platforms)' == ''"">x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms Condition="" '$(Platforms)' == '' "">x64</Platforms>
    </PropertyGroup>
</Project>
")]
                [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup Condition=""'$(Platforms)' == ''"">
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms></Platforms>
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms>;</Platforms>
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms></Platforms>
    </PropertyGroup>
    <PropertyGroup>
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_TriesToMimicEvaluation(string solutionConfiguration, string configuration, string platform, string targetFramework, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configuration, platform, targetFramework, projectXml);
        }

        [Theory]
        [InlineData(
// Solution      Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
null,            "Release",                      "x64",                "net45",
@"
<Project>
    <PropertyGroup>
        <Configurations>Release</Configurations>
        <Platforms>x64</Platforms>
        <TargetFrameworks>net45</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                      "x64",                "net45",
@"
<Project>
    <PropertyGroup>
        <Configurations>Release;Debug</Configurations>
        <Platforms>x64;x86</Platforms>
        <TargetFrameworks>net45;net46</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                      "x64",                "net45",
@"
<Project>
    <PropertyGroup>
        <Configurations>;Release;Debug</Configurations>
        <Platforms>$(Foo);x64;x86</Platforms>
        <TargetFrameworks>; ; net45;net46</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,             "Release",                     "x64",               null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|x64' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|ARM' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|ARM' "" />
</Project>
")]
        [InlineData(
null,             "Debug",                      "AnyCPU",               null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|x64|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|x64|net45' "" />
</Project>
")]
         [InlineData(
null,             "Release",                     "x64",               null,
@"
<Project>
    <PropertyGroup>
        <DebugSymbols Condition="" '$(Configuration)|$(Platform)' == 'Release|x64' "" />
        <DebugSymbols Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
        <DebugSymbols Condition="" '$(Configuration)|$(Platform)' == 'Debug|ARM' "" />
        <DebugSymbols Condition="" '$(Configuration)|$(Platform)' == 'Release|ARM' "" />
    </PropertyGroup>
</Project>
")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenDimensionsSpecifiedInProject_ReturnsThem(string solutionConfiguration, string configuration, string platform, string targetFramework, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configuration, platform, targetFramework, projectXml);
        }

        [Theory]
        [InlineData(
// Solution      Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
</Project>
")]
        [InlineData(
null,            "Debug",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                    "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Configurations>Release</Configurations>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
       [InlineData(
null,            "Debug",                      "AnyCPU",                "net45",
@"
<Project>
    <PropertyGroup>
        <TargetFrameworks>net45</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenProjectsAreMissingDimensions_ReturnsDefaults(string solutionConfiguration, string configuration, string platform, string targetFramework, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configuration, platform, targetFramework, projectXml);
        }

        [Theory]
        [InlineData(
// Solution      Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
null,            "Release",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Configuration>Release</Configuration>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                      "x86",                null,
@"
<Project>
    <PropertyGroup>
        <Configuration>Release</Configuration>
        <Platform>x86</Platform>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
        <Configuration Condition="" '$(Configuration)' == '' "">Release</Configuration>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                      "AnyCPU",                null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)' == '' "">
        <Configuration>Release</Configuration>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                       "x86",                     null,
@"
<Project>
    <PropertyGroup>
        <Platform Condition="" '$(Platform)' == '' "">x86</Platform>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x86",                     null,
@"
<Project>
    <PropertyGroup Condition="" '$(Platform)' == '' "">
        <Platform>x86</Platform>
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Debug",                      "x86",                     null,
@"
<Project>
    <PropertyGroup Condition="" '$(Platform)' == '' "">
        <Platform>x86</Platform>
        <Configuration>Release</Configuration> <!-- Will be ignored -->
    </PropertyGroup>
</Project>
")]
        [InlineData(
null,            "Release",                      "x86",                     null,
@"
<Project>
    <PropertyGroup>
        <Platform>x86</Platform>
        <Configuration>Release</Configuration>
    </PropertyGroup>
</Project>
")]
                [InlineData(
null,            "Release",                      "x86",                     null,
@"
<Project>
    <PropertyGroup>
        <Platform>x86</Platform>
        <Configuration>Release</Configuration>
        <TargetFramework>net45</TargetFramework>    <!-- We never respect singular TFM  -->
    </PropertyGroup>
</Project>
")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenProjectsAreMissingDimensions_ReturnsSingular(string solutionConfiguration, string configuration, string platform, string targetFramework, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configuration, platform, targetFramework, projectXml);
        }

        [Theory]
        [InlineData(
// Solution      Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
"Release",       "Release",                    "AnyCPU",                null,
@"
<Project>
</Project>
")]
        [InlineData(
"Release|",      "Release",                    "AnyCPU",                null,
@"
<Project>
    <PropertyGroup>
    </PropertyGroup>
</Project>
")]
        [InlineData(
"Debug|x64",     "Release",                    "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Configurations>Release</Configurations>
    </PropertyGroup>
</Project>
")]
        [InlineData(
"Release|x86",   "Release",                    "x64",                   null,
@"
<Project>
    <PropertyGroup>
        <Platforms>x64</Platforms>
    </PropertyGroup>
</Project>
")]
       [InlineData(
"Release|x64",   "Release",                    "x64",                   "net45",
@"
<Project>
    <PropertyGroup>
        <TargetFrameworks>net45</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenProjectsAreMissingDimensions_ReturnsSolutionConfiguration(string solutionConfiguration, string configuration, string platform, string targetFramework, string projectXml)
        {
            await VerifyGetBestGuessDefaultValuesForDimensionsAsync(solutionConfiguration, configuration, platform, targetFramework, projectXml);
        }

        [Theory]
        [InlineData(
// Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
new[] { "Release" },             new[] { "x64" },         new[] { "net45" },
@"
<Project>
    <PropertyGroup>
        <Configurations>Release</Configurations>
        <Platforms>x64</Platforms>
        <TargetFrameworks>net45</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Release", "Debug" },    new[] { "x64", "x86" },   new[] { "net45", "net46" },
@"
<Project>
    <PropertyGroup>
        <Configurations>Release;Debug</Configurations>
        <Platforms>x64;x86</Platforms>
        <TargetFrameworks>net45;net46</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Release", "Debug" },    new[] { "x64", "x86" },   new[] { "net45", "net46" },
@"
<Project>
    <PropertyGroup>
        <Configurations>;Release;Debug</Configurations>
        <Platforms>$(ShouldNotExist);x64;x86</Platforms>
        <TargetFrameworks>; ; net45;net46;</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64", "x86" },   new[] { "net46" },
@"
<Project>
    <PropertyGroup>
        <Configurations>Debug;Release;debug;release</Configurations>
        <Platforms>AnyCPU; x64 ; x86; anycpu</Platforms>
    </PropertyGroup>
    <PropertyGroup>
        <TargetFrameworks>net46</TargetFrameworks>
    </PropertyGroup>
</Project>
")]

        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|x64' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|x64' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|x64|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|x64|net45' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           new[] { "net46" },
@"
<Project>
    <PropertyGroup>
        <TargetFrameworks>net46</TargetFrameworks>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|x64|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|x64|net45' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" }, new[] { "AnyCPU", "x64" },              null,
@"
<Project>
    <PropertyGroup>
        <TargetFrameworks></TargetFrameworks>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|x64|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|x64|net45' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == '||' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|AnyCPU|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|x64|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|x64|net45' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|AnyCPU|net46' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|AnyCPU|net46' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Debug|x64|net46' "" />
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)|$(TargetFramework)' == 'Release|x64|net46' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)' == 'Debug' "" />
    <PropertyGroup Condition="" '$(Configuration)' == 'Release' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'AnyCPU' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'x64' "" />
    <PropertyGroup Condition="" '$(TargetFramework)' == 'net45' "" />
    <PropertyGroup Condition="" '$(TargetFramework)' == 'net46' "" />
</Project>
")]
                [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           null,
@"
<Project>
    <PropertyGroup Condition="" '$(Configuration)' == 'Debug' "" />
    <PropertyGroup Condition="" '$(Configuration)' == 'Release' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'AnyCPU' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'anycpu' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'x64' "" />
    <PropertyGroup Condition="" '$(TargetFramework)' == 'net45' "" />
    <PropertyGroup Condition="" '$(TargetFramework)' == 'net46' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },    new[] { "AnyCPU", "x64" },           null,
@"
<Project>
    <PropertyGroup>
        <Configurations>Debug;Release</Configurations>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Platform)' == 'AnyCPU' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'x64' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },   new[] { "x86" },                      null,
@"
<Project>
    <PropertyGroup>
        <Configurations>Debug;Release</Configurations>
        <Platforms>x86</Platforms>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Platform)' == 'AnyCPU' "" />
    <PropertyGroup Condition="" '$(Platform)' == 'x64' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },   new[] { "AnyCPU" },                   null,
@"
<Project>
    <PropertyGroup>
        <Configurations>Debug;Release</Configurations>
    </PropertyGroup>
    <PropertyGroup>
        <DebugSymbols Condition=""'$(Platform)' == 'AnyCPU'"" />
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },   new[] { "AnyCPU" },                   null,
@"
<Project>
    <PropertyGroup>
        <Configurations></Configurations>
    </PropertyGroup>
    <PropertyGroup>
        <DebugSymbols Condition=""'$(Platform)' == 'AnyCPU'"" />
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)' == 'Debug' "" />
    <PropertyGroup Condition="" '$(Configuration)' == 'Release' "" />
</Project>
")]
        [InlineData(
new[] { "Debug", "Release" },   new[] { "AnyCPU" },                   null,
@"
<Project>
    <PropertyGroup>
        <Configurations></Configurations>
    </PropertyGroup>
    <PropertyGroup>
        <Debug>Debug</Debug>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)' == '$(Debug)' "" />
    <PropertyGroup Condition="" '$(Configuration)' == 'Release' "" />
</Project>
")]
        public async Task GetProjectConfigurationDimensionsAsync_WhenDimensionsSpecifiedInProject_ReturnsThem(string[] configurations, string[] platforms, string[] targetFrameworks, string projectXml)
        {
            await VerifyGetProjectConfigurationDimensionsAsync(configurations, platforms, targetFrameworks, projectXml);
        }

        [Theory]
        [InlineData(
// Configuration (Expected)      Platform (Expected)      TargetFramework (Expected)
new[] { "Debug" },               new[] { "AnyCPU" },      null,
@"
<Project>
    <PropertyGroup>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Debug" },               new[] { "AnyCPU" },      null,
@"
<Project>
    <PropertyGroup>
        <Configurations></Configurations>
        <Platforms></Platforms>
        <TargetFrameworks></TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Debug" },               new[] { "AnyCPU" },      null,
@"
<Project>
    <PropertyGroup Condition=""false"">
        <Configurations>Release</Configurations>
        <Platforms>x64</Platforms>
        <TargetFrameworks>net45</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Release" },               new[] { "AnyCPU" },      null,
@"
<Project>
    <PropertyGroup>
        <Configurations>Release</Configurations>
        <Platforms>$(Invalid)</Platforms>
        <TargetFrameworks Condition=""false"">net45</TargetFrameworks>
    </PropertyGroup>
</Project>
")]
        [InlineData(
new[] { "Debug" },               new[] { "AnyCPU" },      null,
@"
<Project>
    <PropertyGroup Condition=""'$(Configuration)'==''"" />
</Project>
")]
        [InlineData(
new[] { "Debug" },               new[] { "AnyCPU" },      null,
@"
<Project>
    <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='|'"" />
</Project>
")]
        public async Task GetProjectConfigurationDimensionsAsync_WhenProjectsAreMissingDimensions_ReturnsDefaults(string[] configurations, string[] platforms, string[] targetFrameworks, string projectXml)
        {
            await VerifyGetProjectConfigurationDimensionsAsync(configurations, platforms, targetFrameworks, projectXml);
        }

        private static async Task VerifyGetProjectConfigurationDimensionsAsync(string[] configurations, string[] platforms, string[]? targetFrameworks, string projectXml)
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = CreateInstance(projectXml);

            var results = await provider.GetProjectConfigurationDimensionsAsync(project);

            Assert.Equal(targetFrameworks == null ? 2: 3, results.Count());

            var configurationResults = results.ElementAt(0);
            var platformResults = results.ElementAt(1);

            Assert.Equal("Configuration", configurationResults.Key);
            Assert.Equal(configurations, configurationResults.Value);
            Assert.Equal("Platform", platformResults.Key);
            Assert.Equal(platforms, platformResults.Value);

            if (targetFrameworks != null)
            {
                var targetFrameworksResults = results.ElementAt(2);

                Assert.Equal("TargetFramework", targetFrameworksResults.Key);
                Assert.Equal(targetFrameworks, targetFrameworksResults.Value);
            }
        }

        private async Task VerifyGetBestGuessDefaultValuesForDimensionsAsync(string? solutionConfiguration, string configuration, string platform, string? targetFramework, string projectXml)
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = CreateInstance(projectXml);
            var results = solutionConfiguration == null ? await provider.GetBestGuessDefaultValuesForDimensionsAsync(project) : await provider.GetBestGuessDefaultValuesForDimensionsAsync(project, solutionConfiguration);

            if (targetFramework == null)
            {
                Assert.Collection(results,
                    firstValue => Assert.Equal(new KeyValuePair<string, string>("Configuration", configuration), firstValue),
                    secondValue => Assert.Equal(new KeyValuePair<string, string>("Platform", platform), secondValue));
            }
            else
            {
                Assert.Collection(results,
                    firstValue => Assert.Equal(new KeyValuePair<string, string>("Configuration", configuration), firstValue),
                    secondValue => Assert.Equal(new KeyValuePair<string, string>("Platform", platform), secondValue),
                    thirdValue => Assert.Equal(new KeyValuePair<string, string>("TargetFramework", targetFramework), thirdValue));
            }
        }

        private static ConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var knownProperties = ConfigurationDimensionProvider.KnownDimensions
                                                                .SelectMany(d => new[] 
                                                                { 
                                                                    (typeof(IStringProperty), d.SingularPropertyName), 
                                                                    (typeof(IStringListProperty), d.MultiplePropertyName) 
                                                                })
                                                                .ToArray();

            var properties = ProjectPropertiesFactory.Create(DeclaredDimensions.SchemaName, projectXml, knownProperties);
            var accessor = IProjectAccessorFactory.Create(projectXml);
            var telemetryService = ITelemetryServiceFactory.Create();

            var mock = new Mock<ConfigurationDimensionProvider>(accessor, telemetryService);
            mock.Protected().Setup<ProjectProperties>("GetProjectProperties", ItExpr.IsAny<ConfiguredProject>())
                .Returns(properties);

            return mock.Object;
        }
    }
}
