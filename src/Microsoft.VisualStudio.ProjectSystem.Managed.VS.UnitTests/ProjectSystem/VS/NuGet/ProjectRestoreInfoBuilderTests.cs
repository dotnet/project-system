using System;
using System.Collections.Immutable;
using System.Linq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    public class ProjectRestoreInfoBuilderTests
    {
        [Fact]
        public void NullUpdate_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("updates", () =>
            {
                ProjectRestoreInfoBuilder.Build(null);
            });
        }

        [Fact]
        public void NoProjectChanges_ReturnsNull()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);
            Assert.Null(restoreInfo);
        }

        [Fact]
        public void WithAnyChanges_ReturnsFullRestoreInfo()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(_sampleSubscriptionUpdate);
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            Assert.Equal(@"obj\", restoreInfo.BaseIntermediatePath);
            Assert.Equal("netcoreapp1.0", restoreInfo.OriginalTargetFrameworks);

            Assert.Single(restoreInfo.TargetFrameworks);
            var tfm = restoreInfo.TargetFrameworks.Item("netcoreapp1.0");
            Assert.Equal(tfm, restoreInfo.TargetFrameworks.Item(0));
            Assert.Null(restoreInfo.TargetFrameworks.Item("InvalidFrameworkMoniker"));

            Assert.Equal("netcoreapp1.0", tfm.TargetFrameworkMoniker);
            Assert.Single(tfm.ProjectReferences);
            AssertEx.CollectionLength(tfm.PackageReferences, 2);
            Assert.Equal(9, tfm.Properties.Count);

            var definingProjectDirectory = "C:\\Test\\Projects\\TestProj";
            var definingProjectFullPath = "C:\\Test\\Projects\\TestProj\\TestProj.csproj";

            // properties
            Assert.Equal("obj\\", tfm.Properties.Item("MSBuildProjectExtensionsPath").Value);
            Assert.Equal(".NETCoreApp,Version=v1.0", tfm.Properties.Item("TargetFrameworkMoniker").Value);
            Assert.Equal("netcoreapp1.0", tfm.Properties.Item("TargetFrameworks").Value);
            Assert.Equal("netcoreapp1.0;netstandard16", tfm.Properties.Item("PackageTargetFallback").Value);
            Assert.Equal("win7-x64", tfm.Properties.Item("RuntimeIdentifier").Value);
            Assert.Equal("win7-x64", tfm.Properties.Item("RuntimeIdentifiers").Value);

            // project references
            var projectRef = tfm.ProjectReferences.Item(0);
            Assert.Equal(projectRef, tfm.ProjectReferences.Item("..\\TestLib\\TestLib.csproj"));
            Assert.Equal("..\\TestLib\\TestLib.csproj", projectRef.Name);
            Assert.Equal("C:\\Test\\Projects\\TestLib\\TestLib.csproj", projectRef.Properties.Item("ProjectFileFullPath").Value);
            Assert.Equal(definingProjectDirectory, projectRef.Properties.Item("DefiningProjectDirectory").Value);
            Assert.Equal(definingProjectFullPath, projectRef.Properties.Item("DefiningProjectFullPath").Value);

            // package references
            var packageRef = tfm.PackageReferences.Item("Microsoft.NETCore.Sdk");
            Assert.Equal("Microsoft.NETCore.Sdk", packageRef.Name);
            Assert.Equal("1.0.0-alpha-20161007-5", packageRef.Properties.Item("Version").Value);
            Assert.Equal(definingProjectDirectory, packageRef.Properties.Item("DefiningProjectDirectory").Value);
            Assert.Equal(definingProjectFullPath, packageRef.Properties.Item("DefiningProjectFullPath").Value);

            packageRef = tfm.PackageReferences.Item("Microsoft.NETCore.App");
            Assert.Equal("Microsoft.NETCore.App", packageRef.Name);
            Assert.Equal("1.0.1", packageRef.Properties.Item("Version").Value);
            Assert.Equal(definingProjectDirectory, packageRef.Properties.Item("DefiningProjectDirectory").Value);
            Assert.Equal(definingProjectFullPath, packageRef.Properties.Item("DefiningProjectFullPath").Value);

            // tool references
            Assert.Single(restoreInfo.ToolReferences);
            var toolRef = restoreInfo.ToolReferences.Item(0);
            Assert.Equal(toolRef, restoreInfo.ToolReferences.Item("Microsoft.AspNet.EF.Tools"));
            Assert.Equal("Microsoft.AspNet.EF.Tools", toolRef.Name);
            Assert.Equal("1.0.0", toolRef.Properties.Item("Version").Value);
        }

        [Fact]
        public void WithNoTargetFrameworkDimension_UsesPropertiesInstead()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netcoreapp1.0"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",            
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0"",
                   ""TargetFrameworks"": ""netcoreapp1.0"",
                   ""TargetFramework"": ""netcoreapp1.0""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            Assert.Single(restoreInfo.TargetFrameworks);
            Assert.NotNull(restoreInfo.TargetFrameworks.Item("netcoreapp1.0"));
        }

        [Fact]
        public void WithEmptyTargetFramework_ReturnsNull()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netcoreapp1.0"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",            
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0"",
                   ""TargetFrameworks"": ""netcoreapp1.0"",
                   ""TargetFramework"": """"
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);
            Assert.Null(restoreInfo);
        }

        [Fact]
        public void WithTwoIdenticalUpdates_ReturnsSingleTFM()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(
                _sampleSubscriptionUpdate,
                _sampleSubscriptionUpdate);
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            Assert.Single(restoreInfo.TargetFrameworks);
            var tfm = restoreInfo.TargetFrameworks.Item(0);
            Assert.Equal("netcoreapp1.0", tfm.TargetFrameworkMoniker);
        }

        [Fact]
        public void WithTwoDifferentUpdates_ReturnsTwoTFMs()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netcoreapp1.0"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",
            ""TargetFramework"": ""netcoreapp1.0"",
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworks"": """",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        }
    }
}", @"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netstandard1.4"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",
            ""TargetFramework"": ""netstandard1.4"",
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworks"": """",
                   ""TargetFrameworkMoniker"": "".NETStandard,Version=v1.4""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            AssertEx.CollectionLength(restoreInfo.TargetFrameworks, 2);
            Assert.NotNull(restoreInfo.TargetFrameworks.Item("netcoreapp1.0"));
            Assert.NotNull(restoreInfo.TargetFrameworks.Item("netstandard1.4"));
        }

        [Fact]
        public void WithRepeatedToolReference_ReturnsJustOne()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netcoreapp1.0"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",
            ""TargetFramework"": ""netcoreapp1.0"",
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworks"": """",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": {
                    ""Microsoft.AspNet.EF.Tools"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.0""
                    }
                }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        }
    }
}", @"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netstandard1.4"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",
            ""TargetFrameworks"": """",
            ""TargetFramework"": ""netstandard1.4"",
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworkMoniker"": "".NETStandard,Version=v1.4""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": {
                    ""Microsoft.AspNet.EF.Tools"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.0""
                    }
                }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": { }
            }
        }
    }
}");
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            AssertEx.CollectionLength(restoreInfo.TargetFrameworks, 2);
            Assert.Single(restoreInfo.ToolReferences);
            var toolRef = restoreInfo.ToolReferences.Item(0);
            Assert.Equal(toolRef, restoreInfo.ToolReferences.Item("Microsoft.AspNet.EF.Tools"));
            Assert.Equal("Microsoft.AspNet.EF.Tools", toolRef.Name);
            Assert.Equal("1.0.0", toolRef.Properties.Item("Version").Value);
        }

        private const string _sampleSubscriptionUpdate = @"{
    ""ProjectConfiguration"": {
        ""Name"": ""Debug|AnyCPU|netcoreapp1.0"",
        ""Dimensions"": {
            ""Configuration"": ""Debug"",
            ""TargetFramework"": ""netcoreapp1.0"",
            ""Platform"": ""AnyCPU""
        }
    },
    ""ProjectChanges"": {
        ""NuGetRestore"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Properties"": {
                   ""MSBuildProjectExtensionsPath"": ""obj\\"",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0"",
                   ""TargetFrameworkIdentifier"": "".NETCoreApp"",
                   ""TargetFrameworkVersion"": ""v1.0"",
                   ""TargetFrameworks"": ""netcoreapp1.0"",
                   ""PackageTargetFallback"": ""netcoreapp1.0;netstandard16"",
                   ""RuntimeIdentifier"": ""win7-x64"",
                   ""RuntimeIdentifiers"": ""win7-x64"",
                   ""MSBuildProjectDirectory"": ""C:\\Test\\Projects\\TestProj""
                }
            }
        },
        ""PackageReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": {
                    ""Microsoft.NETCore.Sdk"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.0-alpha-20161007-5"",
                        ""TargetFramework"": """",
                        ""RuntimeIdentifier"": """"
                    },
                    ""Microsoft.NETCore.App"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.1"",
                        ""TargetFramework"": """",
                        ""RuntimeIdentifier"": """"
                    }
                }
            }
        },
        ""ProjectReference"": {
            ""Difference"": {
                ""AnyChanges"": ""true"",
                ""AddedItems"": [ ""..\\TestLib\\TestLib.csproj"" ]
            },
            ""After"": {
                ""Items"": {
                    ""..\\TestLib\\TestLib.csproj"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""ProjectFileFullPath"": ""C:\\Test\\Projects\\TestLib\\TestLib.csproj"",
                    }
                }
            }
        },
        ""DotNetCliToolReference"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Items"": {
                    ""Microsoft.AspNet.EF.Tools"": {
                        ""DefiningProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj"",
                        ""Version"": ""1.0.0""
                    }
                }
            }
        }
    }
}";

        private static ImmutableList<IProjectVersionedValue<IProjectSubscriptionUpdate>> GetVersionedUpdatesFromJson(
            params string[] jsonStrings) =>
                jsonStrings
                    .Select(s => IProjectSubscriptionUpdateFactory.FromJson(s))
                    .Select(u => IProjectVersionedValueFactory.Create(u))
                    .ToImmutableList();
    }
}
