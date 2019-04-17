// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    public class RestoreBuilderTests
    {
        private const string ProjectWithEmptyItems =
@"{
    ""CurrentState"": {
        ""NuGetRestore"": {
            ""Properties"": {
                ""MSBuildProjectExtensionsPath"": ""C:\\Project\\obj"",
                ""TargetFrameworks"": ""net45"",               
                ""TargetFrameworkMoniker"": "".NETFramework, Version=v4.5"",
                ""Property"": ""Value""
            },
        },
        ""ProjectReference"": {
            ""Items"" : {}
        },
        ""PackageReference"": {
            ""Items"" : {}
        },
        ""DotNetCliToolReference"": {
            ""Items"" : {}
        }
    }
}";
        private const string ProjectWithItems =
@"{
    ""CurrentState"": {
        ""NuGetRestore"": {
            ""Properties"": {
               ""TargetFrameworkMoniker"": "".NETFramework, Version=v4.5"",
               ""TargetFrameworks"": ""net45"",
               ""MSBuildProjectExtensionsPath"": ""C:\\Project\\obj"",
               ""Property"": ""Value""
            },
        },
        ""ProjectReference"": {
            ""Items"" : {
                ""..\\Project\\Project1.csproj"" : {
                    ""ProjectFileFullPath"" : ""C:\\Solution\\Project\\Project1.csproj"",
                },
                ""..\\Project\\Project2.csproj"" : {
                    ""ProjectFileFullPath"" : ""C:\\Solution\\Project\\Project2.csproj"",
                },
                ""..\\Project\\Project3.csproj"" : {
                    ""ProjectFileFullPath"" : ""C:\\Solution\\Project\\Project3.csproj"",
                    ""Property"": ""Value""
                }
            }
        },
        ""PackageReference"": {
            ""Items"" : {
                ""PackageReference1"" : {
                    ""Version"" : ""1.0.0.0"",
                },
                ""PackageReference2"" : {
                    ""Version"" : ""2.0.0.0"",
                },
                ""PackageReference3"" : {
                    ""Name"" : ""Value""
                }
            }
        },
        ""DotNetCliToolReference"": {
            ""Items"" : {
                ""ToolReference1"" : {
                    ""Version"" : ""1.0.0.0"",
                },
                ""ToolReference2"" : {
                    ""Version"" : ""2.0.0.0"",
                },
                ""ToolReference3"" : {
                    ""Name"" : ""Value""
                }
            }
        }
    }
}";

        [Fact]
        public void ToProjectRestoreInfo_WhenNoItems_ReturnsEmptyItemCollections()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(ProjectWithEmptyItems);

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Empty(result.ToolReferences);
            Assert.Equal(1, result.TargetFrameworks.Count);

            var targetFramework = result.TargetFrameworks.Item(0);

            Assert.Empty(targetFramework.PackageReferences);
            Assert.Empty(targetFramework.ProjectReferences);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsCoreProperties()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(ProjectWithEmptyItems);

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal("C:\\Project\\obj",            result.BaseIntermediatePath);
            Assert.Equal("net45",                       result.OriginalTargetFrameworks);
            Assert.Equal(1,                             result.TargetFrameworks.Count);
            Assert.Equal(".NETFramework, Version=v4.5", result.TargetFrameworks.Item(0).TargetFrameworkMoniker);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsTargetFrameworkPropertiesToAllProperties()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(ProjectWithEmptyItems);

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var properties = result.TargetFrameworks.Item(0).Properties;

            Assert.Equal("MSBuildProjectExtensionsPath",    properties.Item("MSBuildProjectExtensionsPath").Name);
            Assert.Equal("C:\\Project\\obj",                properties.Item("MSBuildProjectExtensionsPath").Value);

            Assert.Equal("TargetFrameworks",                properties.Item("TargetFrameworks").Name);
            Assert.Equal("net45",                           properties.Item("TargetFrameworks").Value);

            Assert.Equal("TargetFrameworkMoniker",          properties.Item("TargetFrameworkMoniker").Name);
            Assert.Equal(".NETFramework, Version=v4.5",     properties.Item("TargetFrameworkMoniker").Value);

            Assert.Equal("Property",                        properties.Item("Property").Name);
            Assert.Equal("Value",                           properties.Item("Property").Value);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsToolReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(ProjectWithItems);

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);
            var references = result.ToolReferences;

            Assert.Equal(3, references.Count);

            Assert.Equal("ToolReference1",      references.Item("ToolReference1").Name);
            Assert.Equal("Version",             references.Item("ToolReference1").Properties.Item("Version").Name);
            Assert.Equal("1.0.0.0",             references.Item("ToolReference1").Properties.Item("Version").Value);

            Assert.Equal("ToolReference2",      references.Item("ToolReference2").Name);
            Assert.Equal("Version",             references.Item("ToolReference2").Properties.Item("Version").Name);
            Assert.Equal("2.0.0.0",             references.Item("ToolReference2").Properties.Item("Version").Value);

            Assert.Equal("ToolReference3",      references.Item("ToolReference3").Name);
            Assert.Equal("Name",                references.Item("ToolReference3").Properties.Item("Name").Name);
            Assert.Equal("Value",               references.Item("ToolReference3").Properties.Item("Name").Value);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsPackageReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(ProjectWithItems);

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var references = result.TargetFrameworks.Item(0).PackageReferences;

            Assert.Equal(3, references.Count);

            Assert.Equal("PackageReference1",   references.Item("PackageReference1").Name);
            Assert.Equal("Version",             references.Item("PackageReference1").Properties.Item("Version").Name);
            Assert.Equal("1.0.0.0",             references.Item("PackageReference1").Properties.Item("Version").Value);

            Assert.Equal("PackageReference2",   references.Item("PackageReference2").Name);
            Assert.Equal("Version",             references.Item("PackageReference2").Properties.Item("Version").Name);
            Assert.Equal("2.0.0.0",             references.Item("PackageReference2").Properties.Item("Version").Value);

            Assert.Equal("PackageReference3",   references.Item("PackageReference3").Name);
            Assert.Equal("Name",                references.Item("PackageReference3").Properties.Item("Name").Name);
            Assert.Equal("Value",               references.Item("PackageReference3").Properties.Item("Name").Value);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsProjectReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(ProjectWithItems);

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var references = result.TargetFrameworks.Item(0).ProjectReferences;

            Assert.Equal(3, references.Count);

            Assert.Equal("..\\Project\\Project1.csproj",             references.Item("..\\Project\\Project1.csproj").Name);
            Assert.Equal("ProjectFileFullPath",                      references.Item("..\\Project\\Project1.csproj").Properties.Item("ProjectFileFullPath").Name);
            Assert.Equal("C:\\Solution\\Project\\Project1.csproj",   references.Item("..\\Project\\Project1.csproj").Properties.Item("ProjectFileFullPath").Value);

            Assert.Equal("..\\Project\\Project2.csproj",             references.Item("..\\Project\\Project2.csproj").Name);
            Assert.Equal("ProjectFileFullPath",                      references.Item("..\\Project\\Project2.csproj").Properties.Item("ProjectFileFullPath").Name);
            Assert.Equal("C:\\Solution\\Project\\Project2.csproj",   references.Item("..\\Project\\Project2.csproj").Properties.Item("ProjectFileFullPath").Value);

            Assert.Equal("..\\Project\\Project3.csproj",             references.Item("..\\Project\\Project3.csproj").Name);
            Assert.Equal("ProjectFileFullPath",                      references.Item("..\\Project\\Project3.csproj").Properties.Item("ProjectFileFullPath").Name);
            Assert.Equal("C:\\Solution\\Project\\Project3.csproj",   references.Item("..\\Project\\Project3.csproj").Properties.Item("ProjectFileFullPath").Value);
            Assert.Equal("Property",                                 references.Item("..\\Project\\Project3.csproj").Properties.Item("Property").Name);
            Assert.Equal("Value",                                    references.Item("..\\Project\\Project3.csproj").Properties.Item("Property").Value);
        }
    }
}
