// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;
using Moq;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable IDE0055
#pragma warning disable IDE0058

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    public sealed class BuildUpToDateCheckTests : BuildUpToDateCheckTestBase, IDisposable
    {
        private const string _projectFullPath = "C:\\Dev\\Solution\\Project\\Project.csproj";
        private const string _msBuildProjectFullPath = "NewProjectFullPath";
        private const string _msBuildProjectDirectory = "NewProjectDirectory";
        private const string _msBuildAllProjects = "Project1;Project2";
        private const string _outputPath = "NewOutputPath";

        private readonly DateTime _projectFileTimeUtc = new(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly List<ITelemetryServiceFactory.TelemetryParameters> _telemetryEvents = new();
        private readonly Dictionary<ProjectConfiguration, DateTime> _lastCheckTimeAtUtc = new();

        private readonly BuildUpToDateCheck _buildUpToDateCheck;
        private readonly ITestOutputHelper _output;
        private readonly IFileSystemMock _fileSystem;

        // Values returned by mocks that may be modified in test cases as needed
        private bool _isTaskQueueEmpty = true;
        private bool _isFastUpToDateCheckEnabled = true;

        private UpToDateCheckConfiguredInput? _state;

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
                .ReturnsAsync(LogLevel.Info);
            projectSystemOptions.Setup(o => o.GetIsFastUpToDateCheckEnabledAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _isFastUpToDateCheckEnabled);

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
            configuredProject.SetupGet(c => c.UnconfiguredProject).Returns(UnconfiguredProjectFactory.Create(fullPath: _projectFullPath));

            var projectAsynchronousTasksService = new Mock<IProjectAsynchronousTasksService>(MockBehavior.Strict);
            projectAsynchronousTasksService.SetupGet(s => s.UnloadCancellationToken).Returns(CancellationToken.None);
            projectAsynchronousTasksService.Setup(s => s.IsTaskQueueEmpty(ProjectCriticalOperation.Build)).Returns(() => _isTaskQueueEmpty);

            _fileSystem = new IFileSystemMock();
            _fileSystem.AddFile(_msBuildProjectFullPath, _projectFileTimeUtc);
            _fileSystem.AddFile("Project1", _projectFileTimeUtc);
            _fileSystem.AddFolder(_msBuildProjectDirectory);
            _fileSystem.AddFolder(_outputPath);

            var upToDateCheckHost = new Mock<IUpToDateCheckHost>(MockBehavior.Strict);

            _buildUpToDateCheck = new BuildUpToDateCheck(
                inputDataSource.Object,
                projectSystemOptions.Object,
                configuredProject.Object,
                projectAsynchronousTasksService.Object,
                ITelemetryServiceFactory.Create(telemetryParameters => _telemetryEvents.Add(telemetryParameters)),
                _fileSystem,
                upToDateCheckHost.Object);
        }

        public void Dispose() => _buildUpToDateCheck.Dispose();

        private async Task<UpToDateCheckImplicitConfiguredInput> SetupAsync(
            Dictionary<string, IProjectRuleSnapshotModel>? projectSnapshot = null,
            Dictionary<string, IProjectRuleSnapshotModel>? sourceSnapshot = null,
            bool disableFastUpToDateCheck = false,
            string outDir = _outputPath,
            DateTime? lastCheckTimeAtUtc = null,
            DateTime? lastItemsChangedAtUtc = null,
            DateTime? updateLastCheckedAtUtcValue = null,
            UpToDateCheckImplicitConfiguredInput? upToDateCheckImplicitConfiguredInput = null,
            bool itemRemovedFromSourceSnapshot = false)
        {
            upToDateCheckImplicitConfiguredInput ??= UpToDateCheckImplicitConfiguredInput.CreateEmpty(ProjectConfigurationFactory.Create("testConfiguration"));

            _lastCheckTimeAtUtc[upToDateCheckImplicitConfiguredInput.ProjectConfiguration] = lastCheckTimeAtUtc ?? DateTime.MinValue;
            
            projectSnapshot ??= new Dictionary<string, IProjectRuleSnapshotModel>();

            if (!projectSnapshot.ContainsKey(ConfigurationGeneral.SchemaName))
            {
                projectSnapshot[ConfigurationGeneral.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Properties = ImmutableStringDictionary<string>.EmptyOrdinal
                        .Add("MSBuildProjectFullPath", _msBuildProjectFullPath)
                        .Add("MSBuildProjectDirectory", _msBuildProjectDirectory)
                        .Add("MSBuildAllProjects", _msBuildAllProjects)
                        .Add("OutputPath", _outputPath)
                        .Add("OutDir", outDir)
                        .Add("DisableFastUpToDateCheck", disableFastUpToDateCheck.ToString())
                };
            }

            UpToDateCheckImplicitConfiguredInput configuredInput = UpdateState(
                upToDateCheckImplicitConfiguredInput,
                projectSnapshot,
                sourceSnapshot,
                itemRemovedFromSourceSnapshot: itemRemovedFromSourceSnapshot);

            if (lastItemsChangedAtUtc != null)
            {
                configuredInput = configuredInput.WithLastItemsChangedAtUtc(lastItemsChangedAtUtc.Value);
            }

            _state = new UpToDateCheckConfiguredInput(ImmutableArray.Create(configuredInput));

            var subscription = new Mock<BuildUpToDateCheck.ISubscription>(MockBehavior.Strict);
            
            subscription.Setup(s => s.EnsureInitialized());

            subscription
                .Setup(s => s.UpdateLastCheckedAtUtc())
                .Callback(() => _lastCheckTimeAtUtc[upToDateCheckImplicitConfiguredInput.ProjectConfiguration] = updateLastCheckedAtUtcValue ?? DateTime.UtcNow);

            subscription
                .Setup(s => s.UpdateLastCheckedAtUtc(It.IsAny<IEnumerable<ProjectConfiguration>>()))
                .Callback((IEnumerable<ProjectConfiguration> configs) =>
                {
                    foreach (var config in configs)
                    {
                        _lastCheckTimeAtUtc[config] = updateLastCheckedAtUtcValue ?? DateTime.UtcNow;
                    }
                });

            subscription
                .Setup(s => s.RunAsync(It.IsAny<Func<UpToDateCheckConfiguredInput, IReadOnlyDictionary<ProjectConfiguration, DateTime>, CancellationToken, Task<(bool, ImmutableArray<ProjectConfiguration>)>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(async (Func<UpToDateCheckConfiguredInput, IReadOnlyDictionary<ProjectConfiguration, DateTime>, CancellationToken, Task<(bool, ImmutableArray<ProjectConfiguration>)>>  func, bool updateLastCheckedAt, CancellationToken token) =>
                {
                    Assumes.NotNull(_state);
                    var result = await func(_state, _lastCheckTimeAtUtc, token);
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
            _isFastUpToDateCheckEnabled = isEnabled;

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
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(lastCheckTimeAtUtc: lastCheckTime);

            await AssertUpToDateAsync("No build outputs defined.");

            await AssertNotUpToDateAsync(buildAction: buildAction);
        }

        [Fact]
        public async Task IsUpToDateAsync_False_BuildTasksActive()
        {
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(lastCheckTimeAtUtc: lastCheckTime);

            await AssertUpToDateAsync("No build outputs defined.");

            _isTaskQueueEmpty = false;

            await AssertNotUpToDateAsync(
                "Critical build tasks are running, not up-to-date.",
                "CriticalTasks");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_ItemsChanged()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = SimpleItems("ItemPath1", "ItemPath2")
            };

            var outputTime      = DateTime.UtcNow.AddMinutes(-3);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-2);
            var itemChangedTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangedTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuiltOutputPath1", outputTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    $"The set of project items was changed more recently ({itemChangedTime.ToLocalTime()}) than the earliest output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' ({outputTime.ToLocalTime()}), not up-to-date.",
                    "    Content item added 'ItemPath1' (CopyType=CopyNever, TargetPath='')",
                    "    Content item added 'ItemPath2' (CopyType=CopyNever, TargetPath='')",
                },
                "ProjectItemsChangedSinceEarliestOutput");
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
        public async Task IsUpToDateAsync_False_CopyAlwaysItemExists()
        {
            // TODO add a Content or None item as CopyAlways and verify (these are currently excluded by CollectInputs)

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Items = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal
                        .Add("ItemPath1", ImmutableStringDictionary<string>.EmptyOrdinal
                            .Add("CopyToOutputDirectory", "Always")) // ALWAYS COPY THIS ITEM
                        .Add("ItemPath2", ImmutableStringDictionary<string>.EmptyOrdinal)
                }
            };

            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(sourceSnapshot: sourceSnapshot, lastCheckTimeAtUtc: lastCheckTime);

            await AssertNotUpToDateAsync(
                "Item 'C:\\Dev\\Solution\\Project\\ItemPath1' has CopyToOutputDirectory set to 'Always', not up-to-date.",
                "CopyAlwaysItemExists");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_OutputItemDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(projectSnapshot: projectSnapshot, lastCheckTimeAtUtc: lastCheckTime);

            await AssertNotUpToDateAsync(
                "Output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' does not exist, not up-to-date.",
                "OutputNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_ZeroFilesInProjectAfterItemDeletion()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var inputTime = DateTime.UtcNow.AddMinutes(-5);
            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-3);
            var buildTime = DateTime.UtcNow.AddMinutes(-2);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\ItemPath1", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", buildTime);

            var priorState = await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            await AssertUpToDateAsync(
                $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({buildTime.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\ItemPath1' ({inputTime.ToLocalTime()}).");

            lastCheckTime = DateTime.UtcNow.AddMinutes(-1);
            itemChangeTime = DateTime.UtcNow.AddMinutes(0);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime,
                upToDateCheckImplicitConfiguredInput: priorState,
                itemRemovedFromSourceSnapshot: true);

            await AssertNotUpToDateAsync(new[] {
                $"The set of project items was changed more recently ({itemChangeTime.ToLocalTime()}) than the earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({buildTime.ToLocalTime()}), not up-to-date.",
                "    Compile item removed \'ItemPath1\' (CopyType=CopyNever, TargetPath='')"},
                "ProjectItemsChangedSinceEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_InputItemDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var itemChangedTime = DateTime.UtcNow.AddMinutes(-3);
            var outputTime      = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangedTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuiltOutputPath1", outputTime);

            await AssertNotUpToDateAsync(
                "Input Compile item 'C:\\Dev\\Solution\\Project\\ItemPath1' does not exist and is required, not up-to-date.",
                "InputNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_InputNewerThanBuiltOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var itemChangedTime = DateTime.UtcNow.AddMinutes(-4);
            var outputTime      = DateTime.UtcNow.AddMinutes(-3);
            var inputTime       = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangedTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuiltOutputPath1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\ItemPath1", inputTime);

            await AssertNotUpToDateAsync(
                $"Input Compile item 'C:\\Dev\\Solution\\Project\\ItemPath1' is newer ({inputTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' ({outputTime.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_InputChangedBetweenLastCheckAndEarliestOutput()
        {
            // This test covers a race condition described in https://github.com/dotnet/project-system/issues/4014

            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            // The rule is that no input item should be modified since the last up-to-date check started.

            var t0 = DateTime.UtcNow.AddMinutes(-5); // t0 Output file timestamp
            var t1 = DateTime.UtcNow.AddMinutes(-4); // t1 Input file timestamp
            var t2 = DateTime.UtcNow.AddMinutes(-3); // t2 Check up-to-date (false) so start a build (setting last checked at time)
            var t3 = DateTime.UtcNow.AddMinutes(-2); // t3 Modify input file (during build)
            var t4 = DateTime.UtcNow.AddMinutes(-1); // t4 Produce first (earliest) output DLL (from t0 input)
                                                     // t5 Check incorrectly claims everything up-to-date, as t3 > t2 (inputTime > lastCheckedTime)

            var inputFile = "C:\\Dev\\Solution\\Project\\ItemPath1";
            var outputPath = "C:\\Dev\\Solution\\Project\\BuiltOutputPath1";

            _fileSystem.AddFile(outputPath, t0);
            _fileSystem.AddFile(inputFile, t1);

            // Run test (t2)
            await SetupAsync(projectSnapshot, sourceSnapshot, lastCheckTimeAtUtc: t2);

            await AssertNotUpToDateAsync(
                $"Input Compile item '{inputFile}' is newer ({t1.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' ({t0.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");

            // Modify input while build in progress (t3)
            _fileSystem.AddFile(inputFile, t3);

            // Update write time of output (t4)
            _fileSystem.AddFile(outputPath, t4);

            // Run check again (t5)
            await AssertNotUpToDateAsync(
                $"Input Compile item '{inputFile}' ({t3.ToLocalTime()}) has been modified since the last up-to-date check ({t2.ToLocalTime()}), not up-to-date.",
                "InputModifiedSinceLastCheck");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_RebuildUpdatesLastCheckedTime()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            // The rule is that no input item should be modified since the last up-to-date check started.

            var t0 = DateTime.UtcNow.AddMinutes(-6); // t0 Output file timestamp
            var t1 = DateTime.UtcNow.AddMinutes(-5); // t1 Input file timestamp
            var t2 = DateTime.UtcNow.AddMinutes(-4); // t2 Check up-to-date (false) so start a build (setting last checked at time)
            var t3 = DateTime.UtcNow.AddMinutes(-3); // t3 Modify input file (during build)
            var t4 = DateTime.UtcNow.AddMinutes(-2); // t4 start rebuild (setting lastCheckedTime)
            var t5 = DateTime.UtcNow.AddMinutes(-1); // t5 Produce first (earliest) output DLL (from t0 input)
                                                     // t6 Check incorrectly claims everything up-to-date, as t3 < t5

            var inputFile = "C:\\Dev\\Solution\\Project\\ItemPath1";
            var outputPath = "C:\\Dev\\Solution\\Project\\BuiltOutputPath1";

            _fileSystem.AddFile(outputPath, t0);
            _fileSystem.AddFile(inputFile, t1);

            // Run test (t2)
            await SetupAsync(projectSnapshot, sourceSnapshot, lastCheckTimeAtUtc: t2, updateLastCheckedAtUtcValue: t4);

            await AssertNotUpToDateAsync(
                $"Input Compile item '{inputFile}' is newer ({t1.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' ({t0.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");

            // Modify input while build in progress (t3)
            _fileSystem.AddFile(inputFile, t3);

            // Rebuild
            ((IBuildUpToDateCheckProviderInternal)_buildUpToDateCheck).NotifyRebuildStarting();

            // Update write time of output (t5)
            _fileSystem.AddFile(outputPath, t5);

            // Run check again (t6)
            await AssertUpToDateAsync(
                $"No inputs are newer than earliest output '{outputPath}' ({t5.ToLocalTime()}). Newest input is '{inputFile}' ({t3.ToLocalTime()}).");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_NewProjectRebuiltThenBuilt()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            // The rule is that no input item should be modified since the last up-to-date check started.

            var t0 = DateTime.UtcNow.AddMinutes(-3); // t0 Input file timestamp
            var t1 = DateTime.UtcNow.AddMinutes(-2); // t1 Rebuild (sets output file timestamp)
            var t2 = DateTime.UtcNow.AddMinutes(-1); // t2 Check up-to-date (true)

            var inputFile = "C:\\Dev\\Solution\\Project\\ItemPath1";
            var outputPath = "C:\\Dev\\Solution\\Project\\BuiltOutputPath1";

            _fileSystem.AddFile(inputFile, t0);

            _fileSystem.AddFile(outputPath, t1);

            await SetupAsync(projectSnapshot, sourceSnapshot, updateLastCheckedAtUtcValue: t1);

            // Rebuild (t1)
            ((IBuildUpToDateCheckProviderInternal)_buildUpToDateCheck).NotifyRebuildStarting();

            // Run test (t2)
            await AssertUpToDateAsync(
                $"No inputs are newer than earliest output '{outputPath}' ({t1.ToLocalTime()}). Newest input is '{inputFile}' ({t0.ToLocalTime()}).");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CompileItemNewerThanCustomOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckOutput.SchemaName] = SimpleItems("CustomOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var outputTime      = DateTime.UtcNow.AddMinutes(-3);
            var compileItemTime = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\CustomOutputPath1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\ItemPath1", compileItemTime);

            await AssertNotUpToDateAsync(
                $"Input Compile item 'C:\\Dev\\Solution\\Project\\ItemPath1' is newer ({compileItemTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\CustomOutputPath1' ({outputTime.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyReferenceInputNewerThanMarkerOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [CopyUpToDateMarker.SchemaName] = SimpleItems("Marker"),
                [ResolvedCompilationReference.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Items = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal
                        .Add("Reference1", ImmutableStringDictionary<string>.EmptyOrdinal
                            .Add("CopyUpToDateMarker", "Reference1MarkerPath")
                            .Add("ResolvedPath", "Reference1ResolvedPath")
                            .Add("OriginalPath", "Reference1OriginalPath"))
                }
            };

            var lastCheckTime = DateTime.UtcNow.AddMinutes(-5);

            await SetupAsync(projectSnapshot: projectSnapshot, lastCheckTimeAtUtc: lastCheckTime);

            var outputTime   = DateTime.UtcNow.AddMinutes(-4);
            var resolvedTime = DateTime.UtcNow.AddMinutes(-3);
            var markerTime   = DateTime.UtcNow.AddMinutes(-2);
            var originalTime = DateTime.UtcNow.AddMinutes(-1);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Marker", outputTime);
            _fileSystem.AddFile("Reference1ResolvedPath", resolvedTime);
            _fileSystem.AddFile("Reference1MarkerPath", markerTime);
            _fileSystem.AddFile("Reference1OriginalPath", originalTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Latest write timestamp on input marker is {originalTime.ToLocalTime()} on 'Reference1OriginalPath'.",
                    $"Write timestamp on output marker is {outputTime.ToLocalTime()} on 'C:\\Dev\\Solution\\Project\\Marker'.",
                    "Input marker is newer than output marker, not up-to-date."
                },
                "InputMarkerNewerThanOutputMarker");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_AnalyzerReferenceNewerThanEarliestOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1"),
                [ResolvedAnalyzerReference.SchemaName] = ItemWithMetadata("Analyzer1", "ResolvedPath", "Analyzer1ResolvedPath")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var outputTime     = DateTime.UtcNow.AddMinutes(-3);
            var inputTime      = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            var outputItem = "C:\\Dev\\Solution\\Project\\BuiltOutputPath1";
            var analyzerItem = "C:\\Dev\\Solution\\Project\\Analyzer1ResolvedPath";

            _fileSystem.AddFile(outputItem, outputTime);
            _fileSystem.AddFile(analyzerItem, inputTime);

            await AssertNotUpToDateAsync(
                $"Input ResolvedAnalyzerReference item '{analyzerItem}' is newer ({inputTime.ToLocalTime()}) than earliest output '{outputItem}' ({outputTime.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CompilationReferenceNewerThanEarliestOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1"),
                [ResolvedCompilationReference.SchemaName] = new IProjectRuleSnapshotModel
                {
                    Items = ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal
                        .Add("Reference1", ImmutableStringDictionary<string>.EmptyOrdinal
                            .Add("CopyUpToDateMarker", "Reference1MarkerPath")
                            .Add("ResolvedPath", "C:\\Dev\\Solution\\Project\\Reference1ResolvedPath")
                            .Add("OriginalPath", "..\\Project\\Reference1OriginalPath"))
                }
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuiltOutputPath1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Reference1ResolvedPath", inputTime);

            await AssertNotUpToDateAsync(
                $"Input ResolvedCompilationReference item 'C:\\Dev\\Solution\\Project\\Reference1ResolvedPath' is newer ({inputTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' ({outputTime.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_UpToDateCheckInputNewerThanEarliestOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1"),
                [UpToDateCheckInput.SchemaName] = SimpleItems("Item1", "Item2")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuiltOutputPath1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Item1", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Item2", outputTime);

            await AssertNotUpToDateAsync(
                $"Input UpToDateCheckInput item 'C:\\Dev\\Solution\\Project\\Item1' is newer ({inputTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\BuiltOutputPath1' ({outputTime.ToLocalTime()}), not up-to-date.",
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Sets_InputNewerThanOutput()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
                [UpToDateCheckInput.SchemaName] = ItemWithMetadata("Input1", "Set", "Set1"),
                [UpToDateCheckOutput.SchemaName] = ItemWithMetadata("Output1", "Set", "Set1")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input1", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", outputTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({outputTime.ToLocalTime()}). Newest input is '{_msBuildProjectFullPath}' ({_projectFileTimeUtc.ToLocalTime()}).",
                    $"Input UpToDateCheckInput item 'C:\\Dev\\Solution\\Project\\Input1' is newer ({inputTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\Output1' ({outputTime.ToLocalTime()}), not up-to-date."
                },
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Sets_InputOlderThanOutput_MultipleSets()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
                [UpToDateCheckInput.SchemaName] = Union(ItemWithMetadata("Input1", "Set", "Set1"), ItemWithMetadata("Input2", "Set", "Set2")),
                [UpToDateCheckOutput.SchemaName] = Union(ItemWithMetadata("Output1", "Set", "Set1"), ItemWithMetadata("Output2", "Set", "Set2"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-6);
            var inputTime1     = DateTime.UtcNow.AddMinutes(-5);
            var outputTime1    = DateTime.UtcNow.AddMinutes(-4);
            var inputTime2     = DateTime.UtcNow.AddMinutes(-3);
            var outputTime2    = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input1", inputTime1);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output1", outputTime1);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input2", inputTime2);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output2", outputTime2);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", outputTime1);

            await AssertUpToDateAsync(
                $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({outputTime1.ToLocalTime()}). Newest input is '{_msBuildProjectFullPath}' ({_projectFileTimeUtc.ToLocalTime()}).",
                $"In Set=\"Set1\", no inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\Output1' ({outputTime1.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\Input1' ({inputTime1.ToLocalTime()}).",
                $"In Set=\"Set2\", no inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\Output2' ({outputTime2.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\Input2' ({inputTime2.ToLocalTime()}).");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Sets_InputNewerThanOutput_MultipleSets()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
                [UpToDateCheckInput.SchemaName] = Union(ItemWithMetadata("Input1", "Set", "Set1"), ItemWithMetadata("Input2", "Set", "Set2")),
                [UpToDateCheckOutput.SchemaName] = Union(ItemWithMetadata("Output1", "Set", "Set1"), ItemWithMetadata("Output2", "Set", "Set2"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-6);
            var inputTime1     = DateTime.UtcNow.AddMinutes(-5);
            var outputTime1    = DateTime.UtcNow.AddMinutes(-4);
            var outputTime2    = DateTime.UtcNow.AddMinutes(-3);
            var inputTime2     = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input1", inputTime1);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output1", outputTime1);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input2", inputTime2);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output2", outputTime2);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", outputTime1);

            await AssertNotUpToDateAsync(
                new[]
                {
                    $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({outputTime1.ToLocalTime()}). Newest input is '{_msBuildProjectFullPath}' ({_projectFileTimeUtc.ToLocalTime()}).",
                    $"In Set=\"Set1\", no inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\Output1' ({outputTime1.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\Input1' ({inputTime1.ToLocalTime()}).",
                    $"Input UpToDateCheckInput item 'C:\\Dev\\Solution\\Project\\Input2' is newer ({inputTime2.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\Output2' ({outputTime2.ToLocalTime()}), not up-to-date."
                },
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Sets_InputNewerThanOutput_ItemInMultipleSets()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
                [UpToDateCheckInput.SchemaName] = ItemWithMetadata("Input", "Set", "Set1;Set2"),
                [UpToDateCheckOutput.SchemaName] = Union(ItemWithMetadata("Output1", "Set", "Set1"), ItemWithMetadata("Output2", "Set", "Set2"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-5);
            var outputTime2    = DateTime.UtcNow.AddMinutes(-4);
            var inputTime      = DateTime.UtcNow.AddMinutes(-3);
            var outputTime1    = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-1);
            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output1", outputTime1);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output2", outputTime2);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", outputTime1);

            await AssertNotUpToDateAsync(
                new[]
                {
                    $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({outputTime1.ToLocalTime()}). Newest input is '{_msBuildProjectFullPath}' ({_projectFileTimeUtc.ToLocalTime()}).",
                    $"In Set=\"Set1\", no inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\Output1' ({outputTime1.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\Input' ({inputTime.ToLocalTime()}).",
                    $"Input UpToDateCheckInput item 'C:\\Dev\\Solution\\Project\\Input' is newer ({inputTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\Output2' ({outputTime2.ToLocalTime()}), not up-to-date."
                },
                "InputNewerThanEarliestOutput");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Sets_InputOnly()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
                [UpToDateCheckInput.SchemaName] = ItemWithMetadata("Input1", "Set", "Set1"),
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var buildTime      = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input1", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", buildTime);

            await AssertUpToDateAsync(
                $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({buildTime.ToLocalTime()}). Newest input is '{_msBuildProjectFullPath}' ({_projectFileTimeUtc.ToLocalTime()}).",
                "No build outputs defined in Set=\"Set1\".");
        }

        [Fact]
        public async Task IsUpToDateAsync_True_Sets_OutputOnly()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuildDefault"),
                [UpToDateCheckOutput.SchemaName] = ItemWithMetadata("Output1", "Set", "Set1")
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-3);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-2);
            var outputTime     = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuildDefault", outputTime);

            await AssertUpToDateAsync(
                $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\BuildDefault' ({outputTime.ToLocalTime()}). Newest input is '{_msBuildProjectFullPath}' ({_projectFileTimeUtc.ToLocalTime()}).",
                "No inputs defined in Set=\"Set1\".");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_Kinds_InputNewerThanOutput_WithIgnoredKind()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemsWithMetadata(("Built", "Kind", ""), ("IgnoredBuilt", "Kind", "Ignored")),
                [UpToDateCheckInput.SchemaName] = ItemsWithMetadata(("Input", "Kind", ""), ("IgnoredInput", "Kind", "Ignored")),
                [UpToDateCheckOutput.SchemaName] = ItemsWithMetadata(("Output", "Kind", ""), ("IgnoredOutput", "Kind", "Ignored"))
            };

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var outputTime     = DateTime.UtcNow.AddMinutes(-2);
            var inputTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Built", outputTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "Ignoring up-to-date check items with Kind=\"Ignored\"",
                    $"Input UpToDateCheckInput item 'C:\\Dev\\Solution\\Project\\Input' is newer ({inputTime.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\Output' ({outputTime.ToLocalTime()}), not up-to-date.",
                },
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
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-4);
            var output1Time    = DateTime.UtcNow.AddMinutes(-3);
            var output2Time    = DateTime.UtcNow.AddMinutes(-2);
            var input1Time     = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input",        input1Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\TaggedInput",  input2Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output",       output1Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\TaggedOutput", output2Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Built",        output1Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\TaggedBuilt",  output2Time);

            await AssertNotUpToDateAsync(
                $"Input UpToDateCheckInput item 'C:\\Dev\\Solution\\Project\\Input' is newer ({input1Time.ToLocalTime()}) than earliest output 'C:\\Dev\\Solution\\Project\\Output' ({output1Time.ToLocalTime()}), not up-to-date.",
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
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-2);
            var outputTime     = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input", inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Built", outputTime);

            await AssertUpToDateAsync(
                new[]
                {
                    "Ignoring up-to-date check items with Kind=\"Ignored\"",
                    $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\Output' ({outputTime.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\Input' ({inputTime.ToLocalTime()})."
                },
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

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-7);
            var inputTime      = DateTime.UtcNow.AddMinutes(-6);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-5);
            var output4Time    = DateTime.UtcNow.AddMinutes(-4);
            var output3Time    = DateTime.UtcNow.AddMinutes(-3);
            var output2Time    = DateTime.UtcNow.AddMinutes(-2);
            var output1Time    = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Input",        inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\TaggedInput",  inputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Output",       output4Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\TaggedOutput", output3Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\Built",        output2Time);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\TaggedBuilt",  output1Time);

            await AssertUpToDateAsync(
                new[] { $"No inputs are newer than earliest output 'C:\\Dev\\Solution\\Project\\Output' ({output4Time.ToLocalTime()}). Newest input is 'C:\\Dev\\Solution\\Project\\TaggedInput' ({inputTime.ToLocalTime()})." },
                ignoreKinds: "");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopiedOutputFileSourceIsNewerThanDestination()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemWithMetadata("CopiedOutputDestination", UpToDateCheckBuilt.OriginalProperty, "CopiedOutputSource")
            };

            var destinationPath = @"C:\Dev\Solution\Project\CopiedOutputDestination";
            var sourcePath = @"C:\Dev\Solution\Project\CopiedOutputSource";

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking copied output ({UpToDateCheckBuilt.SchemaName} with {UpToDateCheckBuilt.OriginalProperty} property) file:",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"    Destination {destinationTime.ToLocalTime()}: '{destinationPath}'",
                    "Source is newer than build output destination, not up-to-date."
                },
                "CopySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutDirSourceIsNewerThanDestination()
        {
            const string outDirSnapshot = "newOutDir";

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", "CopyToOutputDirectory", "PreserveNewest")
            };

            await _buildUpToDateCheck.ActivateAsync();

            var destinationPath = @"NewProjectDirectory\newOutDir\Item1";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                outDir: outDirSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"    Destination {destinationTime.ToLocalTime()}: '{destinationPath}'",
                    $"PreserveNewest source '{sourcePath}' is newer than destination '{destinationPath}', not up-to-date."
                },
                "CopyToOutputDirectorySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopiedOutputFileSourceDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemWithMetadata("CopiedOutputDestination", UpToDateCheckBuilt.OriginalProperty, "CopiedOutputSource")
            };

            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(projectSnapshot, lastCheckTimeAtUtc: lastCheckTime);

            var destinationPath = @"C:\Dev\Solution\Project\CopiedOutputDestination";
            var sourcePath = @"C:\Dev\Solution\Project\CopiedOutputSource";

            _fileSystem.AddFile(destinationPath);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking copied output ({UpToDateCheckBuilt.SchemaName} with {UpToDateCheckBuilt.OriginalProperty} property) file:",
                    $"Source '{sourcePath}' does not exist for copy to '{destinationPath}', not up-to-date."
                },
                "CopySourceNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopiedOutputFileDestinationDoesNotExist()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = ItemWithMetadata("CopiedOutputDestination", UpToDateCheckBuilt.OriginalProperty, "CopiedOutputSource")
            };

            var destinationPath = @"C:\Dev\Solution\Project\CopiedOutputDestination";
            var sourcePath = @"C:\Dev\Solution\Project\CopiedOutputSource";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var sourceTime     = DateTime.UtcNow.AddMinutes(-2);

            await SetupAsync(
                projectSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking copied output ({UpToDateCheckBuilt.SchemaName} with {UpToDateCheckBuilt.OriginalProperty} property) file:",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"Destination '{destinationPath}' does not exist for copy from '{sourcePath}', not up-to-date."
                },
                "CopyDestinationNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_SourceIsNewerThanDestination()
        {
            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", "CopyToOutputDirectory", "PreserveNewest")
            };

            var destinationPath = @"NewProjectDirectory\NewOutputPath\Item1";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime      = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"    Destination {destinationTime.ToLocalTime()}: '{destinationPath}'",
                    $"PreserveNewest source '{sourcePath}' is newer than destination '{destinationPath}', not up-to-date."
                },
                "CopyToOutputDirectorySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_SourceIsNewerThanDestination_TargetPath()
        {
            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", ("CopyToOutputDirectory", "PreserveNewest"), ("TargetPath", "TargetPath"))
            };

            var destinationPath = @"NewProjectDirectory\NewOutputPath\TargetPath";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"    Destination {destinationTime.ToLocalTime()}: '{destinationPath}'",
                    $"PreserveNewest source '{sourcePath}' is newer than destination '{destinationPath}', not up-to-date."
                },
                "CopyToOutputDirectorySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_SourceIsNewerThanDestination_Link()
        {
            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", ("CopyToOutputDirectory", "PreserveNewest"), ("Link", "LinkPath"))
            };

            var destinationPath = @"NewProjectDirectory\NewOutputPath\LinkPath";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"    Destination {destinationTime.ToLocalTime()}: '{destinationPath}'",
                    $"PreserveNewest source '{sourcePath}' is newer than destination '{destinationPath}', not up-to-date."
                },
                "CopyToOutputDirectorySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_SourceIsNewerThanDestination_TargetPathAndLink()
        {
            // When both "Link" and "TargetPath" are present, "TargetPath" takes precedence

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", ("CopyToOutputDirectory", "PreserveNewest"), ("Link", "LinkPath"), ("TargetPath", "TargetPath"))
            };

            var destinationPath = @"NewProjectDirectory\NewOutputPath\TargetPath";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);
            var sourceTime = DateTime.UtcNow.AddMinutes(-1);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);
            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"    Destination {destinationTime.ToLocalTime()}: '{destinationPath}'",
                    $"PreserveNewest source '{sourcePath}' is newer than destination '{destinationPath}', not up-to-date."
                },
                "CopyToOutputDirectorySourceNewer");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_SourceDoesNotExist()
        {
            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", "CopyToOutputDirectory", "PreserveNewest")
            };

            var destinationPath = @"NewProjectDirectory\NewOutputPath\Item1";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime  = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime   = DateTime.UtcNow.AddMinutes(-3);
            var destinationTime = DateTime.UtcNow.AddMinutes(-2);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(destinationPath, destinationTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"Source '{sourcePath}' does not exist, not up-to-date."
                },
                "CopyToOutputDirectorySourceNotFound");
        }

        [Fact]
        public async Task IsUpToDateAsync_False_CopyToOutputDirectory_DestinationDoesNotExist()
        {
            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Content.SchemaName] = ItemWithMetadata("Item1", "CopyToOutputDirectory", "PreserveNewest")
            };

            var destinationPath = @"NewProjectDirectory\NewOutputPath\Item1";
            var sourcePath = @"C:\Dev\Solution\Project\Item1";

            var itemChangeTime = DateTime.UtcNow.AddMinutes(-4);
            var lastCheckTime  = DateTime.UtcNow.AddMinutes(-3);
            var sourceTime     = DateTime.UtcNow.AddMinutes(-2);

            await SetupAsync(
                sourceSnapshot: sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangeTime);

            _fileSystem.AddFile(sourcePath, sourceTime);

            await AssertNotUpToDateAsync(
                new[]
                {
                    "No build outputs defined.",
                    $"Checking PreserveNewest file '{sourcePath}':",
                    $"    Source {sourceTime.ToLocalTime()}: '{sourcePath}'",
                    $"Destination '{destinationPath}' does not exist, not up-to-date."
                },
                "CopyToOutputDirectoryDestinationNotFound");
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
                    i => i.Paths.Select(p => new UpToDateCheckInputItem(p, i.ItemType, ImmutableDictionary<string, string>.Empty)).ToImmutableArray());

                return BuildUpToDateCheck.ComputeItemHash(itemsByItemType);
            }
        }

        [Fact]
        public async Task IsUpToDateAsync_True_InputNewerThatBuiltOutput_TargetFrameworkDoesNotMatchBuild()
        {
            var projectSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [UpToDateCheckBuilt.SchemaName] = SimpleItems("BuiltOutputPath1")
            };

            var sourceSnapshot = new Dictionary<string, IProjectRuleSnapshotModel>
            {
                [Compile.SchemaName] = SimpleItems("ItemPath1")
            };

            var itemChangedTime = DateTime.UtcNow.AddMinutes(-4);
            var outputTime = DateTime.UtcNow.AddMinutes(-3);
            var inputTime = DateTime.UtcNow.AddMinutes(-2);
            var lastCheckTime = DateTime.UtcNow.AddMinutes(-1);

            var configuredInput = UpToDateCheckImplicitConfiguredInput.CreateEmpty(
                ProjectConfigurationFactory.Create("TargetFramework", "alphaFramework"));

            await SetupAsync(
                projectSnapshot,
                sourceSnapshot,
                lastCheckTimeAtUtc: lastCheckTime,
                lastItemsChangedAtUtc: itemChangedTime,
                upToDateCheckImplicitConfiguredInput: configuredInput);

            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\BuiltOutputPath1", outputTime);
            _fileSystem.AddFile("C:\\Dev\\Solution\\Project\\ItemPath1", inputTime);

            await AssertUpToDateAsync(Enumerable.Empty<string>(),
                targetFramework: "betaFramework");
        }

        #region Test helpers

        private Task AssertNotUpToDateAsync(string? logMessage = null, string? telemetryReason = null, BuildAction buildAction = BuildAction.Build, string ignoreKinds = "")
        {
            return AssertNotUpToDateAsync(logMessage == null ? null : new[] { logMessage }, telemetryReason, buildAction, ignoreKinds);
        }

        private async Task AssertNotUpToDateAsync(IReadOnlyList<string>? logMessages, string? telemetryReason = null, BuildAction buildAction = BuildAction.Build, string ignoreKinds = "", string targetFramework = "")
        {
            var writer = new AssertWriter(_output);

            if (logMessages != null)
            {
                foreach (var logMessage in logMessages)
                {
                    writer.Add(logMessage);
                }
            }

            Assert.False(await _buildUpToDateCheck.IsUpToDateAsync(buildAction, writer, CreateGlobalProperties(ignoreKinds, targetFramework)));

            if (telemetryReason != null)
                AssertTelemetryFailureEvent(telemetryReason);
            else
                Assert.Empty(_telemetryEvents);

            writer.Assert();
        }

        private Task AssertUpToDateAsync(params string[] logMessages)
        {
            return AssertUpToDateAsync(logMessages, "");
        }

        private async Task AssertUpToDateAsync(IEnumerable<string> logMessages, string ignoreKinds = "", string targetFramework = "")
        {
            var writer = new AssertWriter(_output);

            foreach (var logMessage in logMessages)
            {
                writer.Add(logMessage);
            }

            writer.Add("Project is up-to-date.");

            Assert.True(await _buildUpToDateCheck.IsUpToDateAsync(BuildAction.Build, writer, CreateGlobalProperties(ignoreKinds, targetFramework)));
            AssertTelemetrySuccessEvent();
            writer.Assert();
        }

        private void AssertTelemetryFailureEvent(string reason)
        {
            var telemetryEvent = Assert.Single(_telemetryEvents);

            Assert.Equal(TelemetryEventName.UpToDateCheckFail, telemetryEvent.EventName);
            Assert.NotNull(telemetryEvent.Properties);
            Assert.Equal(4, telemetryEvent.Properties.Count);

            var reasonProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckFailReason));
            Assert.Equal(reason, reasonProp.propertyValue);

            var durationProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckDurationMillis));
            var duration = Assert.IsType<double>(durationProp.propertyValue);
            Assert.True(duration > 0.0);

            var fileCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckFileCount));
            var fileCount = Assert.IsType<int>(fileCountProp.propertyValue);
            Assert.True(fileCount >= 0);

            var configurationCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckConfigurationCount));
            var configurationCount = Assert.IsType<int>(configurationCountProp.propertyValue);
            Assert.True(configurationCount == 1);

            _telemetryEvents.Clear();
        }

        private void AssertTelemetrySuccessEvent()
        {
            var telemetryEvent = Assert.Single(_telemetryEvents);

            Assert.Equal(TelemetryEventName.UpToDateCheckSuccess, telemetryEvent.EventName);

            Assert.NotNull(telemetryEvent.Properties);
            Assert.Equal(3, telemetryEvent.Properties.Count);

            var durationProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckDurationMillis));
            var duration = Assert.IsType<double>(durationProp.propertyValue);
            Assert.True(duration > 0.0);

            var fileCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckFileCount));
            var fileCount = Assert.IsType<int>(fileCountProp.propertyValue);
            Assert.True(fileCount >= 0);

            var configurationCountProp = Assert.Single(telemetryEvent.Properties.Where(p => p.propertyName == TelemetryPropertyName.UpToDateCheckConfigurationCount));
            var configurationCount = Assert.IsType<int>(configurationCountProp.propertyValue);
            Assert.True(configurationCount == 1);

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

        private sealed class AssertWriter : TextWriter, IEnumerable
        {
            private readonly ITestOutputHelper _output;
            private readonly Queue<string> _expectedLines;

            public AssertWriter(ITestOutputHelper output, params string[] expectedLines)
            {
                _output = output;
                _expectedLines = new Queue<string>(expectedLines);
            }

            public void Add(string line)
            {
                _expectedLines.Enqueue(line);
            }

            public override Encoding Encoding { get; } = Encoding.UTF8;

            public override void WriteLine(string value)
            {
                if (_expectedLines.Count == 0)
                {
                    throw new Xunit.Sdk.XunitException("Unexpected log message: " + value);
                }

                var expected = $"FastUpToDate: {_expectedLines.Dequeue()} ({Path.GetFileNameWithoutExtension(_projectFullPath)})";

                if (!string.Equals(expected, value))
                {
                    _output.WriteLine("Expected: " + expected);
                    _output.WriteLine("Actual:   " + value);
                }

                Xunit.Assert.Equal(expected, value);
            }

            public void Assert()
            {
                Xunit.Assert.Empty(_expectedLines);
            }

            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException("Only for collection initialiser syntax");
        }

        #endregion
    }
}
