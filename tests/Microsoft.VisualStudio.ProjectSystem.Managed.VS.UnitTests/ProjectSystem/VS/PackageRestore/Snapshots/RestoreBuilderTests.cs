// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class RestoreBuilderTests
    {
        [Fact]
        public void ToProjectRestoreInfo_WhenItemsAreMissing_ReturnsEmptyItemCollections()
        {
            var update = IProjectSubscriptionUpdateFactory.CreateEmpty();

            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            AssertNoItems(result);
        }

        [Fact]
        public void ToProjectRestoreInfo_WhenNoItems_ReturnsEmptyItemCollections()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "ProjectReference": {
                            "Items" : {}
                        },
                        "PackageReference": {
                            "Items" : {}
                        },
                        "DotNetCliToolReference": {
                            "Items" : {}
                        },
                        "CollectedFrameworkReference": {
                            "Items" : {}
                        },
                        "CollectedPackageDownload": {
                            "Items" : {}
                        },
                        "CollectedPackageVersion": {
                            "Items" : {}
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            AssertNoItems(result);
        }

        [Fact]
        public void ToProjectRestoreInfo_WhenNuGetRestoreRuleMissing_ReturnsEmpty()
        {
            var update = IProjectSubscriptionUpdateFactory.CreateEmpty();
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Empty(result.MSBuildProjectExtensionsPath);
            Assert.Empty(result.OriginalTargetFrameworks);
            Assert.Equal(1, result.TargetFrameworks.Count);

            var targetFramework = result.TargetFrameworks.Item(0);

            Assert.Empty(targetFramework.TargetFrameworkMoniker);
            Assert.Empty(targetFramework.Properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_RespectsNuGetTargetMonikerIfPresent()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "NuGetRestore": {
                            "Properties": {
                                "NuGetTargetMoniker": "UWP, Version=v10",
                                "TargetFrameworkMoniker": ".NETFramework, Version=v4.5"
                            },
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);
            var targetFramework = result.TargetFrameworks.Item(0);

            Assert.Equal("UWP, Version=v10", targetFramework.TargetFrameworkMoniker);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsCoreProperties()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "NuGetRestore": {
                            "Properties": {
                                "MSBuildProjectExtensionsPath": "C:\\Project\\obj",
                                "TargetFrameworks": "net45",
                                "TargetFrameworkMoniker": ".NETFramework, Version=v4.5"
                            },
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal("C:\\Project\\obj", result.MSBuildProjectExtensionsPath);
            Assert.Equal("net45", result.OriginalTargetFrameworks);
            Assert.Equal(1, result.TargetFrameworks.Count);

            var targetFramework = result.TargetFrameworks.Item(0);

            Assert.Equal(".NETFramework, Version=v4.5", targetFramework.TargetFrameworkMoniker);
        }

        [Fact]
        public void ToProjectRestoreInfo_WhenEmpty_SetsCorePropertiesToEmpty()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "NuGetRestore": {
                            "Properties": {
                                "MSBuildProjectExtensionsPath": "",
                                "TargetFrameworks": "",
                                "TargetFrameworkMoniker": ""
                            },
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Empty(result.MSBuildProjectExtensionsPath);
            Assert.Empty(result.OriginalTargetFrameworks);
            Assert.Equal(1, result.TargetFrameworks.Count);

            var targetFramework = result.TargetFrameworks.Item(0);

            Assert.Empty(targetFramework.TargetFrameworkMoniker);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsTargetFrameworkProperties()
        {   // All NuGetRestore properties end up in the "target framework" property bag
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "NuGetRestore": {
                            "Properties": {
                                "Property": "Value",
                                "AnotherProperty": "AnotherValue"
                            },
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var properties = result.TargetFrameworks.Item(0).Properties;

            AssertContainsProperty("Property", "Value", properties);
            AssertContainsProperty("AnotherProperty", "AnotherValue", properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsToolReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "DotNetCliToolReference": {
                            "Items" : {
                                "ToolReference1" : {
                                    "Version" : "1.0.0.0",
                                },
                                "ToolReference2" : {
                                    "Version" : "2.0.0.0",
                                },
                                "ToolReference3" : {
                                    "Name" : "Value"
                                }
                            }
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);
            var references = result.ToolReferences;

            Assert.Equal(3, references.Count);

            Assert.Equal("ToolReference1", references.Item("ToolReference1").Name);
            AssertContainsProperty("Version", "1.0.0.0", references.Item("ToolReference1").Properties);

            Assert.Equal("ToolReference2", references.Item("ToolReference2").Name);
            AssertContainsProperty("Version", "2.0.0.0", references.Item("ToolReference2").Properties);

            Assert.Equal("ToolReference3", references.Item("ToolReference3").Name);
            AssertContainsProperty("Name", "Value", references.Item("ToolReference3").Properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsPackageReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "CollectedPackageReference": {
                            "Items" : {
                                "PackageReference1" : {
                                    "Version" : "1.0.0.0",
                                },
                                "PackageReference2" : {
                                    "Version" : "2.0.0.0",
                                },
                                "PackageReference3" : {
                                    "Name" : "Value"
                                }
                            }
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var references = result.TargetFrameworks.Item(0).PackageReferences;

            Assert.Equal(3, references.Count);

            Assert.Equal("PackageReference1", references.Item("PackageReference1").Name);
            AssertContainsProperty("Version", "1.0.0.0", references.Item("PackageReference1").Properties);

            Assert.Equal("PackageReference2", references.Item("PackageReference2").Name);
            AssertContainsProperty("Version", "2.0.0.0", references.Item("PackageReference2").Properties);

            Assert.Equal("PackageReference3", references.Item("PackageReference3").Name);
            AssertContainsProperty("Name", "Value", references.Item("PackageReference3").Properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsCentralPackageVersions()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "CollectedPackageVersion": {
                            "Items" : {
                                "Newtonsoft.Json" : {
                                    "Version" : "1.0",
                                },
                                "System.IO" : {
                                    "Version" : "2.0",
                                },
                                "Microsoft.Extensions" : {
                                    "Version" : "3.0"
                                }
                            }
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var versions = ((IVsTargetFrameworkInfo3)result.TargetFrameworks.Item(0)).CentralPackageVersions;

            Assert.Equal(3, versions.Count);

            var reference1 = versions.Item("Newtonsoft.Json");
            Assert.Equal("Newtonsoft.Json", reference1.Name);

            AssertContainsProperty("Version", "1.0", reference1.Properties);

            var reference2 = versions.Item("System.IO");
            Assert.Equal("System.IO", reference2.Name);

            AssertContainsProperty("Version", "2.0", reference2.Properties);

            var reference3 = versions.Item("Microsoft.Extensions");
            Assert.Equal("Microsoft.Extensions", reference3.Name);

            AssertContainsProperty("Version", "3.0", reference3.Properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsProjectReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "ProjectReference": {
                            "Items" : {
                                "..\\Project\\Project1.csproj" : {
                                    "ProjectFileFullPath" : "C:\\Solution\\Project\\Project1.csproj",
                                },
                                "..\\Project\\Project2.csproj" : {
                                    "ProjectFileFullPath" : "C:\\Solution\\Project\\Project2.csproj",
                                },
                                "..\\Project\\Project3.csproj" : {
                                    "ProjectFileFullPath" : "C:\\Solution\\Project\\Project3.csproj",
                                    "MetadataName": "MetadataValue"
                                }
                            }
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var references = result.TargetFrameworks.Item(0).ProjectReferences;

            Assert.Equal(3, references.Count);

            var reference1 = references.Item("..\\Project\\Project1.csproj");
            Assert.Equal("..\\Project\\Project1.csproj", reference1.Name);

            AssertContainsProperty("ProjectFileFullPath", "C:\\Solution\\Project\\Project1.csproj", reference1.Properties);

            var reference2 = references.Item("..\\Project\\Project2.csproj");
            Assert.Equal("..\\Project\\Project2.csproj", reference2.Name);

            AssertContainsProperty("ProjectFileFullPath", "C:\\Solution\\Project\\Project2.csproj", reference2.Properties);

            var reference3 = references.Item("..\\Project\\Project3.csproj");
            Assert.Equal("..\\Project\\Project3.csproj", reference3.Name);

            AssertContainsProperty("ProjectFileFullPath", "C:\\Solution\\Project\\Project3.csproj", reference3.Properties);
            AssertContainsProperty("MetadataName", "MetadataValue", reference3.Properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsFrameworkReferences()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "CollectedFrameworkReference": {
                            "Items" : {
                                "WindowsForms" : {
                                },
                                "WPF" : {
                                    "PrivateAssets" : "all",
                                }
                            }
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var references = result.TargetFrameworks.Item(0).FrameworkReferences;

            Assert.Equal(2, references.Count);

            var reference1 = references.Item("WindowsForms");
            Assert.Equal("WindowsForms", reference1.Name);
            Assert.Empty(reference1.Properties);

            var reference2 = references.Item("WPF");
            Assert.Equal("WPF", reference2.Name);

            AssertContainsProperty("PrivateAssets", "all", reference2.Properties);
        }

        [Fact]
        public void ToProjectRestoreInfo_SetsPackageDownloads()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "CurrentState": {
                        "CollectedPackageDownload": {
                            "Items" : {
                                "NuGet.Common" : {
                                    "Version" : "[4.0.0];[5.0.0]",
                                },
                                "NuGet.Frameworks" : {
                                    "Version" : "[4.9.4]",
                                }
                            }
                        }
                    }
                }
                """);
            var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

            Assert.Equal(1, result.TargetFrameworks.Count);

            var downloads = result.TargetFrameworks.Item(0).PackageDownloads;

            Assert.Equal(2, downloads.Count);

            var download1 = downloads.Item("NuGet.Common");
            Assert.Equal("NuGet.Common", download1.Name);

            AssertContainsProperty("Version", "[4.0.0];[5.0.0]", download1.Properties);

            var download2 = downloads.Item("NuGet.Frameworks");
            Assert.Equal("NuGet.Frameworks", download2.Name);

            AssertContainsProperty("Version", "[4.9.4]", download2.Properties);
        }

        private static void AssertContainsProperty(string name, string value, IVsProjectProperties properties)
        {
            var property = properties.Item(name);

            Assert.NotNull(property);
            Assert.Equal(name, property.Name);
            Assert.Equal(value, property.Value);
        }

        private static void AssertContainsProperty(string name, string value, IVsReferenceProperties properties)
        {
            var property = properties.Item(name);

            Assert.NotNull(property);
            Assert.Equal(name, property.Name);
            Assert.Equal(value, property.Value);
        }

        private static void AssertNoItems(IVsProjectRestoreInfo2 result)
        {
            Assert.Empty(result.ToolReferences);
            Assert.Equal(1, result.TargetFrameworks.Count);

            var targetFramework = (IVsTargetFrameworkInfo3)result.TargetFrameworks.Item(0);

            Assert.Empty(targetFramework.FrameworkReferences);
            Assert.Empty(targetFramework.PackageDownloads);
            Assert.Empty(targetFramework.PackageReferences);
            Assert.Empty(targetFramework.ProjectReferences);
            Assert.Empty(targetFramework.CentralPackageVersions);
        }
    }
}
