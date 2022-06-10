// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [AppliesTo(AppliesToExpression)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [Export(typeof(IBuildUpToDateCheckValidator))]
    [Export(typeof(IBuildUpToDateCheckProviderInternal))]
    [Export(typeof(IActiveConfigurationComponent))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed partial class BuildUpToDateCheck
        : IBuildUpToDateCheckProvider2,
          IBuildUpToDateCheckValidator,
          IBuildUpToDateCheckProviderInternal,
          IActiveConfigurationComponent,
          IDisposable
    {
        internal const string AppliesToExpression = $"{ProjectCapability.DotNet} + !{ProjectCapabilities.SharedAssetsProject}";

        internal const string FastUpToDateCheckIgnoresKindsGlobalPropertyName = "FastUpToDateCheckIgnoresKinds";
        internal const string TargetFrameworkGlobalPropertyName = "TargetFramework";

        // This analyzer fires for comparisons against the following constants. Disable it in this file.
        #pragma warning disable CA1820 // Test for empty strings using string length

        internal const string DefaultSetName = "";
        internal const string DefaultKindName = "";

        internal static readonly StringComparer SetNameComparer = StringComparers.ItemNames;
        internal static readonly StringComparer KindNameComparer = StringComparers.ItemNames;

        private static ImmutableHashSet<string> NonCompilationItemTypes => ImmutableHashSet<string>.Empty
            .WithComparer(StringComparers.ItemTypes)
            .Add(None.SchemaName)
            .Add(Content.SchemaName);

        private readonly IUpToDateCheckConfiguredInputDataSource _inputDataSource;
        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly IUpToDateCheckStatePersistence _persistence;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly ITelemetryService _telemetryService;
        private readonly IFileSystem _fileSystem;
        private readonly ISafeProjectGuidService _guidService;
        private readonly IUpToDateCheckHost _upToDateCheckHost;

        private IImmutableDictionary<string, string> _lastGlobalProperties = ImmutableStringDictionary<string>.EmptyOrdinal;
        private string _lastFailureReason = "";
        private DateTime _lastBuildStartTimeUtc = DateTime.MinValue;

        private ISubscription _subscription;
        private int _isDisposed;
        private int _checkNumber;

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IUpToDateCheckConfiguredInputDataSource inputDataSource,
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            IUpToDateCheckStatePersistence persistence,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            ITelemetryService telemetryService,
            IFileSystem fileSystem,
            ISafeProjectGuidService guidService,
            IUpToDateCheckHost upToDateCheckHost)
        {
            _inputDataSource = inputDataSource;
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _persistence = persistence;
            _tasksService = tasksService;
            _telemetryService = telemetryService;
            _fileSystem = fileSystem;
            _guidService = guidService;
            _upToDateCheckHost = upToDateCheckHost;
            _subscription = new Subscription(inputDataSource, configuredProject, upToDateCheckHost, persistence);
        }

        public Task ActivateAsync()
        {
            _subscription.EnsureInitialized();

            return Task.CompletedTask;
        }

        public Task DeactivateAsync()
        {
            RecycleSubscription();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0)
            {
                return;
            }

            RecycleSubscription();
        }

        private void RecycleSubscription()
        {
            ISubscription subscription = Interlocked.Exchange(ref _subscription, new Subscription(_inputDataSource, _configuredProject, _upToDateCheckHost, _persistence));

            subscription.Dispose();
        }

        private bool CheckGlobalConditions(Log log, DateTime? lastSuccessfulBuildStartTimeUtc, bool validateFirstRun, UpToDateCheckImplicitConfiguredInput state)
        {
            if (!_tasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build))
            {
                return log.Fail("CriticalTasks", nameof(Resources.FUTD_CriticalBuildTasksRunning));
            }

            if (state.IsDisabled)
            {
                return log.Fail("Disabled", nameof(Resources.FUTD_DisableFastUpToDateCheckTrue));
            }

            if (validateFirstRun && lastSuccessfulBuildStartTimeUtc is null)
            {
                // We haven't observed a successful built yet. Therefore we don't know whether the set
                // of input items we have actually built the outputs we observe on disk. It's possible
                // that an input has been deleted since then. So we schedule a build.
                //
                // Despite the name, "FirstRun" can occur on the second run if the first build didn't
                // complete correctly. The name is kept though as it allows easier correlation between
                // older and newer data.
                return log.Fail("FirstRun", nameof(Resources.FUTD_FirstRun));
            }

            if (lastSuccessfulBuildStartTimeUtc < state.LastItemsChangedAtUtc)
            {
                Assumes.NotNull(lastSuccessfulBuildStartTimeUtc);
                Assumes.NotNull(state.LastItemsChangedAtUtc);

                log.Fail("ProjectItemsChangedSinceLastSuccessfulBuildStart", nameof(Resources.FUTD_SetOfItemsChangedMoreRecentlyThanOutput_2), state.LastItemsChangedAtUtc, lastSuccessfulBuildStartTimeUtc);

                if (log.Level >= LogLevel.Info)
                {
                    log.Indent++;

                    if (state.LastItemChanges.Length == 0)
                    {
                        log.Info(nameof(Resources.FUTD_SetOfChangedItemsIsEmpty));
                    }
                    else
                    {
                        foreach ((bool isAdd, string itemType, UpToDateCheckInputItem item) in state.LastItemChanges.OrderBy(change => change.ItemType).ThenBy(change => change.Item.Path))
                        {
                            log.Info(isAdd ? nameof(Resources.FUTD_ChangedItemsAddition_4) : nameof(Resources.FUTD_ChangedItemsRemoval_4), itemType, item.Path, item.CopyType, item.TargetPath ?? "");
                        }
                    }

                    log.Indent--;
                }

                return false;
            }

            if (state.IsCopyAlwaysOptimizationDisabled)
            {
                // By default, we optimize CopyAlways to only copy if the time stamps or file sizes differ.
                // If we got here, then the user has opted out of that optimisation, and we must fail if any CopyAlways items exist.

                foreach ((string itemType, ImmutableArray<UpToDateCheckInputItem> items) in state.InputSourceItemsByItemType)
                {
                    foreach (UpToDateCheckInputItem item in items)
                    {
                        if (item.CopyType == CopyType.Always)
                        {
                            return log.Fail("CopyAlwaysItemExists", nameof(Resources.FUTD_CopyAlwaysItemExists_2), itemType, _configuredProject.UnconfiguredProject.MakeRooted(item.Path));
                        }
                    }
                }
            }

            return true;
        }

        private bool CheckInputsAndOutputs(Log log, DateTime? lastSuccessfulBuildStartTimeUtc, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, HashSet<string>? ignoreKinds, CancellationToken token)
        {
            // UpToDateCheckInput/Output/Built items have optional 'Set' metadata that determine whether they
            // are treated separately or not. If omitted, such inputs/outputs are included in the default set,
            // which also includes other items such as project files, compilation items, analyzer references, etc.

            // First, validate the relationship between inputs and outputs within the default set.
            if (!CheckInputsAndOutputs(CollectDefaultInputs(), CollectDefaultOutputs(), timestampCache, DefaultSetName))
            {
                return false;
            }

            // Second, validate the relationships between inputs and outputs in specific sets, if any.
            foreach (string setName in state.SetNames)
            {
                if (log.Level >= LogLevel.Verbose)
                {
                    log.Verbose(nameof(Resources.FUTD_ComparingInputOutputTimestamps_1), setName);
                    log.Indent++;
                }

                if (!CheckInputsAndOutputs(CollectSetInputs(setName), CollectSetOutputs(setName), timestampCache, setName))
                {
                    return false;
                }

                if (log.Level >= LogLevel.Verbose)
                {
                    log.Indent--;
                }
            }

            // Validation passed
            return true;

            bool CheckInputsAndOutputs(IEnumerable<(string Path, string? ItemType, bool IsRequired)> inputs, IEnumerable<string> outputs, in TimestampCache timestampCache, string setName)
            {
                // We assume there are fewer outputs than inputs, so perform a full scan of outputs to find the earliest.
                // This increases the chance that we may return sooner in the case we are not up to date.
                DateTime earliestOutputTime = DateTime.MaxValue;
                string? earliestOutputPath = null;
                bool hasOutput = false;

                foreach (string output in outputs)
                {
                    token.ThrowIfCancellationRequested();

                    DateTime? outputTime = timestampCache.GetTimestampUtc(output);

                    if (outputTime is null)
                    {
                        return log.Fail("OutputNotFound", nameof(Resources.FUTD_OutputDoesNotExist_1), output);
                    }

                    if (outputTime < earliestOutputTime)
                    {
                        earliestOutputTime = outputTime.Value;
                        earliestOutputPath = output;
                    }

                    hasOutput = true;
                }

                if (!hasOutput)
                {
                    log.Info(setName == DefaultSetName ? nameof(Resources.FUTD_NoBuildOutputDefined) : nameof(Resources.FUTD_NoBuildOutputDefinedInSet_1), setName);

                    return true;
                }

                Assumes.NotNull(earliestOutputPath);

                (string Path, DateTime? Time)? latestInput = null;

                foreach ((string input, string? itemType, bool isRequired) in inputs)
                {
                    token.ThrowIfCancellationRequested();

                    DateTime? inputTime = timestampCache.GetTimestampUtc(input);

                    if (inputTime is null)
                    {
                        if (isRequired)
                        {
                            return log.Fail("InputNotFound", itemType is null ? nameof(Resources.FUTD_RequiredInputNotFound_1) : nameof(Resources.FUTD_RequiredTypedInputNotFound_2), input, itemType ?? "");
                        }
                        else
                        {
                            log.Verbose(itemType is null ? nameof(Resources.FUTD_NonRequiredInputNotFound_1) : nameof(Resources.FUTD_NonRequiredTypedInputNotFound_2), input, itemType ?? "");
                        }
                    }

                    if (inputTime > earliestOutputTime)
                    {
                        return log.Fail("InputNewerThanEarliestOutput", itemType is null ? nameof(Resources.FUTD_InputNewerThanOutput_4) : nameof(Resources.FUTD_TypedInputNewerThanOutput_5), input, inputTime.Value, earliestOutputPath, earliestOutputTime, itemType ?? "");
                    }

                    if (inputTime > lastSuccessfulBuildStartTimeUtc)
                    {
                        // Bypass this test if no check has yet been performed. We handle that in CheckGlobalConditions.
                        Assumes.NotNull(inputTime);
                        Assumes.NotNull(lastSuccessfulBuildStartTimeUtc);
                        return log.Fail("InputModifiedSinceLastSuccessfulBuildStart", itemType is null ? nameof(Resources.FUTD_InputModifiedSinceLastSuccessfulBuildStart_3) : nameof(Resources.FUTD_TypedInputModifiedSinceLastSuccessfulBuildStart_4), input, inputTime, lastSuccessfulBuildStartTimeUtc, itemType ?? "");
                    }

                    if (latestInput is null || inputTime > latestInput.Value.Time)
                    {
                        latestInput = (input, inputTime);
                    }
                }

                if (log.Level >= LogLevel.Info)
                {
                    if (latestInput is null)
                    {
                        log.Info(setName == DefaultSetName ? nameof(Resources.FUTD_NoInputsDefined) : nameof(Resources.FUTD_NoInputsDefinedInSet_1), setName);
                    }
                    else
                    {
                        log.Info(setName == DefaultSetName ? nameof(Resources.FUTD_NoInputsNewerThanEarliestOutput_4) : nameof(Resources.FUTD_NoInputsNewerThanEarliestOutputInSet_5), earliestOutputPath, earliestOutputTime, latestInput.Value.Path, latestInput.Value.Time ?? (object)"null", setName);
                    }
                }

                return true;
            }

            IEnumerable<(string Path, string? ItemType, bool IsRequired)> CollectDefaultInputs()
            {
                if (state.MSBuildProjectFullPath != null)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingProjectFileInputs));
                    log.Indent++;
                    log.VerboseLiteral(state.MSBuildProjectFullPath);
                    log.Indent--;
                    yield return (Path: state.MSBuildProjectFullPath, ItemType: null, IsRequired: true);
                }

                if (state.NewestImportInput != null)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingNewestImportInput));
                    log.Indent++;
                    log.VerboseLiteral(state.NewestImportInput);
                    log.Indent--;
                    yield return (Path: state.NewestImportInput, ItemType: null, IsRequired: true);
                }

                foreach ((string itemType, ImmutableArray<UpToDateCheckInputItem> items) in state.InputSourceItemsByItemType)
                {
                    // Skip certain input item types (None, Content). These items do not contribute to build outputs,
                    // and so changes to them are not expected to produce updated outputs during build.
                    //
                    // These items may have CopyToOutputDirectory metadata, which is why we don't exclude them earlier.
                    // The need to schedule a build in order to copy files is handled separately.
                    if (!NonCompilationItemTypes.Contains(itemType))
                    {
                        log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), itemType);
                        log.Indent++;

                        foreach (UpToDateCheckInputItem item in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(item.Path);
                            log.VerboseLiteral(absolutePath);
                            yield return (Path: absolutePath, itemType, IsRequired: true);
                        }

                        log.Indent--;
                    }
                }

                if (!state.ResolvedAnalyzerReferencePaths.IsEmpty)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), ResolvedAnalyzerReference.SchemaName);
                    log.Indent++;

                    foreach (string path in state.ResolvedAnalyzerReferencePaths)
                    {
                        string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                        log.VerboseLiteral(absolutePath);
                        yield return (Path: absolutePath, ItemType: ResolvedAnalyzerReference.SchemaName, IsRequired: true);
                    }

                    log.Indent--;
                }

                if (!state.ResolvedCompilationReferencePaths.IsEmpty)
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), ResolvedCompilationReference.SchemaName);
                    log.Indent++;

                    foreach (string path in state.ResolvedCompilationReferencePaths)
                    {
                        System.Diagnostics.Debug.Assert(Path.IsPathRooted(path), "ResolvedCompilationReference path should be rooted");
                        log.VerboseLiteral(path);
                        yield return (Path: path, ItemType: ResolvedCompilationReference.SchemaName, IsRequired: true);
                    }

                    log.Indent--;
                }

                if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputs_1), UpToDateCheckInput.SchemaName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckInputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return (Path: absolutePath, ItemType: UpToDateCheckInput.SchemaName, IsRequired: true);
                        }
                    }

                    log.Indent--;
                }
            }

            IEnumerable<string> CollectDefaultOutputs()
            {
                if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputs_1), UpToDateCheckOutput.SchemaName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckOutputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }

                if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputs_1), UpToDateCheckBuilt.SchemaName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckBuiltItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }
            }

            IEnumerable<(string Path, string? ItemType, bool IsRequired)> CollectSetInputs(string setName)
            {
                if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedInputsInSet_2), UpToDateCheckInput.SchemaName, setName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckInputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return (Path: absolutePath, ItemType: UpToDateCheckInput.SchemaName, IsRequired: true);
                        }
                    }

                    log.Indent--;
                }
            }

            IEnumerable<string> CollectSetOutputs(string setName)
            {
                if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputsInSet_2), UpToDateCheckOutput.SchemaName, setName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckOutputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }

                if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
                {
                    log.Verbose(nameof(Resources.FUTD_AddingTypedOutputsInSet_2), UpToDateCheckBuilt.SchemaName, setName);
                    log.Indent++;

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckBuiltItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.VerboseLiteral(absolutePath);
                            yield return absolutePath;
                        }
                    }

                    log.Indent--;
                }
            }

            bool ShouldIgnoreItems(string kind, ImmutableArray<string> items)
            {
                if (ignoreKinds?.Contains(kind) != true)
                {
                    return false;
                }

                if (log.Level >= LogLevel.Verbose)
                {
                    log.Indent++;
                   
                    foreach (string path in items)
                    {
                        string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                        log.Verbose(nameof(Resources.FUTD_SkippingIgnoredKindItem_2), absolutePath, kind);
                    }

                    log.Indent--;
                }

                return true;
            }
        }

        private bool CheckMarkers(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state)
        {
            // Reference assembly copy markers are strange. The property is always going to be present on
            // references to SDK-based projects, regardless of whether or not those referenced projects
            // will actually produce a marker. And an item always will be present in an SDK-based project,
            // regardless of whether or not the project produces a marker. So, basically, we only check
            // here if the project actually produced a marker and we only check it against references that
            // actually produced a marker.

            if (Strings.IsNullOrWhiteSpace(state.CopyUpToDateMarkerItem) || state.CopyReferenceInputs.IsEmpty)
            {
                return true;
            }

            string outputMarkerFile = _configuredProject.UnconfiguredProject.MakeRooted(state.CopyUpToDateMarkerItem);

            DateTime? outputMarkerTime = timestampCache.GetTimestampUtc(outputMarkerFile);

            if (outputMarkerTime is null)
            {
                log.Info(nameof(Resources.FUTD_NoOutputMarkerExists_1), outputMarkerFile);
                return true;
            }

            log.Info(nameof(Resources.FUTD_WriteTimeOnOutputMarker_2), outputMarkerTime, outputMarkerFile);

            log.Verbose(nameof(Resources.FUTD_AddingInputReferenceCopyMarkers));

            bool inputMarkerExists = false;

            foreach (string inputMarker in state.CopyReferenceInputs)
            {
                log.Indent++;
                log.VerboseLiteral(inputMarker);
                log.Indent--;

                DateTime? inputMarkerTime = timestampCache.GetTimestampUtc(inputMarker);

                if (inputMarkerTime is not null)
                {
                    inputMarkerExists = true;

                    if (outputMarkerTime < inputMarkerTime)
                    {
                        return log.Fail("InputMarkerNewerThanOutputMarker", nameof(Resources.FUTD_InputMarkerNewerThanOutputMarker_4), inputMarker, inputMarkerTime, outputMarkerFile, outputMarkerTime);
                    }
                }
            }

            if (!inputMarkerExists)
            {
                log.Info(nameof(Resources.FUTD_NoInputMarkersExist));
            }

            return true;
        }

        private bool CheckCopiedOutputFiles(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, CancellationToken token)
        {
            foreach ((string destinationRelative, string sourceRelative) in state.CopiedOutputFiles)
            {
                token.ThrowIfCancellationRequested();

                string source = _configuredProject.UnconfiguredProject.MakeRooted(sourceRelative);
                string destination = _configuredProject.UnconfiguredProject.MakeRooted(destinationRelative);

                log.Info(nameof(Resources.FUTD_CheckingCopiedOutputFile), source);

                DateTime? sourceTime = timestampCache.GetTimestampUtc(source);

                if (sourceTime != null)
                {
                    log.Indent++;
                    log.Info(nameof(Resources.FUTD_SourceFileTimeAndPath_2), sourceTime, source);
                    log.Indent--;
                }
                else
                {
                    return log.Fail("CopySourceNotFound", nameof(Resources.FUTD_CheckingCopiedOutputFileSourceNotFound_2), source, destination);
                }

                DateTime? destinationTime = timestampCache.GetTimestampUtc(destination);

                if (destinationTime != null)
                {
                    log.Indent++;
                    log.Info(nameof(Resources.FUTD_DestinationFileTimeAndPath_2), destinationTime, destination);
                    log.Indent--;
                }
                else
                {
                    return log.Fail("CopyDestinationNotFound", nameof(Resources.FUTD_CheckingCopiedOutputFileDestinationNotFound_2), destination, source);
                }

                if (destinationTime < sourceTime)
                {
                    return log.Fail("CopySourceNewer", nameof(Resources.FUTD_CheckingCopiedOutputFileSourceNewer));
                }
            }

            return true;
        }

        private bool CheckCopyToOutputDirectoryFiles(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, CancellationToken token)
        {
            string outputFullPath = Path.Combine(state.MSBuildProjectDirectory, state.OutputRelativeOrFullPath);

            foreach ((string itemType, ImmutableArray<UpToDateCheckInputItem> items) in state.InputSourceItemsByItemType)
            {
                foreach (UpToDateCheckInputItem item in items)
                {
                    token.ThrowIfCancellationRequested();

                    if (item.CopyType == CopyType.Never)
                    {
                        // Ignore items which are never copied. Only process Always and PreserveNewest items.
                        //
                        // Note that if we see Always items, then state.IsTreatCopyAlwaysAsPreserveNewestDisabled must
                        // be false, as when it is true and any Always item exists, the check returns before this point.

                        continue;
                    }

                    System.Diagnostics.Debug.Assert(item.CopyType != CopyType.Always || !state.IsCopyAlwaysOptimizationDisabled);

                    string sourcePath = _configuredProject.UnconfiguredProject.MakeRooted(item.Path);
                    string filename = Strings.IsNullOrEmpty(item.TargetPath) ? sourcePath : item.TargetPath;

                    if (string.IsNullOrEmpty(filename))
                    {
                        continue;
                    }

                    filename = _configuredProject.UnconfiguredProject.MakeRelative(filename);

                    log.Info(nameof(Resources.FUTD_CheckingCopyToOutputDirectoryItem_3), itemType, item.CopyType.ToString(), sourcePath);
                    log.Indent++;

                    DateTime? sourceTime = timestampCache.GetTimestampUtc(sourcePath);

                    if (sourceTime != null)
                    {
                        log.Info(nameof(Resources.FUTD_SourceFileTimeAndPath_2), sourceTime, sourcePath);
                    }
                    else
                    {
                        return log.Fail("CopyToOutputDirectorySourceNotFound", nameof(Resources.FUTD_CheckingCopyToOutputDirectorySourceNotFound_1), sourcePath);
                    }

                    string destinationPath = Path.Combine(outputFullPath, filename);
                    DateTime? destinationTime = timestampCache.GetTimestampUtc(destinationPath);

                    if (destinationTime != null)
                    {
                        log.Info(nameof(Resources.FUTD_DestinationFileTimeAndPath_2), destinationTime, destinationPath);
                    }
                    else
                    {
                        return log.Fail("CopyToOutputDirectoryDestinationNotFound", nameof(Resources.FUTD_CheckingCopyToOutputDirectoryItemDestinationNotFound_1), destinationPath);
                    }

                    if (item.CopyType == CopyType.PreserveNewest)
                    {
                        if (destinationTime < sourceTime)
                        {
                            return log.Fail("CopyToOutputDirectorySourceNewer", nameof(Resources.FUTD_CheckingCopyToOutputDirectorySourceNewerThanDestination_4), itemType, item.CopyType.ToString(), sourcePath, destinationPath);
                        }
                    }
                    else if (item.CopyType == CopyType.Always)
                    {
                        log.Info(nameof(Resources.FUTD_OptimizingCopyAlwaysItem));

                        // We have already validated the presence of these files, so we don't expect these to return
                        // false. If one of them does, the corresponding size would be zero, so we would schedule a build.
                        // The odds of both source and destination disappearing between the gathering of the timestamps
                        // above and these following statements is vanishingly small, and would suggest bigger problems
                        // such as the entire project directory having been deleted.
                        _fileSystem.TryGetFileSizeBytes(sourcePath, out long sourceSizeBytes);
                        _fileSystem.TryGetFileSizeBytes(destinationPath, out long destinationSizeBytes);

                        if (sourceTime != destinationTime || sourceSizeBytes != destinationSizeBytes)
                        {
                            return log.Fail("CopyAlwaysItemDiffers", nameof(Resources.FUTD_CopyAlwaysItemsDiffer_7), itemType, sourcePath, sourceTime, sourceSizeBytes, destinationPath, destinationTime, destinationSizeBytes);
                        }
                    }

                    log.Indent--;
                }
            }

            return true;
        }

        void IBuildUpToDateCheckProviderInternal.NotifyBuildStarting(DateTime buildStartTimeUtc)
        {
            _lastBuildStartTimeUtc = buildStartTimeUtc;
        }

        async Task IBuildUpToDateCheckProviderInternal.NotifyBuildCompletedAsync(bool wasSuccessful, bool isRebuild)
        {
            if (_lastBuildStartTimeUtc == default)
            {
                // This should not happen
                System.Diagnostics.Debug.Fail("Notification of build completion should follow notification of build starting.");

                return;
            }

            if (wasSuccessful)
            {
                ISubscription subscription = Volatile.Read(ref _subscription);

                await subscription.UpdateLastSuccessfulBuildStartTimeUtcAsync(_lastBuildStartTimeUtc, isRebuild);
            }

            _lastBuildStartTimeUtc = default;
        }

        private static bool ConfiguredInputMatchesTargetFramework(UpToDateCheckImplicitConfiguredInput input, string buildTargetFramework)
        {
            return input.ProjectConfiguration.Dimensions.TryGetValue(ConfigurationGeneral.TargetFrameworkProperty, out string? configurationTargetFramework)
                && buildTargetFramework.Equals(configurationTargetFramework);
        }

        Task<bool> IBuildUpToDateCheckProvider.IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken)
        {
            return IsUpToDateAsync(buildAction, logWriter, ImmutableDictionary<string, string>.Empty, cancellationToken);
        }

        async Task<(bool IsUpToDate, string? FailureReason)> IBuildUpToDateCheckValidator.ValidateUpToDateAsync(CancellationToken cancellationToken)
        {
            bool isUpToDate = await IsUpToDateInternalAsync(TextWriter.Null, _lastGlobalProperties, isValidationRun: true, cancellationToken);

            string failureReason = isUpToDate ? "" : _lastFailureReason;

            return (isUpToDate, failureReason);
        }

        public Task<bool> IsUpToDateAsync(
            BuildAction buildAction,
            TextWriter logWriter,
            IImmutableDictionary<string, string> globalProperties,
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                throw new ObjectDisposedException(nameof(BuildUpToDateCheck));
            }

            if (buildAction != BuildAction.Build)
            {
                return TaskResult.False;
            }

            // Cache the last-used set of global properties. We may be asked to validate this up-to-date check
            // once the build has completed (in ValidateUpToDateAsync), and will re-use the same set of global
            // properties to ensure parity.
            _lastGlobalProperties = globalProperties;

            return IsUpToDateInternalAsync(logWriter, globalProperties, isValidationRun: false, cancellationToken);
        }

        private async Task<bool> IsUpToDateInternalAsync(
            TextWriter logWriter,
            IImmutableDictionary<string, string> globalProperties,
            bool isValidationRun,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Start the stopwatch now, so we include any lock acquisition in the timing
            var sw = Stopwatch.StartNew();

            ISubscription subscription = Volatile.Read(ref _subscription);

            return await subscription.RunAsync(CheckAsync, cancellationToken);

            async Task<(bool, ImmutableArray<ProjectConfiguration>)> CheckAsync(UpToDateCheckConfiguredInput state, IUpToDateCheckStatePersistence persistence, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // Short-lived cache of timestamp by path
                var timestampCache = new TimestampCache(_fileSystem);

                globalProperties.TryGetValue(FastUpToDateCheckIgnoresKindsGlobalPropertyName, out string? ignoreKindsString);

                (LogLevel requestedLogLevel, Guid projectGuid) = await (
                    _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(token),
                    _guidService.GetProjectGuidAsync(token));

                var logger = new Log(
                    logWriter,
                    requestedLogLevel,
                    sw,
                    timestampCache,
                    _configuredProject.UnconfiguredProject.FullPath ?? "",
                    projectGuid,
                    isValidationRun ? null : _telemetryService,
                    state,
                    ignoreKindsString,
                    isValidationRun ? -1 : Interlocked.Increment(ref _checkNumber));

                try
                {
                    HashSet<string>? ignoreKinds = null;
                    if (ignoreKindsString is not null)
                    {
                        ignoreKinds = new HashSet<string>(new LazyStringSplit(ignoreKindsString, ';'), StringComparer.OrdinalIgnoreCase);

                        if (requestedLogLevel >= LogLevel.Info && ignoreKinds.Count != 0)
                        {
                            logger.Info(nameof(Resources.FUTD_IgnoringKinds_1), ignoreKindsString);
                        }
                    }

                    // If we're limiting the build to a particular target framework, limit the set of
                    // configured inputs we check to those that match the framework.
                    globalProperties.TryGetValue(TargetFrameworkGlobalPropertyName, out string? buildTargetFramework);
                    IEnumerable<UpToDateCheckImplicitConfiguredInput> implicitStatesToCheck = Strings.IsNullOrEmpty(buildTargetFramework)
                        ? state.ImplicitInputs
                        : state.ImplicitInputs.Where(input => ConfiguredInputMatchesTargetFramework(input, buildTargetFramework));

                    // Note that if we find a particular configuration is out of date and exit early,
                    // all the configurations we're going to build still count as checked.
                    ImmutableArray<ProjectConfiguration> checkedConfigurations = implicitStatesToCheck.Select(state => state.ProjectConfiguration).ToImmutableArray();

                    bool logConfigurations = state.ImplicitInputs.Length > 1 && logger.Level >= LogLevel.Info;

                    foreach (UpToDateCheckImplicitConfiguredInput implicitState in implicitStatesToCheck)
                    {
                        if (logConfigurations)
                        {
                            logger.Info(nameof(Resources.FUTD_CheckingConfiguration_1), implicitState.ProjectConfiguration.GetDisplayString());
                            logger.Indent++;
                        }

                        string? path = _configuredProject.UnconfiguredProject.FullPath;

                        DateTime? lastSuccessfulBuildStartTimeUtc = path is null
                            ? null
                            : await persistence.RestoreLastSuccessfulBuildStateAsync(
                                path,
                                implicitState.ProjectConfiguration.Dimensions,
                                CancellationToken.None);

                        if (!CheckGlobalConditions(logger, lastSuccessfulBuildStartTimeUtc, validateFirstRun: !isValidationRun, implicitState)
                            || !CheckInputsAndOutputs(logger, lastSuccessfulBuildStartTimeUtc, timestampCache, implicitState, ignoreKinds, token)
                            || !CheckMarkers(logger, timestampCache, implicitState)
                            || !CheckCopyToOutputDirectoryFiles(logger, timestampCache, implicitState, token)
                            || !CheckCopiedOutputFiles(logger, timestampCache, implicitState, token))
                        {
                            return (false, checkedConfigurations);
                        }
                        
                        if (logConfigurations)
                        {
                            logger.Indent--;
                        }
                    }

                    logger.UpToDate();
                    return (true, checkedConfigurations);
                }
                catch (Exception ex)
                {
                    return (logger.Fail("Exception", nameof(Resources.FUTD_Exception_1), ex), ImmutableArray<ProjectConfiguration>.Empty);
                }
                finally
                {
                    logger.Verbose(nameof(Resources.FUTD_Completed), sw.Elapsed.TotalMilliseconds);

                    _lastFailureReason = logger.FailureReason ?? "";
                }
            }
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
        {
            return _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync(cancellationToken);
        }

        internal readonly struct TestAccessor
        {
            private readonly BuildUpToDateCheck _check;

            public TestAccessor(BuildUpToDateCheck check) => _check = check;

            public void SetSubscription(ISubscription subscription) => _check._subscription = subscription;
        }

        /// <summary>For unit testing only.</summary>
#pragma warning disable RS0043 // Do not call 'GetTestAccessor()'
        internal TestAccessor TestAccess => new(this);
#pragma warning restore RS0043
    }
}
