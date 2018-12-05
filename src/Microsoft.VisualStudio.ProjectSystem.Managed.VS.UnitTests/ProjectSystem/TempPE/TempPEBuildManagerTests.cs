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

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.TempPE
{
    public class TempPEBuildManagerTests
    {
        [Fact]
        public void CompilationNeeded_MissingOutput_NeedsRecompile()
        {
            var fileSystem = new IFileSystemMock();
            fileSystem.AddFile("Resource1.Designer.cs", new DateTime(2018, 6, 1));

            var mgr = new TestTempPEBuildManager(fileSystem);

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

            var mgr = new TestTempPEBuildManager(fileSystem);

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

            var mgr = new TestTempPEBuildManager(fileSystem);

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
            // Initial state is an empty object
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": { }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <PropertyGroup>
        <ProjectDir>C:\Code\MyProject</ProjectDir>
        <Configuration>MyConfig</Configuration>
        <TargetFramework>MyFramework</TargetFramework>
        
        <!-- this is just a fake project, but this is what the SDK has in it :) -->
        <IntermediateOutputPath>obj\$(Configuration)\$(TargetFramework)</IntermediateOutputPath>
    </PropertyGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

            // Should have empty collections
            Assert.Equal(@"C:\Code\MyProject\obj\MyConfig\MyFramework\TempPE", result.OutputPath);
        }

        [Fact]
        public async Task Process_NoDesignTimeInput_ReturnsEmptyCollections()
        {
            // Initial state is an empty object
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [ ""Form1.cs"" ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            // Initial state is an empty object
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [ ""Settings.Designer.cs"" ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Settings.Designer.cs"">
            <DesignTimeSharedInput>true</DesignTimeSharedInput>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

            Assert.Empty(result.Inputs);
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_InitialProjectLoad_ShouldntCompile()
        {
            // Initial state is an empty object
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [
                                    ""Form1.cs"",
                                    ""Resources1.Designer.cs""
                                 ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update, null);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] {
                                        "Resources1.Designer.cs",
                                        "Resources2.Designer.cs"
                                      }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [ ""Settings.Designer.cs"" ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Settings.Designer.cs"">
            <DesignTimeSharedInput>true</DesignTimeSharedInput>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            Assert.Empty(mgr.CompiledItems);
        }

        [Fact]
        public async Task Process_OneDesignTimeInput_ReturnsOneInput()
        {
            // Initial state is an empty object
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [
                                    ""Form1.cs"",
                                    ""Resources1.Designer.cs""
                                 ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""RenamedItems"" : {
                    ""Resources1.Designer.cs"": ""Resources3.Designer.cs"" 
                }
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Resources3.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""RenamedItems"" : {
                    ""Settings.Designer.cs"": ""Settings_New.Designer.cs"" 
                }
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Settings_New.Designer.cs"">
            <DesignTimeSharedInput>true</DesignTimeSharedInput>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Resources1.Designer.cs""
                                   ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Resources1.Designer.cs""
                                   ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Resources1.Designer.cs"" />
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Settings.Designer.cs""
                                   ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Settings.Designer.cs"">
            <DesignTimeSharedInput>true</DesignTimeSharedInput>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Settings.Designer.cs""
                                   ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Settings.Designer.cs"" />
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Resources1.Designer.cs""
                                   ]
            }
        }
    }
}");
            // the TempPEManager doesn't actually know about what property changed, so we don't even need to include it the snapshot
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, new[] { "Settings.Designer.cs" });

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Settings.Designer.cs""
                                   ]
            }
        }
    }
}");
            // the TempPEManager doesn't actually know about what property changed, so we don't even need to include it the snapshot
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
        <Compile Include=""Settings.Designer.cs"">
            <DesignTimeSharedInput>true</DesignTimeSharedInput>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs", "Resources2.Designer.cs" }, null, "Before_Namespace");

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
            }
        }
    }
}");
            // the TempPEManager doesn't actually know about what property changed, so we don't even need to include it the snapshot
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <PropertyGroup>
        <RootNamespace>After_Namespace</RootNamespace>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
        <Compile Include=""Resources2.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
        public async Task Process_NonInputPropertyChanged_ReturnsEmptyCollections()
        {
            // Initial state is an empty object
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""ChangedItems"" : [
                                    ""Form1.cs""
                                   ]
            }
        }
    }
}");
            // the TempPEManager doesn't actually know about what property changed, so we don't even need to include it the snapshot
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [
                                    ""Form1.cs"",
                                    ""Resources1.Designer.cs"",
                                    ""Settings.Designer.cs""
                                 ]
            }
        }
    }
}");
            var snapshot = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
        <Compile Include=""Settings.Designer.cs"">
            <DesignTimeSharedInput>true</DesignTimeSharedInput>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(snapshot, update);

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
            var mgr = new TestTempPEBuildManager();

            await mgr.SetInputs(new[] { "Resources1.Designer.cs" }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [ ""Resources2.Designer.cs"" ]
            }
        }
    }
}");
            var projectState = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
        <Compile Include=""Resources2.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(projectState, update);

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
            var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(new[] {
                                        "Resources1.Designer.cs",
                                        "Resources2.Designer.cs"
                                      }, null);

            // Apply our update
            var update = IProjectSubscriptionUpdateFactory.FromJson(@"{
   ""ProjectChanges"": {
        ""Compile"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""RemovedItems"" : [ ""Resources2.Designer.cs"" ]
            }
        }
    }
}");
            var projectState = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(projectState, update);

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
            var mgr = new TestTempPEBuildManager();
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
                ""AnyChanges"": true,
                ""RemovedItems"" : [ ""Settings.Designer.cs"" ]
            }
        }
    }
}");
            var projectState = IProjectSnapshotFactory.FromProjectXml(@"<Project>
    <ItemGroup>
        <Compile Include=""Form1.cs"" />
        <Compile Include=""Resources1.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
        <Compile Include=""Resources2.Designer.cs"">
            <DesignTime>true</DesignTime>
        </Compile>
    </ItemGroup>
</Project>");
            var result = await mgr.TestProcessAsync(projectState, update);

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
            private bool _initialized;

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
                      ILanguageServiceHostFactory.Create(),
                      IActiveConfiguredProjectSubscriptionServiceFactory.Create(),
                      null,
                      null,
                      fileSystem)
            {
                _buildManager = new Lazy<VSBuildManager>(() => new TestBuildManager(this));
            }
            private async Task InitializeAsync()
            {
                if (!_initialized)
                {
                    // Set up the default applied value
                    await InitializeInnerCoreAsync(CancellationToken.None);
                    _initialized = true;
                }
            }

            protected override Task CompileTempPEAsync(HashSet<string> filesToCompile, string outputFileName)
            {
                CompiledItems.Add(outputFileName);
                return Task.CompletedTask;
            }

            public Task<DesignTimeInputsItem> TestProcessAsync(IProjectSnapshot snapshot, IProjectSubscriptionUpdate update)
            {
                return TestProcessAsync(snapshot, update, new DesignTimeInputsDelta());
            }

            public async Task<DesignTimeInputsItem> TestProcessAsync(IProjectSnapshot snapshot, IProjectSubscriptionUpdate update, DesignTimeInputsDelta previousOutput)
            {
                await InitializeAsync();

                var input = IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update));

                // We always pretend this isn't the first process, which occurs on project load, because we have SetInputs for that
                var result = await base.PreprocessAsync(input, previousOutput);

                await base.ApplyAsync(result);
                return AppliedValue.Value;
            }

            protected override HashSet<string> GetFilesToCompile(string fileName, ImmutableHashSet<string> sharedInputs)
            {
                return new HashSet<string>(sharedInputs.Concat(new[] { fileName }));
            }

            public async Task SetInputs(string[] designTimeInputs, string[] sharedDesignTimeInputs, string rootNamespace = "")
            {
                await InitializeAsync();

                designTimeInputs = designTimeInputs ?? Array.Empty<string>();
                sharedDesignTimeInputs = sharedDesignTimeInputs ?? Array.Empty<string>();
                await base.ApplyAsync(new DesignTimeInputsDelta
                {
                    AddedItems = ImmutableArray.CreateRange<string>(designTimeInputs),
                    AddedSharedItems = ImmutableArray.CreateRange<string>(sharedDesignTimeInputs),
                    RootNamespace = rootNamespace,
                    OutputPath = "" // this doesn't matter for most tests, but needs to be non-null for Path.Combine
                });

                DeletedItems.Clear();
                DirtyItems.Clear();
                CompiledItems.Clear();
            }

            private class TestBuildManager : VSBuildManager
            {
                private readonly TestTempPEBuildManager _mgr;

                internal TestBuildManager(TestTempPEBuildManager mgr)
                    : base(IProjectThreadingServiceFactory.Create(), IUnconfiguredProjectCommonServicesFactory.Create(UnconfiguredProjectFactory.Create()), null)
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
