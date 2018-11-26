using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
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
            var result = await mgr.TestProcessAsync(IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update)));

            // Should have empty collections
            Assert.Empty(result.Inputs);
            Assert.Empty(result.SharedInputs);
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
            var result = await mgr.TestProcessAsync(IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update)));

            // Should have empty collections
            Assert.Empty(result.Inputs);
            Assert.Single(result.SharedInputs);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
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
            var result = await mgr.TestProcessAsync(IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update)));

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First().Key);
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
            var result = await mgr.TestProcessAsync(IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update)));

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Single(result.SharedInputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First().Key);
            Assert.Equal("Settings.Designer.cs", result.SharedInputs.First());
        }

        [Fact]
        public async Task Preprocess_AddDesignTimeInput_ReturnsCorrectInputs()
        {
            // Initial state is a single design time input called Resources1.Designer.cs
            var baseDirectory = Directory.GetCurrentDirectory();
            var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(baseDirectory, new[] { "Resources1.Designer.cs" }, null);

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
            var result = await mgr.TestProcessAsync(IProjectVersionedValueFactory.Create(Tuple.Create(projectState, update)));

            // Should be two design time files now
            Assert.Equal(2, result.Inputs.Count);
            Assert.Empty(result.SharedInputs);
            Assert.Contains("Resources1.Designer.cs", result.Inputs.Keys);
            Assert.Contains("Resources2.Designer.cs", result.Inputs.Keys);
        }

        [Fact]
        public async Task Preprocess_RemoveDesignTimeInput_ReturnsCorrectInput()
        {
            // Initial state is two design time inputs
            var baseDirectory = Directory.GetCurrentDirectory();
            var mgr = new TestTempPEBuildManager();
            await mgr.SetInputs(baseDirectory,
                          new[] {
                                    "Resources1.Designer.cs",
                                    "Resources2.Designer.cs"
                                },
                          null);

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
            var result = await mgr.TestProcessAsync(IProjectVersionedValueFactory.Create(Tuple.Create(projectState, update)));

            // One file should have been removed
            Assert.Single(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First().Key);
        }

        internal class TestTempPEBuildManager : TempPEBuildManager
        {
            private bool _initialized;

            public TestTempPEBuildManager()
                : base(IProjectThreadingServiceFactory.Create(), IUnconfiguredProjectCommonServicesFactory.Create(), ILanguageServiceHostFactory.Create())
            {
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

            public async Task<DesignTimeInputsItem> TestProcessAsync(IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>> input)
            {
                await InitializeAsync();

                var result = await base.PreprocessAsync(input, null);
                await base.ApplyAsync(result);
                return AppliedValue.Value;
            }


            public async Task SetInputs(string baseDirectory, string[] designTimeInputs, string[] sharedDesignTimeInputs)
            {
                await InitializeAsync();

                designTimeInputs = designTimeInputs ?? Array.Empty<string>();
                sharedDesignTimeInputs = sharedDesignTimeInputs ?? Array.Empty<string>();
                await base.ApplyAsync(new DesignTimeInputsDelta
                {
                    AddedItems = ImmutableList.CreateRange<string>(designTimeInputs),
                    AddedSharedItems = ImmutableHashSet.CreateRange<string>(StringComparers.Paths, sharedDesignTimeInputs)
                });
            }
        }
    }
}
