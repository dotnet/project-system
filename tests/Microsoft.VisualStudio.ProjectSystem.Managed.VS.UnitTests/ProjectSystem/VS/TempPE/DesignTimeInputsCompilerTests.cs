// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading.Tasks;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class DesignTimeInputsCompilerTests : IDisposable
    {
        private const int TestTimeoutMillisecondsDelay = 1000;

        private DesignTimeInputs? _designTimeInputs;
        private readonly string _projectFolder = @"C:\MyProject";
        private readonly string _outputPath = @"C:\MyProject\MyOutput\TempPE";
        private readonly IFileSystemMock _fileSystem;
        private readonly DesignTimeInputsCompiler _manager;

        // For tracking compilation events that occur, to verify
        private readonly List<(string OutputFileName, string[] SourceFiles)> _compilationResults = new();
        private TaskCompletionSource? _compilationOccurredCompletionSource;
        private int _expectedCompilations;
        private Func<string, ISet<string>, bool> _compilationCallback;

        [Fact]
        public async Task SingleDesignTimeInput_ShouldCompile()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyCompilation(1, inputs);

            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[0].OutputFileName);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputDoesntExist_ShouldCompile()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyCompilation(1, inputs);

            string tempPEDescriptionXml = await _manager.BuildDesignTimeOutputAsync("File1.cs", _outputPath, ImmutableHashSet<string>.Empty);

            // This also validates that getting the description didn't force a compile, because the output is up to date
            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[0].OutputFileName);

            Assert.Equal(@"<root>
  <Application private_binpath = ""C:\MyProject\MyOutput\TempPE""/>
  <Assembly
    codebase = ""File1.cs.dll""
    name = ""File1.cs""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>", tempPEDescriptionXml);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputDoesntExist_ShouldCompileWhenGettingXML()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyCompilation(1, inputs);

            // If the first compile didn't happen, our test results won't be valid
            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[0].OutputFileName);

            // Remove the output file, should mean that getting the XML forces a compile
            _fileSystem.RemoveFile(Path.Combine(_outputPath, "File1.cs.dll"));

            string tempPEDescriptionXml = await _manager.BuildDesignTimeOutputAsync("File1.cs", _outputPath, ImmutableHashSet<string>.Empty);

            // Verify a second compile happened
            Assert.Equal(2, _compilationResults.Count);
            Assert.Single(_compilationResults[1].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[1].OutputFileName);

            Assert.Equal(@"<root>
  <Application private_binpath = ""C:\MyProject\MyOutput\TempPE""/>
  <Assembly
    codebase = ""File1.cs.dll""
    name = ""File1.cs""
    version = ""0.0.0.0""
    snapshot_id = ""1""
    replaceable = ""True""
  />
</root>", tempPEDescriptionXml);
        }

        [Fact]
        public async Task SingleDesignTimeInput_Changes_ShouldCompileTwice()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyCompilation(1, inputs);

            await VerifyCompilation(1, "File1.cs");

            Assert.Equal(2, _compilationResults.Count);
            Assert.Single(_compilationResults[0].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[0].OutputFileName);

            Assert.Single(_compilationResults[1].SourceFiles);
            Assert.Contains("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[1].OutputFileName);
        }

        [Fact]
        public async Task NoDesignTimeInputs_NeverCompiles()
        {
            var inputs = new DesignTimeInputs(new string[] { }, new string[] { });

            await VerifyCompilation(0, inputs);

            Assert.Empty(_compilationResults);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputUpToDate_ShouldntCompile()
        {
            _fileSystem.AddFile(Path.Combine(_outputPath, "File1.cs.dll"), DateTime.UtcNow.AddMinutes(10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyCompilation(0, inputs);

            Assert.Empty(_compilationResults);
        }

        [Fact]
        public async Task SingleDesignTimeInput_OutputOutOfDate_ShouldCompile()
        {
            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            _fileSystem.AddFile(Path.Combine(_outputPath, "File1.cs.dll"), DateTime.UtcNow.AddMinutes(-10));

            await VerifyCompilation(1, inputs);

            Assert.Single(_compilationResults);
            Assert.Single(_compilationResults[0].SourceFiles);
        }

        [Fact]
        public async Task SingleDesignTimeInput_Removed_ShouldntCompile()
        {
            _manager.CompileSynchronously = false;

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            await VerifyDLLsCompiled(0, () =>
            {
                SendDesignTimeInputs(inputs);

                SendDesignTimeInputs(new DesignTimeInputs(new string[] { }, new string[] { }));
            });

            Assert.Empty(_compilationResults);
        }

        [Fact]
        public async Task SingleDesignTimeInput_AnotherAdded_ShouldCompileBoth()
        {
            await VerifyDLLsCompiled(2, () =>
            {
                SendDesignTimeInputs(new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { }));

                SendDesignTimeInputs(new DesignTimeInputs(new string[] { "File1.cs", "File2.cs" }, new string[] { }));
            });

            Assert.Equal(2, _compilationResults.Count);
            Assert.Contains("File1.cs", _compilationResults[0].SourceFiles);
            Assert.DoesNotContain("File2.cs", _compilationResults[0].SourceFiles);
            Assert.Contains("File2.cs", _compilationResults[1].SourceFiles);
            Assert.DoesNotContain("File1.cs", _compilationResults[1].SourceFiles);
            Assert.Equal(Path.Combine(_outputPath, "File1.cs.dll"), _compilationResults[0].OutputFileName);
            Assert.Equal(Path.Combine(_outputPath, "File2.cs.dll"), _compilationResults[1].OutputFileName);
        }

        [Fact]
        public async Task SingleDesignTimeInput_CompileFailed_ShouldDeleteOutputFile()
        {
            var outputPath = Path.Combine(_outputPath, "File1.cs.dll");
            _fileSystem.AddFile(outputPath, DateTime.UtcNow.AddMinutes(-10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            // We want our compilation to fail
            _compilationCallback = (x, z) => false;

            await VerifyCompilation(0, inputs);

            Assert.Empty(_compilationResults);
            Assert.False(_fileSystem.FileExists(outputPath));
        }

        [Fact]
        public async Task SingleDesignTimeInput_CompileCancelled_ShouldDeleteOutputFile()
        {
            var outputPath = Path.Combine(_outputPath, "File1.cs.dll");
            _fileSystem.AddFile(outputPath, DateTime.UtcNow.AddMinutes(-10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            // We want our compilation to throw
            _compilationCallback = (x, z) => throw new OperationCanceledException("Boom!");

            await VerifyCompilation(0, inputs);

            Assert.Empty(_compilationResults);
            Assert.False(_fileSystem.FileExists(outputPath));
        }

        [Fact]
        public async Task SingleDesignTimeInput_CompileThrows_ShouldDeleteOutputFile()
        {
            var outputPath = Path.Combine(_outputPath, "File1.cs.dll");
            _fileSystem.AddFile(outputPath, DateTime.UtcNow.AddMinutes(-10));

            var inputs = new DesignTimeInputs(new string[] { "File1.cs" }, new string[] { });

            // We want our compilation to throw
            _compilationCallback = (x, z) => throw new IOException("Boom!");

            await VerifyCompilation(0, inputs);

            Assert.Empty(_compilationResults);
            Assert.False(_fileSystem.FileExists(outputPath));
        }

        public DesignTimeInputsCompilerTests()
        {
            _compilationCallback = CompilationCallBack;

            _fileSystem = new IFileSystemMock();

            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            using var designTimeInputsSource = ProjectValueDataSourceFactory.Create<DesignTimeInputSnapshot>(services);

            var changeTrackerMock = new Mock<IDesignTimeInputsChangeTracker>();
            changeTrackerMock.SetupGet(s => s.SourceBlock)
                .Returns(designTimeInputsSource.SourceBlock);

            var telemetryService = ITelemetryServiceFactory.Create();
            var threadingService = IProjectThreadingServiceFactory.Create();
            var workspaceWriter = IWorkspaceWriterFactory.ImplementProjectContextAccessor(IWorkspaceMockFactory.Create());
            var unconfiguredProject = UnconfiguredProjectFactory.Create(
                fullPath: Path.Combine(_projectFolder, "MyTestProj.csproj"),
                projectAsynchronousTasksService: IProjectAsynchronousTasksServiceFactory.Create());

            var compilerMock = new Mock<ITempPECompiler>();
            compilerMock.Setup(c => c.CompileAsync(It.IsAny<IWorkspaceProjectContext>(), It.IsAny<string>(), It.IsAny<ISet<string>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((IWorkspaceProjectContext context, string outputFile, ISet<string> filesToCompile, CancellationToken token) => _compilationCallback(outputFile, filesToCompile));

            _manager = new DesignTimeInputsCompiler(unconfiguredProject,
                                      workspaceWriter,
                                      threadingService,
                                      changeTrackerMock.Object,
                                      compilerMock.Object,
                                      _fileSystem,
                                      telemetryService)
            {
                CompileSynchronously = true
            };
        }

        private bool CompilationCallBack(string output, ISet<string> files)
        {
            // "Create" our output file
            _fileSystem.AddFile(output);

            _compilationResults.Add((output, files.Select(f => Path.GetFileName(f)).ToArray()));
            if (_compilationResults.Count == _expectedCompilations)
            {
                _compilationOccurredCompletionSource?.SetResult();
            }

            return true;
        }

        private async Task VerifyCompilation(int numberOfDLLsExpected, DesignTimeInputs designTimeInputs)
        {
            await VerifyDLLsCompiled(numberOfDLLsExpected, () => SendDesignTimeInputs(inputs: designTimeInputs));
        }

        private async Task VerifyCompilation(int numberOfDLLsExpected, params string[] filesToChange)
        {
            await Task.Delay(1);

            // Short delay so that files are actually newer than any previous output, since tests run fast
            Thread.Sleep(1);

            // Ensure our input files are in, and up to date, in the mock file system
            foreach (string file in filesToChange)
            {
                var fullFilePath = Path.Combine(_projectFolder, file);
                _fileSystem.AddFile(fullFilePath);
            }

            await VerifyDLLsCompiled(numberOfDLLsExpected, () => SendDesignTimeInputs(changedFiles: filesToChange));
        }

        private async Task VerifyDLLsCompiled(int numberOfDLLsExpected, Action actionThatCausesCompilation)
        {
            int initialCompilations = _compilationResults.Count;
            _expectedCompilations = initialCompilations + numberOfDLLsExpected;
            _compilationOccurredCompletionSource = new TaskCompletionSource();

            actionThatCausesCompilation();

            // Sadly, we need a timeout
            var delay = Task.Delay(TestTimeoutMillisecondsDelay);

            if (await Task.WhenAny(_compilationOccurredCompletionSource.Task, delay) == delay)
            {
                var actualDLLs = _compilationResults.Count - initialCompilations;
                if (numberOfDLLsExpected != actualDLLs)
                {
                    throw new AssertActualExpectedException(numberOfDLLsExpected, actualDLLs, $"Timed out after {TestTimeoutMillisecondsDelay}ms");
                }
            }
        }

        private void SendDesignTimeInputs(DesignTimeInputs? inputs = null, string[]? changedFiles = null)
        {
            // Make everything full paths here, to allow for easier test authoring
            if (inputs is not null)
            {
                inputs = new DesignTimeInputs(inputs.Inputs.Select(f => Path.Combine(_projectFolder, f)), inputs.SharedInputs.Select(f => Path.Combine(_projectFolder, f)));
            }

            _designTimeInputs = inputs ?? _designTimeInputs!;

            // Ensure our input files are in the mock file system
            foreach (string file in _designTimeInputs.Inputs.Concat(_designTimeInputs.SharedInputs))
            {
                if (!_fileSystem.FileExists(file))
                {
                    _fileSystem.AddFile(file);
                }
            }

            IEnumerable<DesignTimeInputFileChange> changes;
            if (changedFiles is not null)
            {
                changes = changedFiles.Select(f => new DesignTimeInputFileChange(Path.Combine(_projectFolder, f), false));
            }
            else
            {
                changes = _designTimeInputs.Inputs.Select(f => new DesignTimeInputFileChange(f, false));
            }

            _manager.ProcessDataflowChanges(new ProjectVersionedValue<DesignTimeInputSnapshot>(new DesignTimeInputSnapshot(_designTimeInputs.Inputs, _designTimeInputs.SharedInputs, changes, _outputPath), ImmutableDictionary<NamedIdentity, IComparable>.Empty));
        }

        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}
