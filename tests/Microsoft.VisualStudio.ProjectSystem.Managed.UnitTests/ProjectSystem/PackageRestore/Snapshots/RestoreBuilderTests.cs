// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

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
                        "EvaluatedProjectReference": {
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

        var targetFramework = Assert.Single(result.TargetFrameworks);

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
        var targetFramework = result.TargetFrameworks[0];

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

        var targetFramework = Assert.Single(result.TargetFrameworks);

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

        var targetFramework = Assert.Single(result.TargetFrameworks);

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

        var properties = Assert.Single(result.TargetFrameworks).Properties;

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

        Assert.Equal(3, references.Length);

        var toolReference1 = references.FirstOrDefault(r => r.Name == "ToolReference1");
        Assert.NotNull(toolReference1);
        AssertContainsProperty("Version", "1.0.0.0", toolReference1.Properties);

        var toolReference2 = references.FirstOrDefault(r => r.Name == "ToolReference2");
        Assert.NotNull(toolReference2);
        AssertContainsProperty("Version", "2.0.0.0", toolReference2.Properties);

        var toolReference3 = references.FirstOrDefault(r => r.Name == "ToolReference3");
        Assert.NotNull(toolReference3);
        AssertContainsProperty("Name", "Value", toolReference3.Properties);
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

        var references = Assert.Single(result.TargetFrameworks).PackageReferences;

        Assert.Equal(3, references.Length);

        var packageReference1 = references.FirstOrDefault(r => r.Name == "PackageReference1");
        Assert.NotNull(packageReference1);
        AssertContainsProperty("Version", "1.0.0.0", packageReference1.Properties);

        var packageReference2 = references.FirstOrDefault(r => r.Name == "PackageReference2");
        Assert.NotNull(packageReference2);
        AssertContainsProperty("Version", "2.0.0.0", packageReference2.Properties);

        var packageReference3 = references.FirstOrDefault(r => r.Name == "PackageReference3");
        Assert.NotNull(packageReference3);
        AssertContainsProperty("Name", "Value", packageReference3.Properties);
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

        var versions = Assert.Single(result.TargetFrameworks).CentralPackageVersions;

        Assert.Equal(3, versions.Length);

        var reference1 = versions.FirstOrDefault(r => r.Name == "Newtonsoft.Json");
        Assert.NotNull(reference1);
        AssertContainsProperty("Version", "1.0", reference1.Properties);

        var reference2 = versions.FirstOrDefault(r => r.Name == "System.IO");
        Assert.NotNull(reference2);
        AssertContainsProperty("Version", "2.0", reference2.Properties);

        var reference3 = versions.FirstOrDefault(r => r.Name == "Microsoft.Extensions");
        Assert.NotNull(reference3);
        Assert.Equal("Microsoft.Extensions", reference3.Name);

        AssertContainsProperty("Version", "3.0", reference3.Properties);
    }

    [Fact]
    public void ToProjectRestoreInfo_SetsNuGetAuditSuppressions()
    {
        var update = IProjectSubscriptionUpdateFactory.FromJson(
            """
                {
                    "CurrentState": {
                        "CollectedNuGetAuditSuppressions": {
                            "Items" : {
                                "https://cve.contoso.com/1" : {
                                }
                            }
                        }
                    }
                }
                """);
        var result = RestoreBuilder.ToProjectRestoreInfo(update.CurrentState);

        var suppressions = Assert.Single(result.TargetFrameworks).NuGetAuditSuppress;

        var reference1 = Assert.Single(suppressions);
        Assert.Equal("https://cve.contoso.com/1", reference1.Name);
    }

    [Fact]
    public void ToProjectRestoreInfo_SetsProjectReferences()
    {
        var update = IProjectSubscriptionUpdateFactory.FromJson(
            """
                {
                    "CurrentState": {
                        "EvaluatedProjectReference": {
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

        var references = Assert.Single(result.TargetFrameworks).ProjectReferences;

        Assert.Equal(3, references.Length);

        var reference1 = references.FirstOrDefault(p => p.Name == "..\\Project\\Project1.csproj");
        Assert.NotNull(reference1);
        AssertContainsProperty("ProjectFileFullPath", "C:\\Solution\\Project\\Project1.csproj", reference1.Properties);

        var reference2 = references.FirstOrDefault(p => p.Name == "..\\Project\\Project2.csproj");
        Assert.NotNull(reference2);
        AssertContainsProperty("ProjectFileFullPath", "C:\\Solution\\Project\\Project2.csproj", reference2.Properties);

        var reference3 = references.FirstOrDefault(p => p.Name == "..\\Project\\Project3.csproj");
        Assert.NotNull(reference3);
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

        var references = Assert.Single(result.TargetFrameworks).FrameworkReferences;

        Assert.Equal(2, references.Length);

        var reference1 = references.FirstOrDefault(r => r.Name == "WindowsForms");
        Assert.NotNull(reference1);
        Assert.Empty(reference1.Properties);

        var reference2 = references.FirstOrDefault(r => r.Name == "WPF");
        Assert.NotNull(reference2);
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

        var downloads = Assert.Single(result.TargetFrameworks).PackageDownloads;

        Assert.Equal(2, downloads.Length);

        var download1 = downloads.FirstOrDefault(d => d.Name == "NuGet.Common");
        Assert.NotNull(download1);
        AssertContainsProperty("Version", "[4.0.0];[5.0.0]", download1.Properties);

        var download2 = downloads.FirstOrDefault(d => d.Name == "NuGet.Frameworks");
        Assert.NotNull(download2);
        AssertContainsProperty("Version", "[4.9.4]", download2.Properties);
    }

    private static void AssertContainsProperty(string name, string value, IImmutableDictionary<string, string> properties)
    {
        var exists = properties.TryGetValue(name, out var actualValue);

        Assert.True(exists);
        Assert.Equal(value, actualValue);
    }

    private static void AssertNoItems(ProjectRestoreInfo result)
    {
        Assert.Empty(result.ToolReferences);
        var targetFramework = Assert.Single(result.TargetFrameworks);

        Assert.Empty(targetFramework.FrameworkReferences);
        Assert.Empty(targetFramework.PackageDownloads);
        Assert.Empty(targetFramework.PackageReferences);
        Assert.Empty(targetFramework.ProjectReferences);
        Assert.Empty(targetFramework.CentralPackageVersions);
    }
}
