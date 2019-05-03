using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading.Tasks;
using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class TempPEBuildManagerTests
    {
        [Fact]
        public void CompilationNeeded_MissingOutput_NeedsRecompile()
        {
            var fileSystem = new IFileSystemMock();
            fileSystem.AddFile("Resource1.Designer.cs", new DateTime(2018, 6, 1));

            using var mgr = new TestTempPEBuildManager(fileSystem);
            var files = new HashSet<string>
                {
                    "Resource1.Designer.cs"
                };

            bool result = mgr.CompilationNeeded(files, "Resource1.Designer.cs.dll");

            Assert.True(result);
        }

        [Fact]
        public void CompilationNeeded_OldOutput_NeedsRecompile()
        {
            var fileSystem = new IFileSystemMock();
            fileSystem.AddFile("Resource1.Designer.cs", new DateTime(2018, 6, 1));
            fileSystem.AddFile("Resource1.Designer.cs.dll", new DateTime(2018, 1, 1));

            using var mgr = new TestTempPEBuildManager(fileSystem);
            var files = new HashSet<string>
                {
                    "Resource1.Designer.cs"
                };

            bool result = mgr.CompilationNeeded(files, "Resource1.Designer.cs.dll");

            Assert.True(result);
        }

        [Fact]
        public void CompilationNeeded_NewOutput_DoesntRecompile()
        {
            var fileSystem = new IFileSystemMock();
            fileSystem.AddFile("Resource1.Designer.cs", new DateTime(2018, 6, 1));
            fileSystem.AddFile("Resource1.Designer.cs.dll", new DateTime(2018, 12, 1));

            using var mgr = new TestTempPEBuildManager(fileSystem);
            var files = new HashSet<string>
                {
                    "Resource1.Designer.cs"
                };

            bool result = mgr.CompilationNeeded(files, "Resource1.Designer.cs.dll");

            Assert.False(result);
        }

        [Fact]
        public async Task Process_OutputPath_ComputedCorrectly()
        {
            var compileUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": { }
    }
}");

            var configUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""ProjectDir"", ""IntermediateOutputPath"" ]
            },
            ""After"": {
                ""Properties"": {
                    ""ProjectDir"": ""C:\\Code\\MyProject"",
                    ""IntermediateOutputPath"": ""MyOutput""
                }
            }
        }
    }
}");

            using var mgr = new TestTempPEBuildManager();
            var result = await mgr.TestProcessAsync(compileUpdate, configUpdate);

            Assert.Equal(@"C:\Code\MyProject\MyOutput\TempPE", result.OutputPath);
        }

        [Fact]
        public async Task Process_NoDesignTimeInput_ReturnsEmptyCollections()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [ ""Form1.cs"" ]
            },
            ""After"": {
                ""Items"": {
                    ""Form1.cs"": {
                    }
                }
            }
        }
    }
}");

            using var mgr = new TestTempPEBuildManager();
            var result = await mgr.TestProcessAsync(update);

            // Should have empty collections
            Assert.Empty(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_OneSharedDesignTimeInput_ReturnsOneSharedInput()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [ ""Settings.Designer.cs"" ]
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");

            using var mgr = new TestTempPEBuildManager();
            var result = await mgr.TestProcessAsync(update);

            Assert.Empty(result.Inputs);
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }


        [Fact]
        public async Task Process_OneFileBothDesignTimeInputs_WatchesOneFile()
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [ ""Settings.Designer.cs"" ]
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true,
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");

            using var mgr = new TestTempPEBuildManager();
            var result = await mgr.TestProcessAsync(update);

            Assert.Single(result.Inputs);
            Assert.Equal("Settings.Designer.cs", result.Inputs.First());
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Settings.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Single(mgr.CompiledItems);
            Assert.Equal("TempPE\\Settings.Designer.cs.dll", mgr.CompiledItems.First());
        }


        [Fact]
        public async Task Process_BothDesignTimeInputsToSharedOnly_WatchesOneFile()
        {
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Settings.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [ ""Settings.Designer.cs"" ]
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true,
                        ""DesignTime"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            Assert.Empty(result.Inputs);
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Empty(mgr.DirtyItems);
            Assert.Single(mgr.DeletedItems);
            Assert.Equal("Settings.Designer.cs", mgr.DeletedItems.First());
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_BothDesignTimeInputsToNormalOnly_WatchesOneFile()
        {
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Settings.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [ ""Settings.Designer.cs"" ]
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true,
                        ""DesignTime"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            Assert.Single(result.Inputs);
            Assert.Equal("Settings.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Settings.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_InitialProjectLoad_ShouldntCompile()
        {
            using var mgr = new TestTempPEBuildManager();
            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [
                                    ""Form1.cs"",
                                    ""Resources1.Designer.cs""
                                 ]
            },
            ""After"": {
                ""Items"": {
                    ""Form1.cs"": { },
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");

            var configUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
        }
    }
}");

            var result = await mgr.TestProcessAsync(update, configUpdate, null);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }


        [Fact]
        public async Task Process_AddSharedDesignTimeInput_DirtiesAllPEsWithoutCompiling()
        {
            // Initial state is two design time inputs
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] {
                                                "Resources1.Designer.cs",
                                                "Resources2.Designer.cs"
                                                }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [
                                    ""Settings.Designer.cs""
                                 ]
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            Assert.Equal(2, result.Inputs.Count);
            Assert.Contains("Resources1.Designer.cs", result.Inputs);
            Assert.Contains("Resources2.Designer.cs", result.Inputs);
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Equal(2, mgr.DirtyItems.Count);
            Assert.Contains("Resources1.Designer.cs", mgr.DirtyItems);
            Assert.Contains("Resources2.Designer.cs", mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_OneDesignTimeInput_ReturnsOneInput()
        {
            // Initial state is an empty object
            using var mgr = new TestTempPEBuildManager();
            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [
                                    ""Form1.cs"",
                                    ""Resources1.Designer.cs""
                                 ]
            },
            ""After"": {
                ""Items"": {
                    ""Form1.cs"": { },
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");

            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Single(mgr.CompiledItems);
            Assert.Contains("TempPE\\Resources1.Designer.cs.dll", mgr.CompiledItems);
        }


        [Fact]
        public async Task Process_RenamedDesignTimeInput_ReturnsOneInput()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""RenamedItems"": {
                    ""Resources1.Designer.cs"": ""Resources3.Designer.cs"" 
                }
            },
            ""Before"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Resources3.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");

            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources3.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources3.Designer.cs", mgr.DirtyItems.First());
            Assert.Single(mgr.DeletedItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DeletedItems.First());
            Assert.Single(mgr.CompiledItems);
            Assert.Contains("TempPE\\Resources3.Designer.cs.dll", mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_RenamedSharedDesignTimeInput_ReturnsOneInput()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""RenamedItems"": {
                    ""Settings.Designer.cs"": ""Settings_New.Designer.cs"" 
                }
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Settings_New.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");

            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings_New.Designer.cs", result.SharedInputs.First());
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }


        [Fact]
        public async Task Process_DesignInputChangedToTrue_ReturnsOneInput()
        {
            // Initial state is an empty object
            using var mgr = new TestTempPEBuildManager();
            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Resources1.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Single(mgr.CompiledItems);
            Assert.Contains("TempPE\\Resources1.Designer.cs.dll", mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_DesignInputChangedToFalse_ReturnsOneInput()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Resources1.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                    }
                }
            }
        }
    }
}");

            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Empty(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Empty(mgr.DirtyItems);
            Assert.Single(mgr.DeletedItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DeletedItems.First());
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_SharedDesignInputChangedToTrue_DirtiesAllPEsWithoutCompiling()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Settings.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_SharedDesignInputChangedToFalse_DirtiesAllPEsWithoutCompiling()
        {
            // Initial state is a single design time input and a single shared input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Settings.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }


        [Fact]
        public async Task Process_NonInputPropertyChanged_ReturnsOneInput()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Resources1.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_NonInputPropertyChangedOnSharedItem_ReturnsOneInput()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Settings.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_RootNamespaceChanged_DirtiesAllPEs()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs", "Resources2.Designer.cs" }, null);

            var compileUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": { }
    }
}");

            var configUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""RootNamespace"" ]
            },
            ""After"": {
                ""Properties"": {
                    ""RootNamespace"": ""After_Namespace""
                }
            }
        }
    }
}");

            var result = await mgr.TestProcessAsync(compileUpdate, configUpdate);

            // One file should have been added
            Assert.Equal(2, result.Inputs.Count);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Equal("Resources2.Designer.cs", result.Inputs.Last());
            Assert.Empty(result.SharedInputs);
            Assert.Equal(2, mgr.DirtyItems.Count);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Equal("Resources2.Designer.cs", mgr.DirtyItems.Last());
            Assert.Empty(mgr.DeletedItems);
            Assert.Equal(2, mgr.CompiledItems.Count);
            Assert.Equal("TempPE\\Resources1.Designer.cs.dll", mgr.CompiledItems.First());
            Assert.Equal("TempPE\\Resources2.Designer.cs.dll", mgr.CompiledItems.Last());
        }


        [Fact]
        public async Task Process_NonInputPropertyChangedOnNonItem_ReturnsEmptyCollections()
        {
            // Initial state is an empty object
            using var mgr = new TestTempPEBuildManager();
            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""ChangedItems"": [
                    ""Form1.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Form1.Designer.cs"": {
                    }
                }
            },
            ""After"": {
                ""Items"": {
                    ""Form1.Designer.cs"": {
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // Nothing should have been added
            Assert.Empty(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_OneDesignTimeInputAndOneShared_ReturnsOneInputEach()
        {
            // Initial state is an empty object
            using var mgr = new TestTempPEBuildManager();
            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [
                    ""Form1.cs"",
                    ""Resources1.Designer.cs"",
                    ""Settings.Designer.cs""
                ]
            },
            ""After"": {
                ""Items"": {
                    ""Form1.cs"": {
                    },
                    ""Resources1.Designer.cs"": {
                        ""DesignTime"": true
                    },
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Single(mgr.CompiledItems);
            Assert.Contains("TempPE\\Resources1.Designer.cs.dll", mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_AddDesignTimeInput_ReturnsCorrectInputs()
        {
            // Initial state is a single design time input
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AddedItems"": [
                    ""Resources2.Designer.cs""
                ]
            },
            ""After"": {
                ""Items"": {
                    ""Resources2.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // Should be two design time files now
            Assert.Equal(2, result.Inputs.Count);
            Assert.Contains("Resources1.Designer.cs", result.Inputs);
            Assert.Contains("Resources2.Designer.cs", result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources2.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
            Assert.Single(mgr.CompiledItems);
            Assert.Contains("TempPE\\Resources2.Designer.cs.dll", mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_RemoveDesignTimeInput_ReturnsCorrectInput()
        {
            // Initial state is two design time inputs
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] {
                                                "Resources1.Designer.cs",
                                                "Resources2.Designer.cs"
                                              }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""RemovedItems"": [
                    ""Resources2.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Resources2.Designer.cs"": {
                        ""DesignTime"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been removed
            Assert.Single(result.Inputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Empty(result.SharedInputs);
            Assert.Empty(mgr.DirtyItems);
            Assert.Single(mgr.DeletedItems);
            Assert.Equal("Resources2.Designer.cs", mgr.DeletedItems.First());
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_RemoveSharedDesignTimeInput_ReturnsCorrectInput()
        {
            // Initial state is two design time inputs
            using var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] {
                                                "Resources1.Designer.cs",
                                                "Resources2.Designer.cs"
                                              },
new[] {
                                                "Settings.Designer.cs"
});

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""RemovedItems"": [
                    ""Settings.Designer.cs""
                ]
            },
            ""Before"": {
                ""Items"": {
                    ""Settings.Designer.cs"": {
                        ""DesignTimeSharedInput"": true
                    }
                }
            }
        }
    }
}");
            var result = await mgr.TestProcessAsync(update);

            // One file should have been removed
            Assert.Equal(2, result.Inputs.Count);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First());
            Assert.Equal("Resources2.Designer.cs", result.Inputs.Last());
            Assert.Empty(result.SharedInputs);
            Assert.Equal(2, mgr.DirtyItems.Count);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Equal("Resources2.Designer.cs", mgr.DirtyItems.Last());
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        internal class TestTempPEBuildManager : TempPEBuildManager
        {
            public List<string> DeletedItems { get; } = new List<string>();
            public List<string> DirtyItems { get; } = new List<string>();
            public List<string> CompiledItems { get; } = new List<string>();

            public TestTempPEBuildManager()
                : this(null)
            {
            }

            public TestTempPEBuildManager(IFileSystem fileSystem)
                : base(IProjectThreadingServiceFactory.Create(),
                      IUnconfiguredProjectCommonServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create()),
                      IActiveWorkspaceProjectContextHostFactory.Create(),
                      IActiveConfiguredProjectSubscriptionServiceFactory.Create(),
                      null,
                      fileSystem,
                      IProjectFaultHandlerServiceFactory.Create(),
                      null)
            {
                BuildManager = new TestBuildManager(this);

                AppliedValue = new ProjectVersionedValue<DesignTimeInputsItem>(new DesignTimeInputsItem() { OutputPath = "TempPE" }, ImmutableDictionary<NamedIdentity, IComparable>.Empty);
            }

            protected override Task CompileTempPEAsync(HashSet<string> filesToCompile, string outputFileName, CancellationToken token)
            {
                CompiledItems.Add(outputFileName);
                return Task.CompletedTask;
            }

            protected override ITaskDelayScheduler CreateTaskScheduler()
            {
                return new TaskDelayScheduler(TimeSpan.Zero, IProjectThreadingServiceFactory.Create(), default);
            }

            public Task<DesignTimeInputsItem> TestProcessAsync(IProjectSubscriptionUpdate compileUpdate)
            {
                var configUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
        }
    }
}");

                return TestProcessAsync(compileUpdate, configUpdate);
            }

            public Task<DesignTimeInputsItem> TestProcessAsync(IProjectSubscriptionUpdate compileUpdate, IProjectSubscriptionUpdate configurationGeneralUpdate)
            {
                return TestProcessAsync(compileUpdate, configurationGeneralUpdate, new DesignTimeInputsDelta());
            }

            public async Task<DesignTimeInputsItem> TestProcessAsync(IProjectSubscriptionUpdate compileUpdate, IProjectSubscriptionUpdate configurationGeneralUpdate, DesignTimeInputsDelta previousOutput)
            {
                var input = IProjectVersionedValueFactory.Create(Tuple.Create(compileUpdate, configurationGeneralUpdate));

                // We always pretend this isn't the first process, which occurs on project load, because we have SetInputs for that
                var result = await base.PreprocessAsync(input, previousOutput);

                await base.ApplyAsync(result);
                return AppliedValue.Value;
            }

            protected override HashSet<string> GetFilesToCompile(string fileName, ImmutableHashSet<string> sharedInputs)
            {
                return new HashSet<string>(sharedInputs.Concat(new[] { fileName }));
            }

            public async Task SetInputs(string[] designTimeInputs, string[] sharedDesignTimeInputs)
            {
                designTimeInputs ??= Array.Empty<string>();
                sharedDesignTimeInputs ??= Array.Empty<string>();
                await base.ApplyAsync(new DesignTimeInputsDelta
                {
                    AddedItems = ImmutableArray.CreateRange<string>(designTimeInputs),
                    AddedSharedItems = ImmutableArray.CreateRange<string>(sharedDesignTimeInputs)
                });

                DeletedItems.Clear();
                DirtyItems.Clear();
                CompiledItems.Clear();
            }

            private class TestBuildManager : VSBuildManager
            {
                private readonly TestTempPEBuildManager _mgr;

                internal TestBuildManager(TestTempPEBuildManager mgr)
                    : base(IProjectThreadingServiceFactory.Create(), IUnconfiguredProjectCommonServicesFactory.Create(UnconfiguredProjectFactory.Create()))
                {
                    _mgr = mgr;
                }

                internal override void OnDesignTimeOutputDeleted(string outputMoniker)
                {
                    _mgr.DeletedItems.Add(outputMoniker);
                }

                internal override void OnDesignTimeOutputDirty(string outputMoniker)
                {
                    _mgr.DirtyItems.Add(outputMoniker);
                }
            }
        }
    }
}
