using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;
using Microsoft.VisualStudio.Threading.Tasks;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.TempPE
{
    public class TempPEBuildManagerTests
    {
        [Fact]
        public void Preprocess_NoDesignTimeInput_ReturnsEmptyCollections()
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
            var result = mgr.TestPreprocess(IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update)));

            // Should have empty collections
            Assert.Empty(result.Inputs);
            Assert.Empty(result.SharedInputs);
        }

        [Fact]
        public void Preprocess_OneDesignTimeInput_ReturnsOneInput()
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
            var result = mgr.TestPreprocess(IProjectVersionedValueFactory.Create(Tuple.Create(snapshot, update)));

            // One file should have been added
            Assert.Single(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First().Key);
        }

        [Fact]
        public void Preprocess_AddDesignTimeInput_ReturnsCorrectInputs()
        {
            // Initial state is a single design time input called Resources1.Designer.cs
            var baseDirectory = Directory.GetCurrentDirectory();
            var mgr = new TestTempPEBuildManager();
            mgr.SetInputs(baseDirectory, new[] { "Resources1.Designer.cs" }, null);

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
            var result = mgr.TestPreprocess(IProjectVersionedValueFactory.Create(Tuple.Create(projectState, update)));

            // Should be two design time files now
            Assert.Equal(2, result.Inputs.Count);
            Assert.Empty(result.SharedInputs);
            Assert.Contains("Resources1.Designer.cs", result.Inputs.Keys);
            Assert.Contains("Resources2.Designer.cs", result.Inputs.Keys);
        }

        [Fact]
        public void Preprocess_RemoveDesignTimeInput_ReturnsCorrectInput()
        {
            // Initial state is two design time inputs
            var baseDirectory = Directory.GetCurrentDirectory();
            var mgr = new TestTempPEBuildManager();
            mgr.SetInputs(baseDirectory,
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
            var result = mgr.TestPreprocess(IProjectVersionedValueFactory.Create(Tuple.Create(projectState, update)));

            // One file should have been removed
            Assert.Single(result.Inputs);
            Assert.Empty(result.SharedInputs);
            Assert.Equal("Resources1.Designer.cs", result.Inputs.First().Key);
        }

        internal class TestTempPEBuildManager : TempPEBuildManager
        {
            public TestTempPEBuildManager()
                : base(IProjectThreadingServiceFactory.Create(), IUnconfiguredProjectCommonServicesFactory.Create(), ILanguageServiceHostFactory.Create())
            {
            }

            public DesignTimeInputs TestPreprocess(IProjectVersionedValue<Tuple<IProjectSnapshot, IProjectSubscriptionUpdate>> input)
            {
                var result = base.PreprocessAsync(input, null).Result;
                // apply the result in case the test does chained calls to simulate real updates
                base.ApplyAsync(result);
                return result;
            }

            public void SetInputs(string baseDirectory, string[] designTimeInputs, string[] sharedDesignTimeInputs)
            {
                designTimeInputs = designTimeInputs ?? Array.Empty<string>();
                sharedDesignTimeInputs = sharedDesignTimeInputs ?? Array.Empty<string>();
                ApplyAsync(new DesignTimeInputs
                {
                    Inputs = ImmutableDictionary.CreateRange<string, (string, CancellationSeries)>(StringComparers.Paths, designTimeInputs.Select(i => new KeyValuePair<string, (string, CancellationSeries)>(i, (Path.Combine(baseDirectory, i), new CancellationSeries())))),
                    SharedInputs = ImmutableDictionary.CreateRange<string, (string, CancellationSeries)>(StringComparers.Paths, sharedDesignTimeInputs.Select(i => new KeyValuePair<string, (string, CancellationSeries)>(i, (Path.Combine(baseDirectory, i), new CancellationSeries())))),
                    DataSourceVersions = ImmutableDictionary<NamedIdentity, IComparable>.Empty
                });
            }
        }
    }
}
