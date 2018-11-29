using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.TempPE
{
    public class TempPEBuildManagerTests
    {
        [Fact]
        public async Task Preprocess_NoDesignTimeInput_ReturnsEmptyCollections()
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
        }

        [Fact]
        public async Task Preprocess_SharedDesignTimeInput_ReturnsOneSharedInput()
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
        }

        [Fact]
        public async Task Preprocess_AddSharedDesignTimeInput_DirtiesAllPEs()
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
        }

        [Fact]
        public async Task Preprocess_OneDesignTimeInput_ReturnsOneInput()
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
        }


        [Fact]
        public async Task Preprocess_RenamedDesignTimeInput_ReturnsOneInput()
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
        }

        [Fact]
        public async Task Preprocess_DesignInputChangedToTrue_ReturnsOneInput()
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
        }

        [Fact]
        public async Task Preprocess_DesignInputChangedToFalse_ReturnsOneInput()
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
        }

        [Fact]
        public async Task Preprocess_InputPropertyChanged_ReturnsOneInput()
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
        }


        [Fact]
        public async Task Preprocess_NonInputPropertyChanged_ReturnsEmptyCollections()
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

            // One file should have been added
            Assert.Empty(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Empty(mgr.DirtyItems);
            Assert.Empty(mgr.DeletedItems);
        }

        [Fact]
        public async Task Preprocess_OneDesignTimeInputAndOneShared_ReturnsOneInputEach()
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
        }

        [Fact]
        public async Task Preprocess_AddDesignTimeInput_ReturnsCorrectInputs()
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
        }

        [Fact]
        public async Task Preprocess_RemoveDesignTimeInput_ReturnsCorrectInput()
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
        }

        internal class TestTempPEBuildManager : TempPEBuildManager
        {
            private bool _initialized;

            public List<string> DeletedItems { get; } = new List<string>();
            public List<string> DirtyItems { get; } = new List<string>();

            public TestTempPEBuildManager()
                : base(IProjectThreadingServiceFactory.Create(),
                      IUnconfiguredProjectCommonServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create()),
                      ILanguageServiceHostFactory.Create(),
                      IActiveConfiguredProjectSubscriptionServiceFactory.Create(), 
                      null,
                      null)
            {
                _buildManager = new TestBuildManager(this);
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

            public async Task<DesignTimeInputsItem> TestProcessAsync(IProjectSnapshot snapshot, IProjectSubscriptionUpdate update)
            {
                await InitializeAsync();

                var input = IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update));

                var result = await base.PreprocessAsync(input, null);
                await base.ApplyAsync(result);
                return AppliedValue.Value;
            }


            public async Task SetInputs(string[] designTimeInputs, string[] sharedDesignTimeInputs)
            {
                await InitializeAsync();

                designTimeInputs = designTimeInputs ?? Array.Empty<string>();
                sharedDesignTimeInputs = sharedDesignTimeInputs ?? Array.Empty<string>();
                await base.ApplyAsync(new DesignTimeInputsDelta
                {
                    AddedItems = ImmutableArray.CreateRange<string>(designTimeInputs),
                    AddedSharedItems = ImmutableHashSet.CreateRange<string>(StringComparers.Paths, sharedDesignTimeInputs)
                });

                DeletedItems.Clear();
                DirtyItems.Clear();
            }

            private class TestBuildManager : VSBuildManager
            {
                private readonly TestTempPEBuildManager _mgr;

                internal TestBuildManager(TestTempPEBuildManager mgr)
                    : base(IUnconfiguredProjectCommonServicesFactory.Create(UnconfiguredProjectFactory.Create()), null)
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
