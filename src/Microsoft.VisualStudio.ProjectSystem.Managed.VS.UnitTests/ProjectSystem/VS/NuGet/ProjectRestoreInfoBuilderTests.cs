using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    [ProjectSystemTrait]
    public class ProjectRestoreInfoBuilderTests
    {
        [Fact]
        public void ProjectRestoreInfoBuilder_NullUpdate_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("updates", () => {
                ProjectRestoreInfoBuilder.Build(null);
            });
        }

        [Fact]
        public void ProjectRestoreInfoBuilder_NoProjectChanges_ReturnsNull()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            }
        },
        ""PackageReference"": {
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
        public void ProjectRestoreInfoBuilder_WithAnyChanges_ReturnsFullRestoreInfo()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(_sampleSubscriptionUpdate);
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);
            
            Assert.NotNull(restoreInfo);
            Assert.Equal(@"obj\", restoreInfo.BaseIntermediatePath);

            Assert.Equal(1, restoreInfo.TargetFrameworks.Count);
            var tfm = restoreInfo.TargetFrameworks.Item("netcoreapp1.0");
            Assert.Equal(tfm, restoreInfo.TargetFrameworks.Item(0));
            Assert.Null(restoreInfo.TargetFrameworks.Item(1));
            Assert.Null(restoreInfo.TargetFrameworks.Item("InvalidFrameworkMoniker"));

            Assert.Equal("netcoreapp1.0", tfm.TargetFrameworkMoniker);
            Assert.Equal(1, tfm.ProjectReferences.Count);
            Assert.Equal(2, tfm.PackageReferences.Count);

            var definingProjectDirectory = "C:\\Test\\Projects\\TestProj";
            var definingProjectFullPath = "C:\\Test\\Projects\\TestProj\\TestProj.csproj";

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
        }

        [Fact]
        public void ProjectRestoreInfoBuilder_WithTwoIdenticalUpdates_ReturnsSingleTFM()
        {
            var projectSubscriptionUpdates = GetVersionedUpdatesFromJson(
                _sampleSubscriptionUpdate,
                _sampleSubscriptionUpdate);
            var restoreInfo = ProjectRestoreInfoBuilder.Build(projectSubscriptionUpdates);

            Assert.NotNull(restoreInfo);
            Assert.Equal(1, restoreInfo.TargetFrameworks.Count);
            var tfm = restoreInfo.TargetFrameworks.Item(0);
            Assert.Equal("netcoreapp1.0", tfm.TargetFrameworkMoniker);
        }

        [Fact]
        public void ProjectRestoreInfoBuilder_WithTwoDifferentUpdates_ReturnsTwoTFMs()
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
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""BaseIntermediateOutputPath"": ""obj\\"",
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
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""AnyChanges"": ""true""
            },
            ""After"": {
                ""Properties"": {
                   ""BaseIntermediateOutputPath"": ""obj\\"",
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
            Assert.Equal(2, restoreInfo.TargetFrameworks.Count);
            Assert.NotNull(restoreInfo.TargetFrameworks.Item("netcoreapp1.0"));
            Assert.NotNull(restoreInfo.TargetFrameworks.Item("netstandard1.4"));
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
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""AnyChanges"": ""false""
            },
            ""After"": {
                ""Properties"": {
                   ""BaseIntermediateOutputPath"": ""obj\\"",
                   ""TargetFrameworkMoniker"": "".NETCoreApp,Version=v1.0"",
                   ""TargetFrameworkIdentifier"": "".NETCoreApp"",
                   ""TargetFrameworkVersion"": ""v1.0"",
                   ""TargetFrameworks"": ""netcoreapp1.0"",
                   ""Configuration"": ""Debug"",
                   ""Platform"": ""AnyCPU"",
                   ""OutputPath"": ""bin\\Debug\\netcoreapp1.0\\"",
                   ""OutputType"": ""Exe"",
                   ""MSBuildProjectDirectory"": ""C:\\Test\\Projects\\TestProj"",
                   ""IntermediateOutputPath"": ""obj\\Debug\\netcoreapp1.0\\""
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
                        ""DefiningProjectFullPath"": ""C:\\Test\\Projects\\TestProj\\TestProj.csproj""
                    }
                }
            }
        }
    }
}";

        private ImmutableList<IProjectVersionedValue<IProjectSubscriptionUpdate>> GetVersionedUpdatesFromJson(
            params string[] jsonStrings) =>
                jsonStrings
                    .Select(s => IProjectSubscriptionUpdateFactory.FromJson(s))
                    .Select(u => IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(u))
                    .ToImmutableList();
    }
}
