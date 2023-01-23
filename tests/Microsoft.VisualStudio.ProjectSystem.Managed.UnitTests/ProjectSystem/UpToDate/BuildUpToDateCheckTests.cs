// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;
using Xunit.Abstractions;

#pragma warning disable IDE0055
#pragma warning disable IDE0058

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    public sealed class BuildUpToDateCheckTests : BuildUpToDateCheckTestBase, IDisposable
    {
        private const LogLevel _logLevel = LogLevel.Verbose;

        private const string _projectDir = @"C:\Dev\Solution\Project";
        private const string _projectPath = $@"{_projectDir}\Project.csproj";
        private const string _msBuildAllProjects = $@"{_projectPath};C:\Dev\Solution\Project2\Project2.csproj";
        private const string _outputPathRelative = $@"bin\Debug\";

        private const string _inputPath = $@"{_projectDir}\Input.cs";
        private const string _builtPath = $@"{_projectDir}\{_outputPathRelative}Built.dll";

        private readonly DateTime _projectFileTimeUtc = new(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly List<ITelemetryServiceFactory.TelemetryParameters> _telemetryEvents = new();
        private DateTime? _lastSuccessfulBuildStartTime;

        private readonly BuildUpToDateCheck _buildUpToDateCheck;
        private readonly ITestOutputHelper _output;
        private readonly IFileSystemMock _fileSystem;
        private readonly Mock<IUpToDateCheckStatePersistence> _persistence;

        // Values returned by mocks that may be modified in test cases as needed
        private bool _isTaskQueueEmpty = true;
        private bool _isFastUpToDateCheckEnabledInSettings = true;
        private bool? _isBuildAccelerationEnabled;
        private IEnumerable<(string Path, ImmutableArray<CopyItem> CopyItems)> _copyItems = Enumerable.Empty<(string Path, ImmutableArray<CopyItem> CopyItems)>();
        private bool _isCopyItemsComplete = true;

        private UpToDateCheckConfiguredInput? _state;
        private ITimestampCache? _copyItemTimestamps;

        public BuildUpToDateCheckTests(ITestOutputHelper output)
        {
            _output = output;

            // NOTE most of these mocks are only present to prevent NREs in Initialize

            var inputDataSource = new Mock<IUpToDateCheckConfiguredInputDataSource>(MockBehavior.Strict);
            inputDataSource.SetupGet(o => o.SourceBlock)
                .Returns(DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<UpToDateCheckConfiguredInput>>());

            // Enable "Info" log level, as we assert logged messages in tests
            var projectSystemOptions = new Mock<IProjectSystemOptions>(MockBehavior.Strict);
            projectSystemOptions.Setup(o => o.GetFastUpToDateLoggingLevelAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_logLevel);
            projectSystemOptions.Setup(o => o.GetIsFastUpToDateCheckEnabledAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _isFastUpToDateCheckEnabledInSettings);

            var projectCommonServices = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            var jointRuleSource = new ProjectValueDataSource<IProjectSubscriptionUpdate>(projectCommonServices);
            var sourceItemsRuleSource = new ProjectValueDataSource<IProjectSubscriptionUpdate>(projectCommonServices);
            var projectSnapshotSource = new ProjectValueDataSource<IProjectSnapshot>(projectCommonServices);
            var projectCatalogSource = new ProjectValueDataSource<IProjectCatalogSnapshot>(projectCommonServices);

            var projectSubscriptionService = new Mock<IProjectSubscriptionService>(MockBehavior.Strict);
            projectSubscriptionService.SetupGet(o => o.JointRuleSource).Returns(jointRuleSource);
            projectSubscriptionService.SetupGet(o => o.ProjectSource).Returns(projectSnapshotSource);
            projectSubscriptionService.SetupGet(o => o.SourceItemsRuleSource).Returns(sourceItemsRuleSource);
            projectSubscriptionService.SetupGet(o => o.ProjectCatalogSource).Returns(projectCatalogSource);

            var configuredProjectServices = ConfiguredProjectServicesFactory.Create(projectSubscriptionService: projectSubscriptionService.Object);

            var configuredProject = new Mock<ConfiguredProject>(MockBehavior.Strict);
            configuredProject.SetupGet(c => c.Services).Returns(configuredProjectServices);
            configuredProject.SetupGet(c => c.UnconfiguredProject).Returns(UnconfiguredProjectFactory.Create(fullPath: _projectPath));

            _persistence = new Mock<IUpToDateCheckStatePersistence>(MockBehavior.Strict);
            _persistence
                .Setup(o => o.RestoreLastSuccessfulBuildStateAsync(It.IsAny<string>(), It.IsAny<IImmutableDictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(_lastSuccessfulBuildStartTime));

            var projectAsynchronousTasksService = new Mock<IProjectAsynchronousTasksService>(MockBehavior.Strict);
            projectAsynchronousTasksService.SetupGet(s => s.UnloadCancellationToken).Returns(CancellationToken.None);
            projectAsynchronousTasksService.Setup(s => s.IsTaskQueueEmpty(ProjectCriticalOperation.Build)).Returns(() => _isTaskQueueEmpty);

            var guidService = ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(Guid.NewGuid());

            _fileSystem = new IFileSystemMock();
            _fileSystem.AddFolder(_projectDir);
            _fileSystem.AddFile(_projectPath, _projectFileTimeUtc);

            var upToDateCheckHost = new Mock<IUpToDateCheckHost>(MockBehavior.Strict);

            var copyItemAggregator = new Mock<ICopyItemAggregator>(MockBehavior.Strict);
            copyItemAggregator.Setup(o => o.TryGatherCopyItemsForProject(It.IsAny<string>(), It.IsAny<BuildUpToDateCheck.Log>())).Returns(() => (_copyItems, _isCopyItemsComplete));

            _copyItemTimestamps = new ConcurrentTimestampCache(_fileSystem);

            var solutionBuildContext = new Mock<ISolutionBuildContext>(MockBehavior.Strict);
            solutionBuildContext.SetupGet(o => o.CopyItemTimestamps).Returns(() => _copyItemTimestamps);

            _buildUpToDateCheck = new BuildUpToDateCheck(
                solutionBuildContext.Object,
                inputDataSource.Object,
                projectSystemOptions.Object,
                configuredProject.Object,
                _persistence.Object,
                projectAsynchronousTasksService.Object,
                ITelemetryServiceFactory.Create(_telemetryEvents.Add),
                _fileSystem,
                guidService,
                upToDateCheckHost.Object,
                copyItemAggregator.Object);
        }

        public void Dispose() => _buildUpToDateCheck.Dispose();

        private async Task<UpToDateCheckImplicitConfiguredInput> SetupAsync(
            Dictionary<string, IProjectRuleSnapshotModel>? projectSnapshot = null,
            Dictionary<string, IProjectRuleSnapshotModel>? sourceSnapshot = null,
            bool disableFastUpToDateCheck = false,
            string outDir = _outputPathRelative,
            DateTime? lastSuccessfulBuildStartTimeUtc = null,
            DateTime? lastItemsChangedAtUtc = null,
            UpToDateCheckImplicitConfiguredInput? upToDateCheckImplicitConfiguredInput = null,
            bool itemRemovedFromSourceSnapshot = false)
        {
            upToDateCheckImplicitConfiguredInput ??= UpToDateCheckImplicitConfiguredInput.CreateEmpty(ProjectConfigurationFactory.Create("testConfiguration"));

            _lastSuccessfulBuildStartTime = lastSuccessfulBuildStartTimeUtc;

            projectSnapshot ??= new Dictionary<string, IProjectRuleSnapshotModel>();

            if (!projectSnapshot.ContainsKey(ConfigurationGeneral.SchemaName))
            {
                projectSnapshot[ConfigurationGeneral.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Properties = new Dictionary<string, string>(StringComparers.PropertyNames)
                    {
                        { "MSBuildProjectFullPath", _projectPath },
                        { "MSBuildProjectDirectory", _projectDir },
                        { "MSBuildAllProjects", _msBuildAllProjects },
                        { "OutputPath", outDir },
                        { "OutDir", outDir },
                        { ConfigurationGeneral.DisableFastUpToDateCheckProperty, disableFastUpToDateCheck.ToString() },
                        { ConfigurationGeneral.AccelerateBuildsInVisualStudioProperty, _isBuildAccelerationEnabled?.ToString() ?? "" }
                    }
                };
            }

            UpToDateCheckImplicitConfiguredInput configuredInput = UpdateState(
                upToDateCheckImplicitConfiguredInput,
                projectSnapshot,
                sourceSnapshot,
                itemRemovedFromSourceSnapshot: itemRemovedFromSourceSnapshot);

            if (lastItemsChangedAtUtc is not null)
            {
                configuredInput = configuredInput.WithLastItemsChangedAtUtc(lastItemsChangedAtUtc.Value);
            }

            _state = new UpToDateCheckConfiguredInput(ImmutableArray.Create(configuredInput));

            var subscription = new Mock<BuildUpToDateCheck.ISubscription>(MockBehavior.Strict);

            subscription.Setup(s => s.EnsureInitialized());

            subscription
                .Setup(s => s.UpdateLastSuccessfulBuildStartTimeUtcAsync(It.IsAny<DateTime>(), It.IsAny<bool>()))
                .Callback((DateTime timeUtc, bool isRebuild) => _lastSuccessfulBuildStartTime = timeUtc)
                .Returns(Task.CompletedTask);

            subscription
                .Setup(s => s.RunAsync(It.IsAny<Func<UpToDateCheckConfiguredInput, IUpToDateCheckStatePersistence, CancellationToken, Task<(bool, ImmutableArray<ProjectConfiguration>)>>>(), It.IsAny<CancellationToken>()))
                .Returns(async (Func<UpToDateCheckConfiguredInput, IUpToDateCheckStatePersistence, CancellationToken, Task<(bool, ImmutableArray<ProjectConfiguration>)>> func, CancellationToken token) =>
                {
                    Assumes.NotNull(_state);
                    var result = await func(_state, _persistence.Object, token);
                    return result.Item1;
                });

            subscription.Setup(s => s.Dispose());

            await _buildUpToDateCheck.ActivateAsync();

            _buildUpToDateCheck.TestAccess.SetSubscription(subscription.Object);

            return configuredInput;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsUpToDateCheckEnabledAsync_DelegatesToProjectSystemOptions(bool isEnabled)
        {
            _isFastUpToDateCheckEnabledInSettings = isEnabled;

            Assert.Equal(
                isEnabled,
                await _buildUpToDateCheck.IsUpToDateCheckEnabledAsync(CancellationToken.None));
        }

        [Theory]
        [InlineData(BuildAction.Clean)]
        [InlineData(BuildAction.Compile)]
        [InlineData(BuildAction.Deploy)]
        [InlineData(BuildAction.Link)]
        [InlineData(BuildAction.Package)]
        [InlineData(BuildAction.Rebuild)]
        public async Task IsUpToDateAsync_False_NonBuildAction(BuildAction buildAction)
        {
            var lastBuildTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(lastSuccessfulBuildStartTimeUtc: lastBuildTime);

            await AssertUpToDateAsync(
                """
                Comparing timestamps of inputs and outputs:
                    No build outputs defined.
                Project is up-to-date.
                """);

            await AssertNotUpToDateAsync(buildAction: buildAction);
        }

        [Fact]
        public async Task IsUpToDateAsync_False_BuildTasksActive()
        {
            var lastBuildTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(lastSuccessfulBuildStartTimeUtc: lastBuildTime);

            await AssertUpToDateAsync(
                """
                Comparing timestamps of inputs and outputs:
                    No build outputs defined.
                Project is up-to-date.
                """);

            _isTaskQueueEmpty = false;

            await AssertNotUpToDateAsync(
                $"""
                Critical build tasks are running, not up-to-date.
                """,
                "CriticalTasks");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_ItemsChanged()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1", "ItemPath2")
            };

            var outputTime      = DateTime.UtcNow.AddMinutes(-3);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-2);
            var itemChangedTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangedTime);

            _fileSystem.AddFile(_builtPath, outputTime);

            await AssertNotUpToDateAsync(
                $"""
                The set of project items was changed more recently ({ToLocalTime(itemChangedTime)}) than the last successful build start time ({ToLocalTime(lastBuildTime)}), not up-to-date.
                    Compile item added 'ItemPath1'
                    Compile item added 'ItemPath2'
                """,
                "ProjectItemsChangedSinceLastSuccessfulBuildStart");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Disabled()
        {
            await SetupAsync(disableFastUpToDateCheck: true);

            await AssertNotUpToDateAsync(
                "The 'DisableFastUpToDateCheck' property is 'true', not up-to-date.",
                "Disabled");
        }

        [Fact]
        public async Task ValidateUpToDateAsync_True_Disabled()
        {
            await SetupAsync(disableFastUpToDateCheck: true);

            await ValidateUpToDateAsync();
        }

        [Fact]
        public async Task IsUpToDateAsync_False_OutputItemDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var lastBuildTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(projectSnapshot: projectSnapshot, lastSuccessfulBuildStartTimeUtc: lastBuildTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                Output '{_builtPath}' does not exist, not up-to-date.
                """,
                "OutputNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_ZeroFilesInProjectAfterItemDeletion()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            var inputTime = DateTime.UtcNow.AddMinutes(-5);
            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime = DateTime.UtcNow.AddMinutes(-3);
            var builtTime = DateTime.UtcNow.AddMinutes(-2);

            _fileSystem.AddFile(_inputPath, inputTime);
            _fileSystem.AddFile(_builtPath, builtTime);

            var priorState = await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(builtTime)}). Newest input is '{_inputPath}' ({ToLocalTime(inputTime)}).
                Project is up-to-date.
                """);

            lastBuildTime = DateTime.UtcNow.AddMinutes(-1);
            itemChangeTime = DateTime.UtcNow.AddMinutes(0);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime,
                upToDateCheckImplicitConfiguredInput: priorState,
                itemRemovedFromSourceSnapshot: true);

            await AssertNotUpToDateAsync(
                $"""
                The set of project items was changed more recently ({ToLocalTime(itemChangeTime)}) than the last successful build start time ({ToLocalTime(lastBuildTime)}), not up-to-date.
                    Compile item removed 'Input.cs'
                """,
                "ProjectItemsChangedSinceLastSuccessfulBuildStart");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_InputItemDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            var itemChangedTime = DateTime.UtcNow.AddMinutes(-3);
            var builtTime       = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangedTime);

            _fileSystem.AddFile(_builtPath, builtTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                Input Compile item '{_inputPath}' does not exist and is required, not up-to-date.
                """,
                "InputNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_InputNewerThanBuiltOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            var itemChangedTime = DateTime.UtcNow.AddMinutes(-4);
            var builtTime       = DateTime.UtcNow.AddMinutes(-3);
            var inputTime       = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangedTime);

            _fileSystem.AddFile(_builtPath, builtTime);
            _fileSystem.AddFile(_inputPath, inputTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                Input Compile item '{_inputPath}' is newer ({ToLocalTime(inputTime)}) than earliest output '{_builtPath}' ({ToLocalTime(builtTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_InputChangedBetweenLastBuildStartAndAndEarliestOutput()
        {
            // This test covers a race condition described in https://github.com/dotnet/project-system/issues/4014

            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            // The rule is that no input item should be modified since the last successful build started.

            var t0 = DateTime.UtcNow.AddMinutes(-5); // t0 Output file timestamp
            var t1 = DateTime.UtcNow.AddMinutes(-4); // t1 Input file timestamp
            var t2 = DateTime.UtcNow.AddMinutes(-3); // t2 Check up-to-date (false) and build
            var t3 = DateTime.UtcNow.AddMinutes(-2); // t3 Modify input file (during build)
            var t4 = DateTime.UtcNow.AddMinutes(-1); // t4 Produce first (earliest) output DLL (from t0 input)
                                                     // t5 Check incorrectly claims everything up-to-date, as t3 > t2 (inputTime > lastBuildStartTime)

            _fileSystem.AddFile(_builtPath, t0);
            _fileSystem.AddFile(_inputPath, t1);

            // Run test (t2)
            await SetupAsync(projectSnapshot, sourceSnapshot, lastSuccessfulBuildStartTimeUtc: t2);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                Input Compile item '{_inputPath}' is newer ({ToLocalTime(t1)}) than earliest output '{_builtPath}' ({ToLocalTime(t0)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");

            // Modify input while build in progress (t3)
            _fileSystem.AddFile(_inputPath, t3);

            // Update write time of output (t4)
            _fileSystem.AddFile(_builtPath, t4);

            // Run check again (t5)
            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                Input Compile item '{_inputPath}' ({ToLocalTime(t3)}) has been modified since the last successful build started ({ToLocalTime(t2)}), not up-to-date.
                """,
                "InputModifiedSinceLastSuccessfulBuildStart");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_BuildAfterRebuild()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            // If an input was modified since the last successful build started, not up-to-date.

            var t0 = DateTime.Now.Date;    // t0 Output file timestamp
            var t1 = t0.AddMinutes(1);     // t1 Input file timestamp
                                           //    Check up-to-date (false)
            var t2 = t0.AddMinutes(2);     // t2 Start and complete a successful build)
            var t3 = t0.AddMinutes(3);     // t3 Modify input file
            var t4 = t0.AddMinutes(4);     // t4 Rebuild (firing build events but not checking up-to-date)
            var t5 = t0.AddMinutes(5);     // t5 Produce first (earliest) output DLL (from t0 input)
                                           // t6 Check correctly claims everything up-to-date, as t3 < t5

            _fileSystem.AddFile(_builtPath, t0);
            _fileSystem.AddFile(_inputPath, t1);

            // Run test (t2)
            await SetupAsync(projectSnapshot, sourceSnapshot, lastSuccessfulBuildStartTimeUtc: t0.AddMinutes(-1));

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                Input Compile item '{_inputPath}' is newer ({ToLocalTime(t1)}) than earliest output '{_builtPath}' ({ToLocalTime(t0)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");

            // Build (t2)
            ((IProjectBuildEventListener)_buildUpToDateCheck).NotifyBuildStarting(buildStartTimeUtc: t2);
            await ((IProjectBuildEventListener)_buildUpToDateCheck).NotifyBuildCompletedAsync(wasSuccessful: true, isRebuild: false);

            // Modify input (t3)
            _fileSystem.AddFile(_inputPath, t3);

            // Rebuild (t4)
            ((IProjectBuildEventListener)_buildUpToDateCheck).NotifyBuildStarting(buildStartTimeUtc: t4);
            await ((IProjectBuildEventListener)_buildUpToDateCheck).NotifyBuildCompletedAsync(wasSuccessful: true, isRebuild: true);

            // Update output (t5)
            _fileSystem.AddFile(_builtPath, t5);

            // Run check again (t6)
            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(t5)}). Newest input is '{_inputPath}' ({ToLocalTime(t3)}).
                Project is up-to-date.
                """);
        }

        [Fact]
        public async Task IsUpToDateAsync_True_NewProjectRebuiltThenBuilt()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            // The rule is that no input item should be modified since the last successful build started.

            var t0 = DateTime.UtcNow.AddMinutes(-3); // t0 Input file timestamp
            var t1 = DateTime.UtcNow.AddMinutes(-2); // t1 Rebuild (sets output file timestamp)
            var t2 = DateTime.UtcNow.AddMinutes(-1); // t2 Check up-to-date (true)

            _fileSystem.AddFile(_inputPath, t0);

            _fileSystem.AddFile(_builtPath, t1);

            await SetupAsync(projectSnapshot, sourceSnapshot, lastSuccessfulBuildStartTimeUtc: t1);

            // Rebuild (t1)
            ((IProjectBuildEventListener)_buildUpToDateCheck).NotifyBuildStarting(buildStartTimeUtc: t1);
            await ((IProjectBuildEventListener)_buildUpToDateCheck).NotifyBuildCompletedAsync(wasSuccessful: true, isRebuild: true);

            // Run test (t2)
            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(t1)}). Newest input is '{_inputPath}' ({ToLocalTime(t0)}).
                Project is up-to-date.
                """);
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CompileItemNewerThanCustomOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckOutput.SchemaName] = SimpleItems("Output")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("Input.cs")
            };

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var outputTime      = DateTime.UtcNow.AddMinutes(-3);
            var compileItemTime = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output", outputTime);
            _fileSystem.AddFile(_inputPath, compileItemTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckOutput outputs:
                        C:\Dev\Solution\Project\Output
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding Compile inputs:
                        {_inputPath}
                Input Compile item '{_inputPath}' is newer ({ToLocalTime(compileItemTime)}) than earliest output 'C:\Dev\Solution\Project\Output' ({ToLocalTime(outputTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Theory]
        [CombinatorialData]
        public async Task IsUpToDateAsync_CopyReference_InputsOlderThanMarkerOutput(bool? isBuildAccelerationEnabled)
        {
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [CopyUpToDateMarker.SchemaName] = SimpleItems("OutputMarker"),
                [ResolvedCompilationReference.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Items = new Dictionary<string, IImmutableDictionary<string, string>>(StringComparers.ItemNames)
                    {
                        {
                            "Reference1",
                            ImmutableStringDictionary<string>.EmptyOrdinal
                                .Add("CopyUpToDateMarker", "Reference1MarkerPath")
                                .Add("ResolvedPath", "Reference1ResolvedPath")
                                .Add("OriginalPath", "Reference1OriginalPath")
                        }
                    }
                }
            };

            var lastBuildTime = DateTime.UtcNow.AddMinutes(-5);

            await SetupAsync(projectSnapshot: projectSnapshot, lastSuccessfulBuildStartTimeUtc: lastBuildTime);

            var resolvedTime = DateTime.UtcNow.AddMinutes(-4);
            var markerTime   = DateTime.UtcNow.AddMinutes(-3);
            var originalTime = DateTime.UtcNow.AddMinutes(-2);
            var outputTime   = DateTime.UtcNow.AddMinutes(-1);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\OutputMarker", outputTime);
            _fileSystem.AddFile("Reference1ResolvedPath", resolvedTime);
            _fileSystem.AddFile("Reference1MarkerPath", markerTime);
            _fileSystem.AddFile("Reference1OriginalPath", originalTime);

            if (isBuildAccelerationEnabled is true)
            {
                await AssertUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Project is up-to-date.
                    """);
            }
            else if (isBuildAccelerationEnabled is false)
            {
                await AssertUpToDateAsync(
                    $"""
                    Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property. See https://aka.ms/vs-build-acceleration.
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Comparing timestamps of copy marker inputs and outputs:
                        Write timestamp on output marker is {ToLocalTime(outputTime)} on 'C:\Dev\Solution\Project\OutputMarker'.
                        Adding input reference copy markers:
                            Reference1OriginalPath
                            Reference1MarkerPath
                    Project is up-to-date.
                    """);
            }
            else if (isBuildAccelerationEnabled is null)
            {
                await AssertUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Comparing timestamps of copy marker inputs and outputs:
                        Write timestamp on output marker is {ToLocalTime(outputTime)} on 'C:\Dev\Solution\Project\OutputMarker'.
                        Adding input reference copy markers:
                            Reference1OriginalPath
                            Reference1MarkerPath
                    Project is up-to-date.
                    """);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        public async Task IsUpToDateAsync_False_CopyReference_InputNewerThanMarkerOutput(bool? isBuildAccelerationEnabled)
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [CopyUpToDateMarker.SchemaName] = SimpleItems("OutputMarker"),
                [ResolvedCompilationReference.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Items = new Dictionary<string, IImmutableDictionary<string, string>>(StringComparers.ItemNames)
                    {
                        {
                            "Reference1",
                            ImmutableStringDictionary<string>.EmptyOrdinal
                                .Add("CopyUpToDateMarker", "Reference1MarkerPath")
                                .Add("ResolvedPath", "Reference1ResolvedPath")
                                .Add("OriginalPath", "Reference1OriginalPath")
                        }
                    }
                }
            };

            var lastBuildTime = DateTime.UtcNow.AddMinutes(-5);

            // We only check copy markers when build acceleration is disabled (null or false).
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            await SetupAsync(projectSnapshot: projectSnapshot, lastSuccessfulBuildStartTimeUtc: lastBuildTime);

            var outputTime   = DateTime.UtcNow.AddMinutes(-4);
            var resolvedTime = DateTime.UtcNow.AddMinutes(-3);
            var markerTime   = DateTime.UtcNow.AddMinutes(-2);
            var originalTime = DateTime.UtcNow.AddMinutes(-1);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\OutputMarker", outputTime);
            _fileSystem.AddFile("Reference1ResolvedPath", resolvedTime);
            _fileSystem.AddFile("Reference1MarkerPath", markerTime);
            _fileSystem.AddFile("Reference1OriginalPath", originalTime);

            if (isBuildAccelerationEnabled is false)
            {
                await AssertNotUpToDateAsync(
                   $"""
                    Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property. See https://aka.ms/vs-build-acceleration.
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Comparing timestamps of copy marker inputs and outputs:
                        Write timestamp on output marker is {ToLocalTime(outputTime)} on 'C:\Dev\Solution\Project\OutputMarker'.
                        Adding input reference copy markers:
                            Reference1OriginalPath
                    Input marker 'Reference1OriginalPath' is newer ({ToLocalTime(originalTime)}) than output marker 'C:\Dev\Solution\Project\OutputMarker' ({ToLocalTime(outputTime)}), not up-to-date.
                    """,
                    "InputMarkerNewerThanOutputMarker");
            }
            else if (isBuildAccelerationEnabled is null)
            {
                await AssertNotUpToDateAsync(
                   $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Comparing timestamps of copy marker inputs and outputs:
                        Write timestamp on output marker is {ToLocalTime(outputTime)} on 'C:\Dev\Solution\Project\OutputMarker'.
                        Adding input reference copy markers:
                            Reference1OriginalPath
                    Input marker 'Reference1OriginalPath' is newer ({ToLocalTime(originalTime)}) than output marker 'C:\Dev\Solution\Project\OutputMarker' ({ToLocalTime(outputTime)}), not up-to-date.
                    This project appears to be a candidate for build acceleration. To opt in, set the 'AccelerateBuildsInVisualStudio' MSBuild property to 'true'. See https://aka.ms/vs-build-acceleration.
                    """,
                    "InputMarkerNewerThanOutputMarker");
            }
        }

        [Fact]
        public async Task IsUpToDateAsync_False_AnalyzerReference_NewerThanEarliestOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [ResolvedAnalyzerReference.SchemaName] = ItemWithMetadata("Analyzer1", "ResolvedPath", "Analyzer1ResolvedPath")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var outputTime     = DateTime.UtcNow.AddMinutes(-3);
            var inputTime      = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            var analyzerItem = @"C:\Dev\Solution\Project\Analyzer1ResolvedPath";

            _fileSystem.AddFile(_builtPath, outputTime);
            _fileSystem.AddFile(analyzerItem, inputTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding ResolvedAnalyzerReference inputs:
                        {analyzerItem}
                Input ResolvedAnalyzerReference item '{analyzerItem}' is newer ({ToLocalTime(inputTime)}) than earliest output '{_builtPath}' ({ToLocalTime(outputTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CompilationReference_NewerThanEarliestOutput()
        {
            var resolvedReferencePath = @"C:\Dev\Solution\Project\Reference1ResolvedPath";

            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [ResolvedCompilationReference.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Items = new Dictionary<string, IImmutableDictionary<string, string>>(StringComparers.ItemNames)
                    {
                        {
                            "Reference1",
                            ImmutableStringDictionary<string>.EmptyOrdinal
                                .Add("CopyUpToDateMarker", "Reference1MarkerPath")
                                .Add("ResolvedPath", resolvedReferencePath)
                                .Add("OriginalPath", @"..\Project\Reference1OriginalPath")
                        }
                    }
                }
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(_builtPath, outputTime);
            _fileSystem.AddFile(resolvedReferencePath, inputTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding ResolvedCompilationReference inputs:
                        {resolvedReferencePath}
                Input ResolvedCompilationReference item 'C:\Dev\Solution\Project\Reference1ResolvedPath' is newer ({ToLocalTime(inputTime)}) than earliest output '{_builtPath}' ({ToLocalTime(outputTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_UpToDateCheckInput_NewerThanEarliestOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckInput.SchemaName] = SimpleItems("Item1", "Item2")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(_builtPath, outputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Item1", inputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Item2", outputTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding UpToDateCheckInput inputs:
                        C:\Dev\Solution\Project\Item1
                Input UpToDateCheckInput item 'C:\Dev\Solution\Project\Item1' is newer ({ToLocalTime(inputTime)}) than earliest output '{_builtPath}' ({ToLocalTime(outputTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Sets_InputNewerThanOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckInput.SchemaName] = ItemWithMetadata("Input1", "Set", "Set1"),
                [UpToDateCheckOutput.SchemaName] = ItemWithMetadata("Output1", "Set", "Set1")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input1", inputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output1", outputTime);
            _fileSystem.AddFile(_builtPath, outputTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(outputTime)}). Newest input is '{_projectPath}' ({ToLocalTime(_projectFileTimeUtc)}).
                Comparing timestamps of inputs and outputs in Set="Set1":
                    Adding UpToDateCheckOutput outputs in Set="Set1":
                        C:\Dev\Solution\Project\Output1
                    Adding UpToDateCheckInput inputs in Set="Set1":
                        C:\Dev\Solution\Project\Input1
                Input UpToDateCheckInput item 'C:\Dev\Solution\Project\Input1' is newer ({ToLocalTime(inputTime)}) than earliest output 'C:\Dev\Solution\Project\Output1' ({ToLocalTime(outputTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Sets_InputOlderThanOutput_MultipleSets()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckInput.SchemaName] = Union(ItemWithMetadata("Input1", "Set", "Set1"), ItemWithMetadata("Input2", "Set", "Set2")),
                [UpToDateCheckOutput.SchemaName] = Union(ItemWithMetadata("Output1", "Set", "Set1"), ItemWithMetadata("Output2", "Set", "Set2"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-6);
            var inputTime1     = DateTime.UtcNow.AddMinutes(-5);
            var outputTime1    = DateTime.UtcNow.AddMinutes(-4);
            var inputTime2     = DateTime.UtcNow.AddMinutes(-3);
            var outputTime2    = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input1", inputTime1);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output1", outputTime1);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input2", inputTime2);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output2", outputTime2);
            _fileSystem.AddFile(_builtPath, outputTime1);

            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(outputTime1)}). Newest input is '{_projectPath}' ({ToLocalTime(_projectFileTimeUtc)}).
                Comparing timestamps of inputs and outputs in Set="Set1":
                    Adding UpToDateCheckOutput outputs in Set="Set1":
                        C:\Dev\Solution\Project\Output1
                    Adding UpToDateCheckInput inputs in Set="Set1":
                        C:\Dev\Solution\Project\Input1
                    In Set="Set1", no inputs are newer than earliest output 'C:\Dev\Solution\Project\Output1' ({ToLocalTime(outputTime1)}). Newest input is 'C:\Dev\Solution\Project\Input1' ({ToLocalTime(inputTime1)}).
                Comparing timestamps of inputs and outputs in Set="Set2":
                    Adding UpToDateCheckOutput outputs in Set="Set2":
                        C:\Dev\Solution\Project\Output2
                    Adding UpToDateCheckInput inputs in Set="Set2":
                        C:\Dev\Solution\Project\Input2
                    In Set="Set2", no inputs are newer than earliest output 'C:\Dev\Solution\Project\Output2' ({ToLocalTime(outputTime2)}). Newest input is 'C:\Dev\Solution\Project\Input2' ({ToLocalTime(inputTime2)}).
                Project is up-to-date.
                """);
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Sets_InputNewerThanOutput_MultipleSets()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckInput.SchemaName] = Union(ItemWithMetadata("Input1", "Set", "Set1"), ItemWithMetadata("Input2", "Set", "Set2")),
                [UpToDateCheckOutput.SchemaName] = Union(ItemWithMetadata("Output1", "Set", "Set1"), ItemWithMetadata("Output2", "Set", "Set2"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-6);
            var inputTime1     = DateTime.UtcNow.AddMinutes(-5);
            var outputTime1    = DateTime.UtcNow.AddMinutes(-4);
            var outputTime2    = DateTime.UtcNow.AddMinutes(-3);
            var inputTime2     = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input1", inputTime1);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output1", outputTime1);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input2", inputTime2);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output2", outputTime2);
            _fileSystem.AddFile(_builtPath, outputTime1);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(outputTime1)}). Newest input is '{_projectPath}' ({ToLocalTime(_projectFileTimeUtc)}).
                Comparing timestamps of inputs and outputs in Set="Set1":
                    Adding UpToDateCheckOutput outputs in Set="Set1":
                        C:\Dev\Solution\Project\Output1
                    Adding UpToDateCheckInput inputs in Set="Set1":
                        C:\Dev\Solution\Project\Input1
                    In Set="Set1", no inputs are newer than earliest output 'C:\Dev\Solution\Project\Output1' ({ToLocalTime(outputTime1)}). Newest input is 'C:\Dev\Solution\Project\Input1' ({ToLocalTime(inputTime1)}).
                Comparing timestamps of inputs and outputs in Set="Set2":
                    Adding UpToDateCheckOutput outputs in Set="Set2":
                        C:\Dev\Solution\Project\Output2
                    Adding UpToDateCheckInput inputs in Set="Set2":
                        C:\Dev\Solution\Project\Input2
                Input UpToDateCheckInput item 'C:\Dev\Solution\Project\Input2' is newer ({ToLocalTime(inputTime2)}) than earliest output 'C:\Dev\Solution\Project\Output2' ({ToLocalTime(outputTime2)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Sets_InputNewerThanOutput_ItemInMultipleSets()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckInput.SchemaName] = ItemWithMetadata("Input.cs", "Set", "Set1;Set2"),
                [UpToDateCheckOutput.SchemaName] = Union(ItemWithMetadata("Output1", "Set", "Set1"), ItemWithMetadata("Output2", "Set", "Set2"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-5);
            var outputTime2    = DateTime.UtcNow.AddMinutes(-4);
            var inputTime      = DateTime.UtcNow.AddMinutes(-3);
            var outputTime1    = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-1);
            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(_inputPath, inputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output1", outputTime1);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output2", outputTime2);
            _fileSystem.AddFile(_builtPath, outputTime1);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(outputTime1)}). Newest input is '{_projectPath}' ({ToLocalTime(_projectFileTimeUtc)}).
                Comparing timestamps of inputs and outputs in Set="Set1":
                    Adding UpToDateCheckOutput outputs in Set="Set1":
                        C:\Dev\Solution\Project\Output1
                    Adding UpToDateCheckInput inputs in Set="Set1":
                        C:\Dev\Solution\Project\Input.cs
                    In Set="Set1", no inputs are newer than earliest output 'C:\Dev\Solution\Project\Output1' ({ToLocalTime(outputTime1)}). Newest input is '{_inputPath}' ({ToLocalTime(inputTime)}).
                Comparing timestamps of inputs and outputs in Set="Set2":
                    Adding UpToDateCheckOutput outputs in Set="Set2":
                        C:\Dev\Solution\Project\Output2
                    Adding UpToDateCheckInput inputs in Set="Set2":
                        C:\Dev\Solution\Project\Input.cs
                Input UpToDateCheckInput item '{_inputPath}' is newer ({ToLocalTime(inputTime)}) than earliest output 'C:\Dev\Solution\Project\Output2' ({ToLocalTime(outputTime2)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Sets_InputOnly()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckInput.SchemaName] = ItemWithMetadata("Input1", "Set", "Set1"),
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var buildTime      = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input1", inputTime);
            _fileSystem.AddFile(_builtPath, buildTime);

            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(buildTime)}). Newest input is '{_projectPath}' ({ToLocalTime(_projectFileTimeUtc)}).
                Comparing timestamps of inputs and outputs in Set="Set1":
                    No build outputs defined in Set="Set1".
                Project is up-to-date.
                """);
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Sets_OutputOnly()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll"),
                [UpToDateCheckOutput.SchemaName] = ItemWithMetadata("Output1", "Set", "Set1")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-3);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-2);
            var outputTime     = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output1", outputTime);
            _fileSystem.AddFile(_builtPath, outputTime);

            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckBuilt outputs:
                        {_builtPath}
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    No inputs are newer than earliest output '{_builtPath}' ({ToLocalTime(outputTime)}). Newest input is '{_projectPath}' ({ToLocalTime(_projectFileTimeUtc)}).
                Comparing timestamps of inputs and outputs in Set="Set1":
                    Adding UpToDateCheckOutput outputs in Set="Set1":
                        C:\Dev\Solution\Project\Output1
                    No inputs defined in Set="Set1".
                Project is up-to-date.
                """);
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Kinds_InputNewerThanOutput_WithIgnoredKind()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemsWithMetadata(("Built", "Kind", ""), ("IgnoredBuilt.dll", "Kind", "Ignored")),
                [UpToDateCheckInput.SchemaName] = ItemsWithMetadata(("Input", "Kind", ""), ("IgnoredInput.cs", "Kind", "Ignored")),
                [UpToDateCheckOutput.SchemaName] = ItemsWithMetadata(("Output", "Kind", ""), ("IgnoredOutput", "Kind", "Ignored"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input", inputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output", outputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Built", outputTime);

            await AssertNotUpToDateAsync(
                $"""
                Ignoring up-to-date check items with Kind="Ignored"
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckOutput outputs:
                        C:\Dev\Solution\Project\Output
                        Skipping 'C:\Dev\Solution\Project\IgnoredOutput' with ignored Kind="Ignored"
                    Adding UpToDateCheckBuilt outputs:
                        C:\Dev\Solution\Project\Built
                        Skipping 'C:\Dev\Solution\Project\IgnoredBuilt.dll' with ignored Kind="Ignored"
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding UpToDateCheckInput inputs:
                        C:\Dev\Solution\Project\Input
                Input UpToDateCheckInput item 'C:\Dev\Solution\Project\Input' is newer ({ToLocalTime(inputTime)}) than earliest output 'C:\Dev\Solution\Project\Output' ({ToLocalTime(outputTime)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput",
                ignoreKinds: "Ignored");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Kinds_InputNewerThanOutput_NoKindIgnored()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemsWithMetadata(("Built", "Kind", ""), ("TaggedBuilt", "Kind", "Tagged")),
                [UpToDateCheckInput.SchemaName] = ItemsWithMetadata(("Input", "Kind", ""), ("TaggedInput", "Kind", "Tagged")),
                [UpToDateCheckOutput.SchemaName] = ItemsWithMetadata(("Output", "Kind", ""), ("TaggedOutput", "Kind", "Tagged"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-6);
            var input2Time     = DateTime.UtcNow.AddMinutes(-5);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-4);
            var output1Time    = DateTime.UtcNow.AddMinutes(-3);
            var output2Time    = DateTime.UtcNow.AddMinutes(-2);
            var input1Time     = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input",        input1Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\TaggedInput",  input2Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output",       output1Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\TaggedOutput", output2Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Built",        output1Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\TaggedBuilt",  output2Time);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckOutput outputs:
                        C:\Dev\Solution\Project\Output
                        C:\Dev\Solution\Project\TaggedOutput
                    Adding UpToDateCheckBuilt outputs:
                        C:\Dev\Solution\Project\Built
                        C:\Dev\Solution\Project\TaggedBuilt
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding UpToDateCheckInput inputs:
                        C:\Dev\Solution\Project\Input
                Input UpToDateCheckInput item 'C:\Dev\Solution\Project\Input' is newer ({ToLocalTime(input1Time)}) than earliest output 'C:\Dev\Solution\Project\Output' ({ToLocalTime(output1Time)}), not up-to-date.
                """,
                "InputNewerThanEarliestOutput",
                ignoreKinds: "");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Kinds_InputNewerThanOutput_WithIgnoredKind()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemsWithMetadata(("Built", "Kind", ""), ("IgnoredBuilt", "Kind", "Ignored")),
                [UpToDateCheckInput.SchemaName] = ItemsWithMetadata(("Input", "Kind", ""), ("IgnoredInput", "Kind", "Ignored")),
                [UpToDateCheckOutput.SchemaName] = ItemsWithMetadata(("Output", "Kind", ""), ("IgnoredOutput", "Kind", "Ignored"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var inputTime      = DateTime.UtcNow.AddMinutes(-3);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-2);
            var outputTime     = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input", inputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output", outputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Built", outputTime);

            await AssertUpToDateAsync(
                $"""
                Ignoring up-to-date check items with Kind="Ignored"
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckOutput outputs:
                        C:\Dev\Solution\Project\Output
                        Skipping 'C:\Dev\Solution\Project\IgnoredOutput' with ignored Kind="Ignored"
                    Adding UpToDateCheckBuilt outputs:
                        C:\Dev\Solution\Project\Built
                        Skipping 'C:\Dev\Solution\Project\IgnoredBuilt' with ignored Kind="Ignored"
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding UpToDateCheckInput inputs:
                        C:\Dev\Solution\Project\Input
                        Skipping 'C:\Dev\Solution\Project\IgnoredInput' with ignored Kind="Ignored"
                    No inputs are newer than earliest output 'C:\Dev\Solution\Project\Output' ({ToLocalTime(outputTime)}). Newest input is 'C:\Dev\Solution\Project\Input' ({ToLocalTime(inputTime)}).
                Project is up-to-date.
                """,
                ignoreKinds: "Ignored");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Kinds_InputNewerThanOutput_NoKindIgnored()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemsWithMetadata(("Built", "Kind", ""), ("TaggedBuilt", "Kind", "Tagged")),
                [UpToDateCheckInput.SchemaName] = ItemsWithMetadata(("Input", "Kind", ""), ("TaggedInput", "Kind", "Tagged")),
                [UpToDateCheckOutput.SchemaName] = ItemsWithMetadata(("Output", "Kind", ""), ("TaggedOutput", "Kind", "Tagged"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-8);
            var input2Time     = DateTime.UtcNow.AddMinutes(-7);
            var input1Time     = DateTime.UtcNow.AddMinutes(-6);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-5);
            var output4Time    = DateTime.UtcNow.AddMinutes(-4);
            var output3Time    = DateTime.UtcNow.AddMinutes(-3);
            var output2Time    = DateTime.UtcNow.AddMinutes(-2);
            var output1Time    = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Input",        input2Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\TaggedInput",  input1Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Output",       output4Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\TaggedOutput", output3Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\Built",        output2Time);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\TaggedBuilt",  output1Time);

            await AssertUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    Adding UpToDateCheckOutput outputs:
                        C:\Dev\Solution\Project\Output
                        C:\Dev\Solution\Project\TaggedOutput
                    Adding UpToDateCheckBuilt outputs:
                        C:\Dev\Solution\Project\Built
                        C:\Dev\Solution\Project\TaggedBuilt
                    Adding project file inputs:
                        {_projectPath}
                    Adding newest import input:
                        {_projectPath}
                    Adding UpToDateCheckInput inputs:
                        C:\Dev\Solution\Project\Input
                        C:\Dev\Solution\Project\TaggedInput
                    No inputs are newer than earliest output 'C:\Dev\Solution\Project\Output' ({ToLocalTime(output4Time)}). Newest input is 'C:\Dev\Solution\Project\TaggedInput' ({ToLocalTime(input1Time)}).
                Project is up-to-date.
                """,
                ignoreKinds: "");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_BuiltItemWithSource_SourceIsNewerThanDestination()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemWithMetadata("CopiedOutputDestination", UpToDateCheckBuilt.OriginalProperty, "CopiedOutputSource")
            };

            var destinationPath = @"C:\Dev\Solution\Project\CopiedOutputDestination";
            var sourcePath = @"C:\Dev\Solution\Project\CopiedOutputSource";

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    No build outputs defined.
                Checking built output ({UpToDateCheckBuilt.SchemaName} with {UpToDateCheckBuilt.OriginalProperty} property) file:
                    Source      {ToLocalTime(sourceTime)}: '{sourcePath}'
                    Destination {ToLocalTime(destinationTime)}: '{destinationPath}'
                Source is newer than build output destination, not up-to-date.
                """,
                "CopySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_BuiltItemWithSource_SourceDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemWithMetadata("CopiedOutputDestination", UpToDateCheckBuilt.OriginalProperty, "CopiedOutputSource")
            };

            var lastBuildTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(projectSnapshot, lastSuccessfulBuildStartTimeUtc: lastBuildTime);

            var destinationPath = @"C:\Dev\Solution\Project\CopiedOutputDestination";
            var sourcePath = @"C:\Dev\Solution\Project\CopiedOutputSource";

            _fileSystem.AddFile(destinationPath);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    No build outputs defined.
                Checking built output ({UpToDateCheckBuilt.SchemaName} with {UpToDateCheckBuilt.OriginalProperty} property) file:
                Source '{sourcePath}' does not exist for build into target '{destinationPath}', not up-to-date.
                """,
                "CopySourceNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_BuiltItemWithSource_DestinationDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemWithMetadata("CopiedOutputDestination", UpToDateCheckBuilt.OriginalProperty, "CopiedOutputSource")
            };

            var destinationPath = @"C:\Dev\Solution\Project\CopiedOutputDestination";
            var sourcePath = @"C:\Dev\Solution\Project\CopiedOutputSource";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var sourceTime     = DateTime.UtcNow.AddMinutes(-2);

            await SetupAsync(
                projectSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    No build outputs defined.
                Checking built output ({UpToDateCheckBuilt.SchemaName} with {UpToDateCheckBuilt.OriginalProperty} property) file:
                    Source      {ToLocalTime(sourceTime)}: '{sourcePath}'
                Destination '{destinationPath}' does not exist for copy from '{sourcePath}', not up-to-date.
                """,
                "CopyDestinationNotFound");
        }

        [Theory]
        [CombinatorialData]
        public async Task IsUpToDateAsync_CopyToOutputDirectory_SourceIsNewerThanDestination(bool? isBuildAccelerationEnabled)
        {
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            var sourcePath1 = @"C:\Dev\Solution\Project\Item1";
            var sourcePath2 = @"C:\Dev\Solution\Project\Item2";
            var destinationPath1 = @"C:\Dev\Solution\Project\bin\Debug\Item1";
            var destinationPath2 = @"C:\Dev\Solution\Project\bin\Debug\Item2";

            SetCopyItems(
                new CopyItem(sourcePath1, "Item1", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: false),
                new CopyItem(sourcePath2, "Item2", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: true));

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(sourcePath1, sourceTime);
            _fileSystem.AddFile(sourcePath2, sourceTime);
            _fileSystem.AddFile(destinationPath1, destinationTime);
            _fileSystem.AddFile(destinationPath2, destinationTime);

            if (isBuildAccelerationEnabled is true)
            {
                await AssertUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                                Remembering the need to copy file '{sourcePath1}' to '{destinationPath1}'.
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath2}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath2}'
                                Remembering the need to copy file '{sourcePath2}' to '{destinationPath2}'.
                    Copying 2 files to accelerate build (https://aka.ms/vs-build-acceleration):
                        From '{sourcePath1}' to '{destinationPath1}'.
                        From '{sourcePath2}' to '{destinationPath2}'.
                    Build acceleration copied 2 files.
                    Project is up-to-date.
                    """);
            }
            else if (isBuildAccelerationEnabled is false)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property. See https://aka.ms/vs-build-acceleration.
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                    Item with CopyToOutputDirectory="PreserveNewest" source '{sourcePath1}' is newer than destination '{destinationPath1}', not up-to-date.
                    """,
                    "CopyToOutputDirectorySourceNewer");
            }
            else if (isBuildAccelerationEnabled is null)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                    Item with CopyToOutputDirectory="PreserveNewest" source '{sourcePath1}' is newer than destination '{destinationPath1}', not up-to-date.
                    This project appears to be a candidate for build acceleration. To opt in, set the 'AccelerateBuildsInVisualStudio' MSBuild property to 'true'. See https://aka.ms/vs-build-acceleration.
                    """,
                    "CopyToOutputDirectorySourceNewer");
            }
        }

        [Theory]
        [CombinatorialData]
        public async Task IsUpToDateAsync_CopyToOutputDirectory_SourceIsNewerThanDestination_TargetPath(bool? isBuildAccelerationEnabled)
        {
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            var sourcePath1 = @"C:\Dev\Solution\Project\Item1";
            var sourcePath2 = @"C:\Dev\Solution\Project\Item2";
            var destinationPath1 = @"C:\Dev\Solution\Project\bin\Debug\Item1";
            var destinationPath2 = @"C:\Dev\Solution\Project\bin\Debug\Item2";

            SetCopyItems(
                new CopyItem(sourcePath1, "Item1", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: false),
                new CopyItem(sourcePath2, "Item2", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: true));

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(sourcePath1, sourceTime);
            _fileSystem.AddFile(sourcePath2, sourceTime);
            _fileSystem.AddFile(destinationPath1, destinationTime);
            _fileSystem.AddFile(destinationPath2, destinationTime);

            if (isBuildAccelerationEnabled is true)
            {
                await AssertUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                                Remembering the need to copy file '{sourcePath1}' to '{destinationPath1}'.
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath2}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath2}'
                                Remembering the need to copy file '{sourcePath2}' to '{destinationPath2}'.
                    Copying 2 files to accelerate build (https://aka.ms/vs-build-acceleration):
                        From '{sourcePath1}' to '{destinationPath1}'.
                        From '{sourcePath2}' to '{destinationPath2}'.
                    Build acceleration copied 2 files.
                    Project is up-to-date.
                    """);
            }
            else if (isBuildAccelerationEnabled is false)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property. See https://aka.ms/vs-build-acceleration.
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                    Item with CopyToOutputDirectory="PreserveNewest" source '{sourcePath1}' is newer than destination '{destinationPath1}', not up-to-date.
                    """,
                    "CopyToOutputDirectorySourceNewer");
            }
            else if (isBuildAccelerationEnabled is null)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                    Item with CopyToOutputDirectory="PreserveNewest" source '{sourcePath1}' is newer than destination '{destinationPath1}', not up-to-date.
                    This project appears to be a candidate for build acceleration. To opt in, set the 'AccelerateBuildsInVisualStudio' MSBuild property to 'true'. See https://aka.ms/vs-build-acceleration.
                    """,
                    "CopyToOutputDirectorySourceNewer");
            }
        }

        [Theory]
        [CombinatorialData]
        public async Task IsUpToDateAsync_CopyToOutputDirectory_SourceIsNewerThanDestination_CustomOutDir(bool? isBuildAccelerationEnabled)
        {
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            const string outDirSnapshot = "newOutDir";

            var sourcePath1 = @"C:\Dev\Solution\Project\Item1";
            var sourcePath2 = @"C:\Dev\Solution\Project\Item2";
            var destinationPath1 = $@"C:\Dev\Solution\Project\{outDirSnapshot}\Item1";
            var destinationPath2 = $@"C:\Dev\Solution\Project\{outDirSnapshot}\Item2";

            SetCopyItems(
                new CopyItem(sourcePath1, "Item1", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: false),
                new CopyItem(sourcePath2, "Item2", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: true));

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                outDir: outDirSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath1, destinationTime);
            _fileSystem.AddFile(destinationPath2, destinationTime);
            _fileSystem.AddFile(sourcePath1, sourceTime);
            _fileSystem.AddFile(sourcePath2, sourceTime);

            if (isBuildAccelerationEnabled is true)
            {
                await AssertUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                                Remembering the need to copy file '{sourcePath1}' to '{destinationPath1}'.
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath2}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath2}'
                                Remembering the need to copy file '{sourcePath2}' to '{destinationPath2}'.
                    Copying 2 files to accelerate build (https://aka.ms/vs-build-acceleration):
                        From '{sourcePath1}' to '{destinationPath1}'.
                        From '{sourcePath2}' to '{destinationPath2}'.
                    Build acceleration copied 2 files.
                    Project is up-to-date.
                    """);
            }
            else if (isBuildAccelerationEnabled is false)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property. See https://aka.ms/vs-build-acceleration.
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                    Item with CopyToOutputDirectory="PreserveNewest" source '{sourcePath1}' is newer than destination '{destinationPath1}', not up-to-date.
                    """,
                    "CopyToOutputDirectorySourceNewer");
            }
            else if (isBuildAccelerationEnabled is null)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination {ToLocalTime(destinationTime)}: '{destinationPath1}'
                    Item with CopyToOutputDirectory="PreserveNewest" source '{sourcePath1}' is newer than destination '{destinationPath1}', not up-to-date.
                    This project appears to be a candidate for build acceleration. To opt in, set the 'AccelerateBuildsInVisualStudio' MSBuild property to 'true'. See https://aka.ms/vs-build-acceleration.
                    """,
                    "CopyToOutputDirectorySourceNewer");
            }
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_SourceDoesNotExist()
        {
            var sourcePath1 = @"C:\Dev\Solution\Project\Item1";
            var sourcePath2 = @"C:\Dev\Solution\Project\Item2";
            var destinationPath1 = @"C:\Dev\Solution\Project\bin\Debug\Item1";

            SetCopyItems(
                new CopyItem(sourcePath1, "Item1", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: false),
                new CopyItem(sourcePath2, "Item2", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: true));

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime   = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);

            await SetupAsync(
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath1, destinationTime);

            await AssertNotUpToDateAsync(
                $"""
                Comparing timestamps of inputs and outputs:
                    No build outputs defined.
                Checking items to copy to the output directory:
                    Checking copy items from project '{_projectPath}':
                        Checking PreserveNewest item
                Source '{sourcePath1}' does not exist, not up-to-date.
                """,
                "CopyToOutputDirectorySourceNotFound");
        }

        [Theory]
        [CombinatorialData]
        public async Task IsUpToDateAsync_CopyToOutputDirectory_DestinationDoesNotExist(bool? isBuildAccelerationEnabled)
        {
            _isBuildAccelerationEnabled = isBuildAccelerationEnabled;

            var sourcePath1 = @"C:\Dev\Solution\Project\Item1";
            var sourcePath2 = @"C:\Dev\Solution\Project\Item2";
            var destinationPath1 = @"C:\Dev\Solution\Project\bin\Debug\Item1";
            var destinationPath2 = @"C:\Dev\Solution\Project\bin\Debug\Item2";

            SetCopyItems(
                new CopyItem(sourcePath1, "Item1", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: false),
                new CopyItem(sourcePath2, "Item2", BuildUpToDateCheck.CopyType.PreserveNewest, isBuildAccelerationOnly: true));

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastBuildTime  = DateTime.UtcNow.AddMinutes(-3);
            var sourceTime     = DateTime.UtcNow.AddMinutes(-2);

            await SetupAsync(
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(sourcePath1, sourceTime);
            _fileSystem.AddFile(sourcePath2, sourceTime);

            if (isBuildAccelerationEnabled is true)
            {
                await AssertUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination '{destinationPath1}' does not exist.
                                Remembering the need to copy file '{sourcePath1}' to '{destinationPath1}'.
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath2}'
                                Destination '{destinationPath2}' does not exist.
                                Remembering the need to copy file '{sourcePath2}' to '{destinationPath2}'.
                    Copying 2 files to accelerate build (https://aka.ms/vs-build-acceleration):
                        From '{sourcePath1}' to '{destinationPath1}'.
                        From '{sourcePath2}' to '{destinationPath2}'.
                    Build acceleration copied 2 files.
                    Project is up-to-date.
                    """);
            }
            else if (isBuildAccelerationEnabled is false)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property. See https://aka.ms/vs-build-acceleration.
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination '{destinationPath1}' does not exist.
                    Destination '{destinationPath1}' does not exist, not up-to-date.
                    """,
                    "CopyToOutputDirectoryDestinationNotFound");
            }
            else if (isBuildAccelerationEnabled is null)
            {
                await AssertNotUpToDateAsync(
                    $"""
                    Comparing timestamps of inputs and outputs:
                        No build outputs defined.
                    Checking items to copy to the output directory:
                        Checking copy items from project '{_projectPath}':
                            Checking PreserveNewest item
                                Source      {ToLocalTime(sourceTime)}: '{sourcePath1}'
                                Destination '{destinationPath1}' does not exist.
                    Destination '{destinationPath1}' does not exist, not up-to-date.
                    This project appears to be a candidate for build acceleration. To opt in, set the 'AccelerateBuildsInVisualStudio' MSBuild property to 'true'. See https://aka.ms/vs-build-acceleration.
                    """,
                    "CopyToOutputDirectoryDestinationNotFound");
            }
        }

        [Fact]
        public void ComputeItemHash()
        {
            Assert.Equal(
                HashItems(("Compile", new[] { "Path1" })),
                HashItems(("Compile", new[] { "Path1" })));

            // Order independent
            Assert.Equal(
                HashItems(("Compile", new[] { "Path1", "Path2" })),
                HashItems(("Compile", new[] { "Path2", "Path1" })));

            // Item type dependent
            Assert.NotEqual(
                HashItems(("Compile", new[] { "Path1" })),
                HashItems(("None",    new[] { "Path1" })));

            // Adding an item causes a difference
            Assert.NotEqual(
                HashItems(("Compile", new[] { "Path1" })),
                HashItems(("Compile", new[] { "Path1", "Path2" })));

            static int HashItems(params (string ItemType, string[] Paths)[] items)
            {
                var itemsByItemType = items.ToImmutableDictionary(
                    i => i.ItemType,
                    i => i.Paths.ToImmutableArray());

                return BuildUpToDateCheck.ComputeItemHash(itemsByItemType);
            }
        }

        [Theory]
        [InlineData("", 371857150)]
        [InlineData("Hello, World!", 562640209)]
        [InlineData("Test", -871204762)]
        [InlineData("TEst", -2094096442)]
        [InlineData("Some text", -1076058823)]
        [InlineData("AAAAAAAAAAAAAAAAAAAAA", 1397387549)]
        [InlineData("1(*!)#2*$&@_fa!f(#(b$(12", -2128933005)]
        public void GetStableHashCode_Matches_NetFrameworkX64(string str, int netFrameworkX64HashPrecomputed)
        {
            int hash = BuildUpToDateCheck.GetStableHashCode(str);

            Assert.Equal(hash, netFrameworkX64HashPrecomputed);
        }

        [Fact]
        public void GetStableHashCode_SameText_SameHash()
        {
            string str1 = "SomeText";
            string str2 = "SomeText";

            int h1 = BuildUpToDateCheck.GetStableHashCode(str1);
            int h2 = BuildUpToDateCheck.GetStableHashCode(str2);

            Assert.Equal(h1, h2);
        }

        [Fact]
        public void GetStableHashCode_DifferentText_DifferentHash()
        {
            string str1 = "SomeText";
            string str2 = "SomeTexT";

            int h1 = BuildUpToDateCheck.GetStableHashCode(str1);
            int h2 = BuildUpToDateCheck.GetStableHashCode(str2);

            Assert.NotEqual(h1, h2);
        }

        [Fact]
        public async Task IsUpToDateAsync_True_InputNewerThatBuiltOutput_TargetFrameworkDoesNotMatchBuild()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems(@"bin\Debug\Built.dll")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var itemChangedTime = DateTime.UtcNow.AddMinutes(-4);
            var outputTime = DateTime.UtcNow.AddMinutes(-3);
            var inputTime = DateTime.UtcNow.AddMinutes(-2);
            var lastBuildTime = DateTime.UtcNow.AddMinutes(-1);

            var configuredInput = UpToDateCheckImplicitConfiguredInput.CreateEmpty(
                ProjectConfigurationFactory.Create("TargetFramework", "alphaFramework"));

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastSuccessfulBuildStartTimeUtc: lastBuildTime,
                lastItemsChangedAtUtc: itemChangedTime,
                upToDateCheckImplicitConfiguredInput: configuredInput);

            _fileSystem.AddFile(_builtPath, outputTime);
            _fileSystem.AddFile(@"C:\Dev\Solution\Project\ItemPath1", inputTime);

            await AssertUpToDateAsync(
                """
                Project is up-to-date.
                """,
                targetFramework: "betaFramework");
        }

        #region Test helpers

        private void SetCopyItems(params CopyItem[] items)
        {
            _copyItems = new (string Path, ImmutableArray<CopyItem> CopyItems)[]
            {
                (_projectPath, items.ToImmutableArray())
            };
        }
        private static string ToLocalTime(DateTime time)
        {
            return time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private async Task AssertNotUpToDateAsync(string? expectedLogOutput = null, string? telemetryReason = null, BuildAction buildAction = BuildAction.Build, string ignoreKinds = "", string targetFramework = "")
        {
            var writer = new AssertWriter(_output, expectedLogOutput ?? "");

            var isUpToDate = await _buildUpToDateCheck.IsUpToDateAsync(buildAction, writer, CreateGlobalProperties(ignoreKinds, targetFramework));

            writer.Assert();

            if (telemetryReason is not null)
                AssertTelemetryFailureEvent(telemetryReason, ignoreKinds);
            else
                Assert.Empty(_telemetryEvents);

            Assert.False(isUpToDate, "Expected not up-to-date, but was.");

            if (buildAction == BuildAction.Build)
            {
                if (telemetryReason == "Disabled")
                {
                    await ValidateUpToDateAsync();
                }
                else
                {
                    await ValidateNotUpToDateAsync();
                }
            }
        }

        private async Task AssertUpToDateAsync(string expectedLogOutput, string ignoreKinds = "", string targetFramework = "")
        {
            var writer = new AssertWriter(_output, expectedLogOutput);

            bool isUpToDate = await _buildUpToDateCheck.IsUpToDateAsync(BuildAction.Build, writer, CreateGlobalProperties(ignoreKinds, targetFramework));

            // Check the output message first, as if other checks would fail,
            // the actual output should explain what's going on.
            writer.Assert();

            Assert.True(isUpToDate);

            AssertTelemetrySuccessEvent(ignoreKinds);

            await ValidateUpToDateAsync();
        }

        private async Task ValidateUpToDateAsync()
        {
            IBuildUpToDateCheckValidator validator = _buildUpToDateCheck;

            (bool isUpToDate, string? failureReason, string? failureDescription) = await validator.ValidateUpToDateAsync();

            Assert.True(isUpToDate);
            Assert.Null(failureReason);
            Assert.Null(failureDescription);
        }

        private async Task ValidateNotUpToDateAsync()
        {
            IBuildUpToDateCheckValidator validator = _buildUpToDateCheck;

            (bool isUpToDate, string? failureReason, string? failureDescription) = await validator.ValidateUpToDateAsync();

            Assert.False(isUpToDate);
            Assert.NotNull(failureReason);
            Assert.NotNull(failureDescription);
        }

        private void AssertTelemetryFailureEvent(string reason, string ignoreKinds)
        {
            var telemetryEvent = Assert.Single(_telemetryEvents);

            Assert.Equal(TelemetryEventName.UpToDateCheckFail, telemetryEvent.EventName);
            Assert.NotNull(telemetryEvent.Properties);
            Assert.Equal(10, telemetryEvent.Properties.Count);

            var reasonProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.FailReason));
            Assert.Equal(reason, reasonProp.propertyValue);

            var durationProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.DurationMillis));
            var duration = Assert.IsType<double>(durationProp.propertyValue);
            Assert.True(duration > 0.0);

            var waitDurationProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.WaitDurationMillis));
            var waitDuration = Assert.IsType<double>(waitDurationProp.propertyValue);
            Assert.True(waitDuration > 0.0);

            var fileCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.FileCount));
            var fileCount = Assert.IsType<int>(fileCountProp.propertyValue);
            Assert.True(fileCount >= 0);

            var configurationCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.ConfigurationCount));
            var configurationCount = Assert.IsType<int>(configurationCountProp.propertyValue);
            Assert.Equal(1, configurationCount);

            var logLevelProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.LogLevel));
            var logLevel = Assert.IsType<LogLevel>(logLevelProp.propertyValue);
            Assert.Equal(_logLevel, logLevel);

            var ignoreKindsProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.IgnoreKinds));
            var ignoreKindsStr = Assert.IsType<string>(ignoreKindsProp.propertyValue);
            Assert.Equal(ignoreKinds, ignoreKindsStr);

            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.Project));
            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.CheckNumber));
            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.AccelerationResult));

            _telemetryEvents.Clear();
        }

        private void AssertTelemetrySuccessEvent(string ignoreKinds)
        {
            var telemetryEvent = Assert.Single(_telemetryEvents);

            Assert.Equal(TelemetryEventName.UpToDateCheckSuccess, telemetryEvent.EventName);

            Assert.NotNull(telemetryEvent.Properties);
            Assert.Equal(10, telemetryEvent.Properties.Count);

            var durationProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.DurationMillis));
            var duration = Assert.IsType<double>(durationProp.propertyValue);
            Assert.True(duration > 0.0);

            var waitDurationProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.WaitDurationMillis));
            var waitDuration = Assert.IsType<double>(waitDurationProp.propertyValue);
            Assert.True(waitDuration > 0.0);

            var fileCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.FileCount));
            var fileCount = Assert.IsType<int>(fileCountProp.propertyValue);
            Assert.True(fileCount >= 0);

            var configurationCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.ConfigurationCount));
            var configurationCount = Assert.IsType<int>(configurationCountProp.propertyValue);
            Assert.True(configurationCount == 1);

            var logLevelProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.LogLevel));
            var logLevel = Assert.IsType<LogLevel>(logLevelProp.propertyValue);
            Assert.True(logLevel == _logLevel);

            var ignoreKindsProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.IgnoreKinds));
            var ignoreKindsStr = Assert.IsType<string>(ignoreKindsProp.propertyValue);
            Assert.Equal(ignoreKinds, ignoreKindsStr);

            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.Project));
            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.CheckNumber));
            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.AcceleratedCopyCount));
            Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheck.AccelerationResult));

            _telemetryEvents.Clear();
        }

        private static ImmutableDictionary<string, string> CreateGlobalProperties(string ignoreKinds, string targetFramework)
        {
            var globalProperties = ImmutableDictionary<string, string>.Empty;

            if (ignoreKinds.Length != 0)
            {
                globalProperties = globalProperties.SetItem(BuildUpToDateCheck.FastUpToDateCheckIgnoresKindsGlobalPropertyName, ignoreKinds);
            }

            if (targetFramework.Length != 0)
            {
                globalProperties = globalProperties.SetItem(BuildUpToDateCheck.TargetFrameworkGlobalPropertyName, targetFramework);
            }

            return globalProperties;
        }

        private sealed class AssertWriter : TextWriter
        {
            private readonly StringBuilder _actual = new();
            private readonly ITestOutputHelper _output;
            private readonly string _expected;

            public AssertWriter(ITestOutputHelper output, string expected)
            {
                _output = output;
                _expected = expected;
            }

            public override Encoding Encoding { get; } = Encoding.UTF8;

            public override void WriteLine(string value)
            {
                value = value.Substring("FastUpToDate: ".Length);
                value = value.Substring(0, value.Length - $" ({Path.GetFileNameWithoutExtension(_projectPath)})".Length);

                _actual.AppendLine(value);
            }

            public void Assert()
            {
                var actual = _actual.ToString();

                // Verbose output includes a line "Up-to-date check completed in {N} ms".
                // We cannot predict the value of N, so we validate the text is there, and remove it before comparing.
                int index = actual.IndexOf("Up-to-date check completed in ", StringComparison.Ordinal);
                if (index != -1)
                {
                    actual = actual.Substring(0, index).TrimEnd();
                }
                actual = actual.TrimEnd();

                if (!string.Equals(_expected, actual))
                {
                    _output.WriteLine("Expected:");
                    _output.WriteLine(_expected);
                    _output.WriteLine("");
                    _output.WriteLine("Actual:");
                    _output.WriteLine(actual);
                }

                Xunit.Assert.Equal(_expected, actual);
            }
        }

        #endregion
    }
}
