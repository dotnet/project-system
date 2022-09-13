// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading.Tasks;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class DesignTimeInputsChangeTrackerTests : IDisposable
    {
        private const int TestTimeoutMillisecondsDelay = 1000;

        private string? _lastIntermediateOutputPath;
        private readonly string _projectFolder = @"C:\MyProject";
        private string _intermediateOutputPath = "MyOutput";
        private readonly DesignTimeInputsChangeTracker _changeTracker;

        private readonly List<DesignTimeInputSnapshot> _outputProduced = new();
        private readonly TaskCompletionSource _outputProducedSource = new();
        private int _expectedOutput;

        [Fact]
        public async Task SingleDesignTimeInput_ProducesOutput()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyOutput(1, () =>
            {
                SendDesignTimeInputs(inputs);
            });

            Assert.Single(_outputProduced[0].Inputs);
            Assert.Empty(_outputProduced[0].SharedInputs);
            Assert.Contains("File1.cs", _outputProduced[0].Inputs);
            Assert.Single(_outputProduced[0].ChangedInputs);
            Assert.Equal("File1.cs", _outputProduced[0].ChangedInputs[0].File);
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);
        }

        [Fact]
        public async Task FileChange_WithoutInputs_NoOutput()
        {
            await VerifyOutput(0, () =>
            {
                SendFileChange("File1.cs");
            });
        }

        [Fact]
        public async Task SingleDesignTimeInput_Changes_ChangedTwice()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyOutput(2, () =>
            {
                SendDesignTimeInputs(inputs);

                SendFileChange("File1.cs");
            });

            Assert.Single(_outputProduced[0].ChangedInputs);
            Assert.Single(_outputProduced[0].Inputs);
            Assert.Empty(_outputProduced[0].SharedInputs);
            Assert.Equal("File1.cs", _outputProduced[0].ChangedInputs[0].File);
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);

            Assert.Single(_outputProduced[1].ChangedInputs);
            Assert.Single(_outputProduced[1].Inputs);
            Assert.Empty(_outputProduced[1].SharedInputs);
            Assert.Equal("File1.cs", _outputProduced[1].ChangedInputs[0].File);
            Assert.False(_outputProduced[1].ChangedInputs[0].IgnoreFileWriteTime);
        }

        [Fact]
        public async Task TwoDesignTimeInputs_SharedInputChanges_BothChanged()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { "SharedFile.cs" });

            await VerifyOutput(2, () =>
            {
                SendDesignTimeInputs(inputs);

                SendFileChange("SharedFile.cs");
            });

            Assert.Equal(2, _outputProduced[0].ChangedInputs.Length);
            Assert.Equal(2, _outputProduced[0].Inputs.Count);
            Assert.Single(_outputProduced[0].SharedInputs);
            Assert.Contains("File1.cs", _outputProduced[0].ChangedInputs.Select(f => f.File));
            Assert.Contains("File2.cs", _outputProduced[0].ChangedInputs.Select(f => f.File));
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);

            Assert.Equal(2, _outputProduced[1].ChangedInputs.Length);
            Assert.Equal(2, _outputProduced[1].Inputs.Count);
            Assert.Single(_outputProduced[1].SharedInputs);
            Assert.Contains("File1.cs", _outputProduced[1].ChangedInputs.Select(f => f.File));
            Assert.Contains("File2.cs", _outputProduced[1].ChangedInputs.Select(f => f.File));
            Assert.False(_outputProduced[1].ChangedInputs[0].IgnoreFileWriteTime);
        }

        [Fact]
        public async Task SingleDesignTimeInput_Removed_ShouldntBeChanged()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyOutput(2, () =>
            {
                SendDesignTimeInputs(inputs);

                SendDesignTimeInputs(new DesignTimeInputs(new string[] { }, new string[] { }));
            });

            // First update should include the file
            Assert.Single(_outputProduced[0].ChangedInputs);
            Assert.Single(_outputProduced[0].Inputs);
            Assert.Empty(_outputProduced[0].SharedInputs);
            Assert.Equal("File1.cs", _outputProduced[0].ChangedInputs[0].File);
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);

            // Second shouldn't
            Assert.Empty(_outputProduced[1].ChangedInputs);
            Assert.Empty(_outputProduced[1].Inputs);
            Assert.Empty(_outputProduced[1].SharedInputs);
        }

        [Fact]
        public async Task SingleDesignTimeInput_AnotherAdded_OneInEachChange()
        {
            await VerifyOutput(2, () =>
            {
                SendDesignTimeInputs(new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { }));

                SendDesignTimeInputs(new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { }));
            });

            Assert.Single(_outputProduced[0].ChangedInputs);
            Assert.Single(_outputProduced[0].Inputs);
            Assert.Empty(_outputProduced[0].SharedInputs);
            Assert.Equal("File1.cs", _outputProduced[0].ChangedInputs[0].File);
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);

            Assert.Single(_outputProduced[1].ChangedInputs);
            Assert.Equal(2, _outputProduced[1].Inputs.Count);
            Assert.Empty(_outputProduced[1].SharedInputs);
            Assert.Equal("File2.cs", _outputProduced[1].ChangedInputs[0].File);
            Assert.False(_outputProduced[1].ChangedInputs[0].IgnoreFileWriteTime);
        }

        [Fact]
        public async Task TwoDesignTimeInputs_OutputPathChanged_BothChanged()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { });

            await VerifyOutput(2, () =>
            {
                SendDesignTimeInputs(inputs);

                SendDesignTimeInputs(inputs, "NewOutputPath");
            });

            Assert.Equal(2, _outputProduced[0].ChangedInputs.Length);
            Assert.Equal(2, _outputProduced[0].Inputs.Count);
            Assert.Empty(_outputProduced[0].SharedInputs);
            Assert.Contains("File1.cs", _outputProduced[0].ChangedInputs.Select(f => f.File));
            Assert.Contains("File2.cs", _outputProduced[0].ChangedInputs.Select(f => f.File));
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);

            Assert.Equal(2, _outputProduced[1].ChangedInputs.Length);
            Assert.Equal(2, _outputProduced[1].Inputs.Count);
            Assert.Empty(_outputProduced[1].SharedInputs);
            Assert.Contains("File1.cs", _outputProduced[1].ChangedInputs.Select(f => f.File));
            Assert.Contains("File2.cs", _outputProduced[1].ChangedInputs.Select(f => f.File));
            Assert.True(_outputProduced[1].ChangedInputs[0].IgnoreFileWriteTime);
        }

        [Fact]
        public async Task NewSharedDesignTimeInput_ModifiedInThePast_ShouldIgnoreFileWriteTime()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyOutput(2, () =>
            {
                SendDesignTimeInputs(inputs);

                inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { "OldFile.cs" });

                SendDesignTimeInputs(inputs);
            });

            Assert.Single(_outputProduced[0].ChangedInputs);
            Assert.Single(_outputProduced[0].Inputs);
            Assert.Empty(_outputProduced[0].SharedInputs);
            Assert.Equal("File1.cs", _outputProduced[0].ChangedInputs[0].File);
            Assert.False(_outputProduced[0].ChangedInputs[0].IgnoreFileWriteTime);

            Assert.Single(_outputProduced[1].ChangedInputs);
            Assert.Single(_outputProduced[1].Inputs);
            Assert.Single(_outputProduced[1].SharedInputs);
            Assert.Equal("File1.cs", _outputProduced[1].ChangedInputs[0].File);
            Assert.True(_outputProduced[1].ChangedInputs[0].IgnoreFileWriteTime);
        }

        public DesignTimeInputsChangeTrackerTests()
        {
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            using var designTimeInputsSource = ProjectValueDataSourceFactory.Create<DesignTimeInputs>(services);

            var dataSourceMock = new Mock<IDesignTimeInputsDataSource>();
            dataSourceMock.SetupGet(s => s.SourceBlock)
                .Returns(designTimeInputsSource.SourceBlock);

            using var fileWatcherSource = ProjectValueDataSourceFactory.Create<string[]>(services);

            var watcherMock = new Mock<IDesignTimeInputsFileWatcher>();
            watcherMock.SetupGet(s => s.SourceBlock)
                .Returns(fileWatcherSource.SourceBlock);

            var threadingService = IProjectThreadingServiceFactory.Create();
            var projectSubscriptionService = IActiveConfiguredProjectSubscriptionServiceFactory.Create();
            var unconfiguredProject = UnconfiguredProjectFactory.Create(
                fullPath: Path.Combine(_projectFolder, "MyTestProj.csproj"),
                projectAsynchronousTasksService: IProjectAsynchronousTasksServiceFactory.Create());

            var unconfiguredProjectServices = IUnconfiguredProjectServicesFactory.Create(
                   projectService: IProjectServiceFactory.Create(
                       services: ProjectServicesFactory.Create(
                           threadingService: threadingService)));

            _changeTracker = new DesignTimeInputsChangeTracker(unconfiguredProject,
                                      unconfiguredProjectServices,
                                      threadingService,
                                      projectSubscriptionService,
                                      dataSourceMock.Object,
                                      watcherMock.Object)
            {
                AllowSourceBlockCompletion = true
            };

            // Create a block to receive the output
            var receiver = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<DesignTimeInputSnapshot>>(OutputProduced);
            _changeTracker.SourceBlock.LinkTo(receiver, DataflowOption.PropagateCompletion);
        }

        private void OutputProduced(IProjectVersionedValue<DesignTimeInputSnapshot> val)
        {
            _outputProduced.Add(val.Value);

            if (_outputProduced.Count == _expectedOutput)
            {
                _outputProducedSource?.SetResult();
            }
        }

        private async Task VerifyOutput(int numberOfOutputExpected, Action actionThatCausesCompilation)
        {
            _expectedOutput = numberOfOutputExpected;

            actionThatCausesCompilation();

            // complete out block so that it produces output
            _changeTracker.SourceBlock.Complete();
            await _changeTracker.SourceBlock.Completion;

            // The timeout here is annoying, but even though our test is "smart" and waits for data, unfortunately if the code breaks the test is more likely to hang than fail
            var delay = Task.Delay(TestTimeoutMillisecondsDelay);

            if (await Task.WhenAny(_outputProducedSource.Task, delay) == delay)
            {
                if (_outputProduced.Count != numberOfOutputExpected)
                {
                    throw new AssertActualExpectedException(numberOfOutputExpected, _outputProduced.Count, $"Timed out after {TestTimeoutMillisecondsDelay}ms");
                }
            }
        }

        private void SendDesignTimeInputs(DesignTimeInputs inputs, string? intermediateOutputPath = null)
        {
            _intermediateOutputPath = intermediateOutputPath ?? _intermediateOutputPath;

            var ruleUpdate =
                """
                {
                    "ProjectChanges": {
                        "ConfigurationGeneral": {
                            "Difference": {
                                "ChangedProperties": [

                """;

            if (_lastIntermediateOutputPath != _intermediateOutputPath)
            {
                ruleUpdate +=
                    """
                                        "IntermediateOutputPath",

                    """;
            }

            // root namespace and project folder have changed if its the first time we've sent inputs
            if (_lastIntermediateOutputPath is null)
            {
                ruleUpdate +=
                    """
                                       "ProjectDir",
                                       "RootNamespace"

                    """;
            }

            ruleUpdate = ruleUpdate.TrimEnd(',');
            ruleUpdate +=
                $$"""
                                ]
                            },
                            "After": {
                                "Properties": {
                                    "ProjectDir": "{{_projectFolder.Replace("\\", "\\\\")}}",
                                    "IntermediateOutputPath": "{{_intermediateOutputPath.Replace("\\", "\\\\")}}",
                                    "RootNamespace": "MyNamespace"
                                }
                            }
                        }
                    }
                }
                """;
            IProjectSubscriptionUpdate subscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(ruleUpdate);

            _lastIntermediateOutputPath = _intermediateOutputPath;

            _changeTracker.ProcessDataflowChanges(new ProjectVersionedValue<ValueTuple<DesignTimeInputs, IProjectSubscriptionUpdate>>(new ValueTuple<DesignTimeInputs, IProjectSubscriptionUpdate>(inputs, subscriptionUpdate), ImmutableDictionary<NamedIdentity, IComparable>.Empty));
        }

        private void SendFileChange(params string[] files)
        {
            _changeTracker.ProcessFileChangeNotification(new ProjectVersionedValue<string[]>(files, ImmutableDictionary<NamedIdentity, IComparable>.Empty));
        }

        public void Dispose()
        {
            _changeTracker.Dispose();
        }
    }
}
