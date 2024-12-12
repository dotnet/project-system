﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

[AppliesTo(AppliesToExpression)]
[Export(typeof(IBuildUpToDateCheckProvider))]
[Export(typeof(IBuildUpToDateCheckValidator))]
[Export(typeof(IProjectBuildEventListener))]
[Export(typeof(IActiveConfigurationComponent))]
[ExportMetadata("BeforeDrainCriticalTasks", true)]
internal sealed partial class BuildUpToDateCheck
    : IBuildUpToDateCheckProvider2,
      IBuildUpToDateCheckValidator,
      IProjectBuildEventListener,
      IActiveConfigurationComponent,
      IDisposable
{
    // TODO: Remove the dependency on RunningInVisualStudio when IBuildUpToDateCheckProvider2 is supported in the project system server.
    internal const string AppliesToExpression = $"{ProjectCapability.DotNet} + !{ProjectCapabilities.SharedAssetsProject} + {ProjectCapabilities.RunningInVisualStudio}";

    internal const string FastUpToDateCheckIgnoresKindsGlobalPropertyName = "FastUpToDateCheckIgnoresKinds";
    internal const string TargetFrameworkGlobalPropertyName = "TargetFramework";

    // This analyzer fires for comparisons against the following constants. Disable it in this file.
    #pragma warning disable CA1820 // Test for empty strings using string length

    internal const string DefaultSetName = "";
    internal const string DefaultKindName = "";

    internal static readonly StringComparer SetNameComparer = StringComparers.ItemNames;
    internal static readonly StringComparer KindNameComparer = StringComparers.ItemNames;

    private readonly ISolutionBuildContextProvider _solutionBuildContextProvider;
    private readonly ISolutionBuildEventListener _solutionBuildEventListener;
    private readonly IUpToDateCheckConfiguredInputDataSource _inputDataSource;
    private readonly IProjectSystemOptions _projectSystemOptions;
    private readonly ConfiguredProject _configuredProject;
    private readonly IUpToDateCheckStatePersistence _persistence;
    private readonly IProjectAsynchronousTasksService _tasksService;
    private readonly ITelemetryService _telemetryService;
    private readonly IFileSystem _fileSystem;
    private readonly ISafeProjectGuidService _guidService;
    private readonly IUpToDateCheckHost _upToDateCheckHost;
    private readonly ICopyItemAggregator _copyItemAggregator;

    private IImmutableDictionary<string, string> _lastGlobalProperties = ImmutableStringDictionary<string>.EmptyOrdinal;
    private string? _lastFailureReason;
    private string? _lastFailureDescription;
    private DateTime _lastBuildStartTimeUtc = DateTime.MinValue;

    private ISubscription _subscription;
    private int _isDisposed;
    private int _checkNumber;
    private IEnumerable<string>? _lastCopyTargetsFromThisProject;

    /// <summary>
    /// Gets the set of up-to-date checkers that apply to this project.
    /// </summary>
    /// <remarks>
    /// We can use this information in log output if multiple up-to-date checks are present, so that users can
    /// observe that even if we find the project up-to-date, another check may not, which will lead to the
    /// project being built. If other checkers exist, we log that fact and include their type names, which
    /// can avoid confusion and assist with further debugging.
    /// </remarks>
    [ImportMany]
    internal OrderPrecedenceImportCollection<IBuildUpToDateCheckProvider> UpToDateCheckers { get; }

    [ImportingConstructor]
    public BuildUpToDateCheck(
        ISolutionBuildContextProvider solutionBuildContextProvider,
        ISolutionBuildEventListener solutionBuildEventListener,
        IUpToDateCheckConfiguredInputDataSource inputDataSource,
        IProjectSystemOptions projectSystemOptions,
        ConfiguredProject configuredProject,
        IUpToDateCheckStatePersistence persistence,
        [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
        ITelemetryService telemetryService,
        IFileSystem fileSystem,
        ISafeProjectGuidService guidService,
        IUpToDateCheckHost upToDateCheckHost,
        ICopyItemAggregator copyItemAggregator)
    {
        _solutionBuildContextProvider = solutionBuildContextProvider;
        _solutionBuildEventListener = solutionBuildEventListener;
        _inputDataSource = inputDataSource;
        _projectSystemOptions = projectSystemOptions;
        _configuredProject = configuredProject;
        _persistence = persistence;
        _tasksService = tasksService;
        _telemetryService = telemetryService;
        _fileSystem = fileSystem;
        _guidService = guidService;
        _upToDateCheckHost = upToDateCheckHost;
        _copyItemAggregator = copyItemAggregator;

        UpToDateCheckers = new OrderPrecedenceImportCollection<IBuildUpToDateCheckProvider>(projectCapabilityCheckProvider: configuredProject);

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
            return log.Fail("CriticalTasks", nameof(VSResources.FUTD_CriticalBuildTasksRunning));
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
            return log.Fail("FirstRun", nameof(VSResources.FUTD_FirstRun));
        }

        if (lastSuccessfulBuildStartTimeUtc < state.LastItemsChangedAtUtc)
        {
            Assumes.NotNull(lastSuccessfulBuildStartTimeUtc);
            Assumes.NotNull(state.LastItemsChangedAtUtc);

            log.Fail("ProjectItemsChangedSinceLastSuccessfulBuildStart", nameof(VSResources.FUTD_SetOfItemsChangedMoreRecentlyThanOutput_2), state.LastItemsChangedAtUtc, lastSuccessfulBuildStartTimeUtc);

            if (log.Level >= LogLevel.Info)
            {
                using Log.Scope _ = log.IndentScope();

                if (state.LastItemChanges.Length == 0)
                {
                    log.Info(nameof(VSResources.FUTD_SetOfChangedItemsIsEmpty));
                }
                else
                {
                    foreach ((bool isAdd, string itemType, string item) in state.LastItemChanges.OrderBy(change => change.ItemType).ThenBy(change => change.Item))
                    {
                        log.Info(isAdd ? nameof(VSResources.FUTD_ChangedItemsAddition_2) : nameof(VSResources.FUTD_ChangedItemsRemoval_2), itemType, item);
                    }
                }
            }

            return false;
        }

        return true;
    }

    private bool CheckInputsAndOutputs(Log log, DateTime? lastSuccessfulBuildStartTimeUtc, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, HashSet<string>? ignoreKinds, CancellationToken token)
    {
        // UpToDateCheckInput/Output/Built items have optional 'Set' metadata that determine whether they
        // are treated separately or not. If omitted, such inputs/outputs are included in the default set,
        // which also includes other items such as project files, compilation items, analyzer references, etc.

        log.Info(nameof(VSResources.FUTD_ComparingInputOutputTimestamps));

        using (log.IndentScope())
        {
            // First, validate the relationship between inputs and outputs within the default set.
            if (!CheckInputsAndOutputs(CollectDefaultInputs(), CollectDefaultOutputs(), timestampCache, DefaultSetName))
            {
                return false;
            }
        }

        // Second, validate the relationships between inputs and outputs in specific sets, if any.
        foreach (string setName in state.SetNames)
        {
            if (log.Level >= LogLevel.Verbose)
            {
                log.Verbose(nameof(VSResources.FUTD_ComparingInputOutputTimestampsInSet_1), setName);
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
                System.Diagnostics.Debug.Assert(Path.IsPathRooted(output), "Output path must be rooted", output);

                token.ThrowIfCancellationRequested();

                log.VerboseLiteral(output);

                DateTime? outputTime = timestampCache.GetTimestampUtc(output);

                if (outputTime is null)
                {
                    return log.Fail("OutputNotFound", nameof(VSResources.FUTD_OutputDoesNotExist_1), output);
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
                log.Info(setName == DefaultSetName ? nameof(VSResources.FUTD_NoBuildOutputDefined) : nameof(VSResources.FUTD_NoBuildOutputDefinedInSet_1), setName);

                return true;
            }

            Assumes.NotNull(earliestOutputPath);

            (string Path, DateTime? Time)? latestInput = null;

            foreach ((string input, string? itemType, bool isRequired) in inputs)
            {
                System.Diagnostics.Debug.Assert(Path.IsPathRooted(input), "Output path must be rooted", input);

                token.ThrowIfCancellationRequested();

                log.VerboseLiteral(input);

                DateTime? inputTime = timestampCache.GetTimestampUtc(input);

                if (inputTime is null)
                {
                    if (isRequired)
                    {
                        return log.Fail("InputNotFound", itemType is null ? nameof(VSResources.FUTD_RequiredInputNotFound_1) : nameof(VSResources.FUTD_RequiredTypedInputNotFound_2), input, itemType ?? "");
                    }
                    else
                    {
                        log.Verbose(itemType is null ? nameof(VSResources.FUTD_NonRequiredInputNotFound_1) : nameof(VSResources.FUTD_NonRequiredTypedInputNotFound_2), input, itemType ?? "");
                    }
                }

                if (inputTime > earliestOutputTime)
                {
                    return log.Fail("InputNewerThanEarliestOutput", itemType is null ? nameof(VSResources.FUTD_InputNewerThanOutput_4) : nameof(VSResources.FUTD_TypedInputNewerThanOutput_5), input, inputTime.Value, earliestOutputPath, earliestOutputTime, itemType ?? "");
                }

                if (inputTime > lastSuccessfulBuildStartTimeUtc)
                {
                    // Bypass this test if no check has yet been performed. We handle that in CheckGlobalConditions.
                    Assumes.NotNull(inputTime);
                    Assumes.NotNull(lastSuccessfulBuildStartTimeUtc);
                    return log.Fail("InputModifiedSinceLastSuccessfulBuildStart", itemType is null ? nameof(VSResources.FUTD_InputModifiedSinceLastSuccessfulBuildStart_3) : nameof(VSResources.FUTD_TypedInputModifiedSinceLastSuccessfulBuildStart_4), input, inputTime, lastSuccessfulBuildStartTimeUtc, itemType ?? "");
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
                    log.Info(setName == DefaultSetName ? nameof(VSResources.FUTD_NoInputsDefined) : nameof(VSResources.FUTD_NoInputsDefinedInSet_1), setName);
                }
                else
                {
                    log.Info(setName == DefaultSetName ? nameof(VSResources.FUTD_NoInputsNewerThanEarliestOutput_4) : nameof(VSResources.FUTD_NoInputsNewerThanEarliestOutputInSet_5), earliestOutputPath, earliestOutputTime, latestInput.Value.Path, latestInput.Value.Time ?? (object)"null", setName);
                }
            }

            return true;
        }

        IEnumerable<(string Path, string? ItemType, bool IsRequired)> CollectDefaultInputs()
        {
            if (state.NewestImportInput is not null)
            {
                log.Verbose(nameof(VSResources.FUTD_AddingNewestImportInput));
                using Log.Scope _ = log.IndentScope();
                yield return (Path: state.NewestImportInput, ItemType: null, IsRequired: true);
            }

            foreach ((string itemType, ImmutableArray<string> items) in state.InputSourceItemsByItemType)
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedInputs_1), itemType);

                using Log.Scope _ = log.IndentScope();

                foreach (string item in items)
                {
                    string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(item);
                    yield return (Path: absolutePath, itemType, IsRequired: true);
                }
            }

            if (!state.ResolvedAnalyzerReferencePaths.IsEmpty)
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedInputs_1), ResolvedAnalyzerReference.SchemaName);

                using Log.Scope _ = log.IndentScope();

                foreach (string path in state.ResolvedAnalyzerReferencePaths)
                {
                    string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                    yield return (Path: absolutePath, ItemType: ResolvedAnalyzerReference.SchemaName, IsRequired: true);
                }
            }

            if (!state.ResolvedCompilationReferencePaths.IsEmpty)
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedInputs_1), ResolvedCompilationReference.SchemaName);

                using Log.Scope _ = log.IndentScope();

                foreach (string path in state.ResolvedCompilationReferencePaths)
                {
                    System.Diagnostics.Debug.Assert(Path.IsPathRooted(path), "ResolvedCompilationReference path should be rooted");
                    yield return (Path: path, ItemType: ResolvedCompilationReference.SchemaName, IsRequired: true);
                }
            }

            if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedInputs_1), UpToDateCheckInput.SchemaName);

                using Log.Scope _ = log.IndentScope();

                foreach (string kind in state.KindNames)
                {
                    if (upToDateCheckInputItems.TryGetValue(kind, out ImmutableArray<string> items))
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            yield return (Path: absolutePath, ItemType: UpToDateCheckInput.SchemaName, IsRequired: true);
                        }
                    }
                }
            }
        }

        IEnumerable<string> CollectDefaultOutputs()
        {
            if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedOutputs_1), UpToDateCheckOutput.SchemaName);

                using Log.Scope _ = log.IndentScope();

                foreach (string kind in state.KindNames)
                {
                    if (upToDateCheckOutputItems.TryGetValue(kind, out ImmutableArray<string> items))
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            yield return _configuredProject.UnconfiguredProject.MakeRooted(path);
                        }
                    }
                }
            }

            if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedOutputs_1), UpToDateCheckBuilt.SchemaName);

                using Log.Scope _ = log.IndentScope();

                foreach (string kind in state.KindNames)
                {
                    if (upToDateCheckBuiltItems.TryGetValue(kind, out ImmutableArray<string> items))
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            yield return _configuredProject.UnconfiguredProject.MakeRooted(path);
                        }
                    }
                }
            }
        }

        IEnumerable<(string Path, string? ItemType, bool IsRequired)> CollectSetInputs(string setName)
        {
            if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedInputsInSet_2), UpToDateCheckInput.SchemaName, setName);

                using Log.Scope _ = log.IndentScope();

                foreach (string kind in state.KindNames)
                {
                    if (upToDateCheckInputItems.TryGetValue(kind, out ImmutableArray<string> items))
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            yield return (Path: absolutePath, ItemType: UpToDateCheckInput.SchemaName, IsRequired: true);
                        }
                    }
                }
            }
        }

        IEnumerable<string> CollectSetOutputs(string setName)
        {
            if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedOutputsInSet_2), UpToDateCheckOutput.SchemaName, setName);

                using Log.Scope _ = log.IndentScope();

                foreach (string kind in state.KindNames)
                {
                    if (upToDateCheckOutputItems.TryGetValue(kind, out ImmutableArray<string> items))
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            yield return _configuredProject.UnconfiguredProject.MakeRooted(path);
                        }
                    }
                }
            }

            if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
            {
                log.Verbose(nameof(VSResources.FUTD_AddingTypedOutputsInSet_2), UpToDateCheckBuilt.SchemaName, setName);

                using Log.Scope _ = log.IndentScope();

                foreach (string kind in state.KindNames)
                {
                    if (upToDateCheckBuiltItems.TryGetValue(kind, out ImmutableArray<string> items))
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            yield return _configuredProject.UnconfiguredProject.MakeRooted(path);
                        }
                    }
                }
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
                foreach (string path in items)
                {
                    string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                    log.Verbose(nameof(VSResources.FUTD_SkippingIgnoredKindItem_2), absolutePath, kind);
                }
            }

            return true;
        }
    }

    private bool CheckMarkers(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, bool? isBuildAccelerationEnabled, FileSystemOperationAggregator fileSystemOperations)
    {
        if (isBuildAccelerationEnabled is true)
        {
            // Build acceleration replaces the need to check the copy marker. We will be checking the items reported
            // by each project directly on disk, and will catch specific instances where they are out of date.
            return true;
        }

        // Copy markers support the use of reference assemblies, which in turn help avoid redundant compilation.
        //
        // Building a project that produces reference assemblies may update both:
        //
        // 1. The implementation assembly (bin/Debug/MyApp.dll), used at runtime, containing the full implementation.
        // 2. The reference assembly (obj/Debug/ref/MyApp.dll), used at compile time, containing only public API.
        //
        // The reference assembly is only modified when the public API surface of the implementation assembly changes.
        // A referencing project can use this information to avoid recompiling itself in response to the referenced
        // project change. In such cases, the implementation assembly can be simply be copied.
        //
        // A big part of the build acceleration feature is being able to have VS copy the implementation assembly
        // and report the project as up-to-date. It is much cheaper for VS to just copy the file than to call MSBuild
        // to do the same.
        //
        // When determining whether to build a referencing project, we examine the timestamps of:
        //
        // 1. `state.CopyUpToDateMarkerItem`
        //
        //    - Example: `MyApp/obj/Debug/net7.0/MyApp.csproj.CopyComplete`.
        //    - From `CopyUpToDateMarker` MSBuild item. Requires a single instance of this item.
        //    - Always present for SDK-based projects, regardless of whether `ProduceReferenceAssembly` is true.
        //
        // 2. `state.CopyReferenceInputs`
        //
        //    - Example:
        //      - `MyLibrary/bin/Debug/net6.0/MyLibrary.dll`
        //      - `MyLibrary/obj/Debug/net6.0/MyLibrary.csproj.CopyComplete`
        //    - From the `ResolvedPath` and `CopyUpToDateMarker` metadata on `ResolvedCompilationReference` items.
        //
        // If either are empty, there is no check to perform and we return immediately. This path would be taken for
        // projects having `ProduceReferenceAssembly` set to false.
        //
        // During build, if a project has references marked as "CopyLocal" (such dependencies, PDBs, XMLs, satellite assemblies)
        // that are copied to the output directory, the build touches its own CopyMarker item. Referencing projects
        // consider this CopyMarker as an input, which allows the copy to trigger builds in those referencing projects.
        //
        // When the project build copies a file into its output directory, it touches its own `.csproj.CopyComplete`
        // file. This gives future builds a timestamp to compare against.
        //
        // If a project does not have any project references, it will not produce a `.csproj.CopyComplete` file.
        //
        // When values exist, they will resemble:
        //
        //     Comparing timestamps of copy marker inputs and output:
        //         Write timestamp on output marker is 2022-11-10 13:42:01.665 on 'C:/Users/drnoakes/source/repos/MyApp/MyApp/obj/Debug/net7.0/MyApp.csproj.CopyComplete'.
        //         Adding input reference copy markers:
        //             C:/Users/drnoakes/source/repos/MyApp/MyLibrary/bin/Debug/net6.0/MyLibrary.dll
        //             C:/Users/drnoakes/source/repos/MyApp/MyLibrary/obj/Debug/net6.0/MyLibrary.csproj.CopyComplete
        //                 Input marker does not exist.
        //
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

        log.Info(nameof(VSResources.FUTD_ComparingCopyMarkerTimestamps));

        using Log.Scope _ = log.IndentScope();

        string outputMarkerFile = _configuredProject.UnconfiguredProject.MakeRooted(state.CopyUpToDateMarkerItem);

        DateTime? outputMarkerTime = timestampCache.GetTimestampUtc(outputMarkerFile);

        if (outputMarkerTime is null)
        {
            // No output marker exists, so we can't be out of date.
            log.Info(nameof(VSResources.FUTD_NoOutputMarkerExists_1), outputMarkerFile);
            return true;
        }

        log.Info(nameof(VSResources.FUTD_WriteTimeOnOutputMarker_2), outputMarkerTime, outputMarkerFile);

        log.Verbose(nameof(VSResources.FUTD_AddingInputReferenceCopyMarkers));

        bool inputMarkerExists = false;

        using (log.IndentScope())
        {
            foreach (string inputMarker in state.CopyReferenceInputs)
            {
                log.VerboseLiteral(inputMarker);

                DateTime? inputMarkerTime = timestampCache.GetTimestampUtc(inputMarker);

                if (inputMarkerTime is null)
                {
                    using (log.IndentScope())
                    {
                        log.Verbose(nameof(VSResources.FUTD_InputMarkerDoesNotExist));
                        continue;
                    }
                }

                inputMarkerExists = true;

                // See if input marker is newer than output marker
                if (outputMarkerTime < inputMarkerTime)
                {
                    fileSystemOperations.IsAccelerationCandidate = true;

                    return log.Fail("InputMarkerNewerThanOutputMarker", nameof(VSResources.FUTD_InputMarkerNewerThanOutputMarker_4), inputMarker, inputMarkerTime, outputMarkerFile, outputMarkerTime);
                }
            }
        }

        if (!inputMarkerExists)
        {
            log.Info(nameof(VSResources.FUTD_NoInputMarkersExist));
        }

        return true;
    }

    private bool CheckBuiltFromInputFiles(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, CancellationToken token)
    {
        // Note we cannot accelerate builds by copying these items. The input is potentially transformed
        // in some way, during build, to produce the output. It's not always a straight copy. We have to
        // call ths build to satisfy these items, unlike our CopyToOutputDirectory items.

        foreach ((string destinationRelative, string sourceRelative) in state.BuiltFromInputFileItems)
        {
            token.ThrowIfCancellationRequested();

            string sourcePath = _configuredProject.UnconfiguredProject.MakeRooted(sourceRelative);
            string destinationPath = _configuredProject.UnconfiguredProject.MakeRooted(destinationRelative);

            log.Info(nameof(VSResources.FUTD_CheckingBuiltOutputFile), sourcePath);

            using Log.Scope _ = log.IndentScope();

            DateTime? sourceTime = timestampCache.GetTimestampUtc(sourcePath);

            if (sourceTime is null)
            {
                // We don't generally expect the source to be unavailable.
                // If this occurs, schedule a build to be on the safe side.
                return log.Fail("CopySourceNotFound", nameof(VSResources.FUTD_CheckingBuiltOutputFileSourceNotFound_2), sourcePath, destinationPath);
            }

            log.Info(nameof(VSResources.FUTD_SourceFileTimeAndPath_2), sourceTime, sourcePath);

            DateTime? destinationTime = timestampCache.GetTimestampUtc(destinationPath);

            if (destinationTime is null)
            {
                return log.Fail("CopyDestinationNotFound", nameof(VSResources.FUTD_CheckingBuiltOutputFileDestinationNotFound_2), destinationPath, sourcePath);
            }

            log.Info(nameof(VSResources.FUTD_DestinationFileTimeAndPath_2), destinationTime, destinationPath);

            if (destinationTime < sourceTime)
            {
                return log.Fail("CopySourceNewer", nameof(VSResources.FUTD_CheckingBuiltOutputFileSourceNewer));
            }
        }

        return true;
    }

    private bool CheckCopyToOutputDirectoryItems(Log log, UpToDateCheckImplicitConfiguredInput state, IEnumerable<(string Path, ImmutableArray<CopyItem> CopyItems)> copyItemsByProject, ConfiguredFileSystemOperationAggregator fileSystemAggregator, bool? isBuildAccelerationEnabled, SolutionBuildContext solutionBuildContext, CancellationToken token)
    {
        ITimestampCache timestampCache = solutionBuildContext.CopyItemTimestamps;

        string outputFullPath = Path.Combine(state.MSBuildProjectDirectory, state.OutputRelativeOrFullPath);

        Log.Scope? scope1 = null;

        foreach ((string project, ImmutableArray<CopyItem> copyItems) in copyItemsByProject)
        {
            Log.Scope? scope2 = null;

            foreach ((string sourcePath, string targetPath, CopyType copyType, bool isBuildAccelerationOnly) in copyItems)
            {
                token.ThrowIfCancellationRequested();

                if (isBuildAccelerationEnabled is not true && isBuildAccelerationOnly)
                {
                    // This item should only be checked when build acceleration is enabled.
                    // For example, we only check referenced output assemblies when enabled.
                    // When not accelerating builds, checking these is unnecessary overhead.
                    continue;
                }

                string destinationPath = Path.Combine(outputFullPath, targetPath);

                if (StringComparers.Paths.Equals(sourcePath, destinationPath))
                {
                    // This can occur when a project is checking its own items, and the item already
                    // exists in the output directory.
                    continue;
                }

                if (scope1 is null)
                {
                    log.Verbose(nameof(VSResources.FUTD_CheckingCopyToOutputDirectoryItems));
                    scope1 = log.IndentScope();
                }

                if (scope2 is null)
                {
                    log.Verbose(nameof(VSResources.FUTDC_CheckingCopyItemsForProject_1), project);
                    scope2 = log.IndentScope();
                }

                log.Verbose(nameof(VSResources.FUTD_CheckingCopyToOutputDirectoryItem_1), copyType.ToString());

                DateTime? sourceTime = timestampCache.GetTimestampUtc(sourcePath);

                if (sourceTime is null)
                {
                    // We don't generally expect the source to be unavailable.
                    // If this occurs, schedule a build to be on the safe side.
                    return log.Fail("CopyToOutputDirectorySourceNotFound", nameof(VSResources.FUTD_CheckingCopyToOutputDirectorySourceNotFound_1), sourcePath);
                }

                using Log.Scope _ = log.IndentScope();

                log.Verbose(nameof(VSResources.FUTD_SourceFileTimeAndPath_2), sourceTime, sourcePath);

                DateTime? destinationTime = timestampCache.GetTimestampUtc(destinationPath);

                if (destinationTime is null)
                {
                    log.Verbose(nameof(VSResources.FUTD_DestinationDoesNotExist_1), destinationPath);

                    if (!fileSystemAggregator.AddCopy(sourcePath, destinationPath))
                    {
                        return log.Fail("CopyToOutputDirectoryDestinationNotFound", nameof(VSResources.FUTD_CheckingCopyToOutputDirectoryItemDestinationNotFound_1), destinationPath);
                    }

                    continue;
                }

                log.Verbose(nameof(VSResources.FUTD_DestinationFileTimeAndPath_2), destinationTime, destinationPath);

                switch (copyType)
                {
                    case CopyType.Always:
                    {
                        // We have already validated the presence of these files, so we don't expect these to return
                        // false. If one of them does, the corresponding size would be zero, so we would schedule a build.
                        // The odds of both source and destination disappearing between the gathering of the timestamps
                        // above and these following statements is vanishingly small, and would suggest bigger problems
                        // such as the entire project directory having been deleted.
                        _fileSystem.TryGetFileSizeBytes(sourcePath, out long sourceSizeBytes);
                        _fileSystem.TryGetFileSizeBytes(destinationPath, out long destinationSizeBytes);

                        if (sourceTime != destinationTime || sourceSizeBytes != destinationSizeBytes)
                        {
                            if (!fileSystemAggregator.AddCopy(sourcePath, destinationPath))
                            {
                                return log.Fail("CopyAlwaysItemDiffers", nameof(VSResources.FUTD_CopyAlwaysItemsDiffer_6), sourcePath, sourceTime, sourceSizeBytes, destinationPath, destinationTime, destinationSizeBytes);
                            }
                        }

                        break;
                    }

                    case CopyType.PreserveNewest:
                    {
                        if (destinationTime < sourceTime)
                        {
                            if (!fileSystemAggregator.AddCopy(sourcePath, destinationPath))
                            {
                                return log.Fail("CopyToOutputDirectorySourceNewer", nameof(VSResources.FUTD_CheckingCopyToOutputDirectorySourceNewerThanDestination_3), CopyType.PreserveNewest.ToString(), sourcePath, destinationPath);
                            }
                        }

                        break;
                    }

                    default:
                    {
                        System.Diagnostics.Debug.Fail("Project copy items should only contain copyable items.");
                        break;
                    }
                }
            }

            scope2?.Dispose();
        }

        scope1?.Dispose();

        return true;
    }

    void IProjectBuildEventListener.NotifyBuildStarting(DateTime buildStartTimeUtc)
    {
        _lastBuildStartTimeUtc = buildStartTimeUtc;
    }

    async Task IProjectBuildEventListener.NotifyBuildCompletedAsync(bool wasSuccessful, bool isRebuild)
    {
        if (_lastCopyTargetsFromThisProject is not null)
        {
            // The project build has completed. We must assume this project modified its outputs,
            // so we remove the outputs that were likely modified from our cache. The next requests
            // for these files will perform a fresh query.
            _solutionBuildContextProvider.CurrentSolutionBuildContext?.CopyItemTimestamps?.ClearTimestamps(_lastCopyTargetsFromThisProject);

            // We don't use this again after clearing the cache, so release it for GC.
            _lastCopyTargetsFromThisProject = null;
        }

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
            && buildTargetFramework.Equals(configurationTargetFramework, StringComparisons.ConfigurationDimensionValues);
    }

    Task<bool> IBuildUpToDateCheckProvider.IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken)
    {
        return IsUpToDateAsync(buildAction, logWriter, ImmutableDictionary<string, string>.Empty, cancellationToken);
    }

    async Task<(bool IsUpToDate, string? FailureReason, string? FailureDescription)> IBuildUpToDateCheckValidator.ValidateUpToDateAsync(CancellationToken cancellationToken)
    {
        bool isUpToDate = await IsUpToDateInternalAsync(TextWriter.Null, _lastGlobalProperties, isValidationRun: true, cancellationToken);

        string? failureReason = isUpToDate ? null : _lastFailureReason;
        string? failureDescription = isUpToDate ? null : _lastFailureDescription;

        return (isUpToDate, failureReason, failureDescription);
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
            // The subscription object calls GetLatestVersionAsync for project data, which can take a while.
            // We separate out the wait time from the overall time, so we can more easily identify when the
            // wait is long, versus the check's actual execution time.
            TimeSpan waitTime = sw.Elapsed;

            token.ThrowIfCancellationRequested();

            // Short-lived cache of timestamp by path
            var timestampCache = new TimestampCache(_fileSystem);

            // Ensure we have a context object for the current solution build.
            //
            // Ordinarily, this is created when the SBM calls ISolutionBuildEventListener.NotifySolutionBuildStarting,
            // and cleared again later when the SBM calls ISolutionBuildEventListener.NotifySolutionBuildCompleted.
            //
            // However there are two cases where it may be null here:
            //
            // 1. When performing a validation run that continues after the solution build completed, or
            // 2. When the build occurs in response to debugging (e.g. F5) in which case the SBM calls the
            //    FUTDC *before* it invokes any solution build events.
            //
            // In either case, we construct an event here lazily so that we can correctly test for the
            // existence of copy items in CheckCopyToOutputDirectoryItems.
            SolutionBuildContext? solutionBuildContext = _solutionBuildContextProvider.CurrentSolutionBuildContext;
            if (solutionBuildContext is null)
            {
                _solutionBuildEventListener.NotifySolutionBuildStarting();
                solutionBuildContext = _solutionBuildContextProvider.CurrentSolutionBuildContext;
                Assumes.NotNull(solutionBuildContext);
            }

            globalProperties.TryGetValue(FastUpToDateCheckIgnoresKindsGlobalPropertyName, out string? ignoreKindsString);

            (LogLevel requestedLogLevel, Guid projectGuid) = await (
                _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(token),
                _guidService.GetProjectGuidAsync(token));

            var logger = new Log(
                logWriter,
                this,
                UpToDateCheckers,
                requestedLogLevel,
                _solutionBuildEventListener,
                sw,
                waitTime,
                timestampCache,
                _configuredProject.UnconfiguredProject.FullPath ?? "",
                projectGuid,
                isValidationRun ? null : _telemetryService,
                state,
                ignoreKindsString,
                isValidationRun ? -1 : Interlocked.Increment(ref _checkNumber));

            var fileSystemOperations = new FileSystemOperationAggregator(_fileSystem, logger);

            logger.FileSystemOperations = fileSystemOperations;

            HashSet<string> copyItemPaths = new(StringComparers.Paths);

            try
            {
                HashSet<string>? ignoreKinds = null;
                if (ignoreKindsString is not null)
                {
                    ignoreKinds = new HashSet<string>(new LazyStringSplit(ignoreKindsString, ';'), KindNameComparer);

                    if (requestedLogLevel >= LogLevel.Info && ignoreKinds.Count != 0)
                    {
                        logger.Info(nameof(VSResources.FUTD_IgnoringKinds_1), ignoreKindsString);
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

                // Loop over target frameworks (or whatever other implicit configurations exist)
                foreach (UpToDateCheckImplicitConfiguredInput implicitState in implicitStatesToCheck)
                {
                    token.ThrowIfCancellationRequested();

                    if (logConfigurations)
                    {
                        logger.Info(nameof(VSResources.FUTD_CheckingConfiguration_1), implicitState.ProjectConfiguration.GetDisplayString());
                        logger.Indent++;
                    }

                    if (implicitState.IsDisabled)
                    {
                        if (isValidationRun)
                        {
                            // We don't run validation if the FUTDC is disabled. So pretend we're up to date.
                            return (true, checkedConfigurations);
                        }
                        else
                        {
                            logger.Fail("Disabled", nameof(VSResources.FUTD_DisableFastUpToDateCheckTrue));
                            return (false, checkedConfigurations);
                        }
                    }

                    string? path = _configuredProject.UnconfiguredProject.FullPath;

                    DateTime? lastSuccessfulBuildStartTimeUtc = path is null
                        ? null
                        : await persistence.RestoreLastSuccessfulBuildStateAsync(
                            path,
                            implicitState.ProjectConfiguration.Dimensions,
                            CancellationToken.None);

                    Assumes.NotNull(implicitState.ProjectTargetPath);

                    // We may have an incomplete set of copy items.
                    // We check timestamps of whatever items we can find, but only perform acceleration when the full set is available.
                    CopyItemsResult copyInfo = _copyItemAggregator.TryGatherCopyItemsForProject(implicitState.ProjectTargetPath, logger);

                    bool? isBuildAccelerationEnabled = await IsBuildAccelerationEnabledAsync(copyInfo.IsComplete, copyInfo.DuplicateCopyItemRelativeTargetPaths, implicitState);

                    var configuredFileSystemOperations = new ConfiguredFileSystemOperationAggregator(fileSystemOperations, isBuildAccelerationEnabled, copyInfo.TargetsWithoutReferenceAssemblies);

                    string outputFullPath = Path.Combine(implicitState.MSBuildProjectDirectory, implicitState.OutputRelativeOrFullPath);

                    copyItemPaths.UnionWith(implicitState.ProjectCopyData.CopyItems.Select(copyItem => Path.Combine(outputFullPath, copyItem.RelativeTargetPath)));

                    if (!CheckGlobalConditions(logger, lastSuccessfulBuildStartTimeUtc, validateFirstRun: !isValidationRun, implicitState) ||
                        !CheckInputsAndOutputs(logger, lastSuccessfulBuildStartTimeUtc, timestampCache, implicitState, ignoreKinds, token) ||
                        !CheckBuiltFromInputFiles(logger, timestampCache, implicitState, token) ||
                        !CheckMarkers(logger, timestampCache, implicitState, isBuildAccelerationEnabled, fileSystemOperations) ||
                        !CheckCopyToOutputDirectoryItems(logger, implicitState, copyInfo.ItemsByProject, configuredFileSystemOperations, isBuildAccelerationEnabled, solutionBuildContext, token))
                    {
                        return (false, checkedConfigurations);
                    }

                    if (logConfigurations)
                    {
                        logger.Indent--;
                    }
                }

                if (!isValidationRun)
                {
                    (bool success, int copyCount) = fileSystemOperations.TryApplyFileSystemOperations();

                    if (!success)
                    {
                        // Details of the failure will already have been logged.
                        return (false, checkedConfigurations);
                    }

                    if (copyCount != 0)
                    {
                        logger.Info(nameof(VSResources.FUTD_BuildAccelerationSummary_1), copyCount);
                    }

                    logger.UpToDate(copyCount);
                }

                return (true, checkedConfigurations);
            }
            catch (Exception ex)
            {
                return (logger.Fail("Exception", nameof(VSResources.FUTD_Exception_1), ex), ImmutableArray<ProjectConfiguration>.Empty);
            }
            finally
            {
                if (fileSystemOperations.IsAccelerationCandidate && fileSystemOperations.IsAccelerationEnabled is null)
                {
                    // We didn't copy anything, but we did find a candidate for build acceleration,
                    // and the project does not specify AccelerateBuildsInVisualStudio. Log a message to
                    // let the user know that their project might benefit from Build Acceleration.
                    logger.Minimal(nameof(VSResources.FUTD_AccelerationCandidate));
                }

                if (fileSystemOperations.IsAccelerationEnabled is true && fileSystemOperations.TargetsWithoutReferenceAssemblies is { Count: > 0 })
                {
                    // This project is configured to use build acceleration, but some of its references do not
                    // produce reference assemblies. Log a message to let the user know that they may be able
                    // to improve their build performance by enabling the production of reference assemblies.
                    logger.Minimal(nameof(VSResources.FUTD_NotAllReferencesProduceReferenceAssemblies_1), string.Join(", ", fileSystemOperations.TargetsWithoutReferenceAssemblies.Select(s => $"'{s}'")));
                }

                logger.Verbose(nameof(VSResources.FUTD_Completed), sw.Elapsed.TotalMilliseconds);

                _lastFailureReason = logger.FailureReason;
                _lastFailureDescription = logger.FailureDescription;

                _lastCopyTargetsFromThisProject = copyItemPaths;
            }

            async ValueTask<bool?> IsBuildAccelerationEnabledAsync(bool isCopyItemsComplete, IReadOnlyList<string>? duplicateCopyItemRelativeTargetPaths, UpToDateCheckImplicitConfiguredInput implicitState)
            {
                // Build acceleration requires:
                //
                // 1. being enabled, either in the project or via feature flags, and
                // 2. having a full set of copy items, and
                // 3. not having any project references known to be incompatible with Build Acceleration, and
                // 4. not having any duplicate copy items that would overwrite one another in the output directory (due to ordering issues).
                //
                // Being explicitly disabled in the project overrides any feature flag.

                if (implicitState.PresentBuildAccelerationIncompatiblePackages.Any())
                {
                    // At least one package reference exists that is incompatible with build acceleration.

                    // Check the log level to avoid the allocating string.Join unless needed.
                    if (logger.Level >= LogLevel.Info)
                    {
                        logger.Info(
                            nameof(VSResources.BuildAccelerationDisabledDueToIncompatiblePackageReferences_1),
                            string.Join(", ", implicitState.PresentBuildAccelerationIncompatiblePackages.Select(id => $"'{id}'")));
                    }

                    return false;
                }

                // Start with the preference specified in the project.
                bool? isEnabledInProject = implicitState.IsBuildAccelerationEnabled;

                bool isEnabled;

                if (isEnabledInProject is bool b)
                {
                    isEnabled = b;
                }
                else
                {
                    // No value has been specified in the project. Query the environment to decide (e.g. feature flag).
                    if (await _projectSystemOptions.IsBuildAccelerationEnabledByDefaultAsync(cancellationToken))
                    {
                        // The user has opted-in via feature flag. Set this to true and carry on with further checks.
                        logger.Info(nameof(VSResources.FUTD_BuildAccelerationEnabledViaFeatureFlag));
                        isEnabled = true;
                    }
                    else
                    {
                        logger.Info(nameof(VSResources.FUTD_BuildAccelerationIsNotEnabledForThisProject));
                        return null;
                    }
                }

                if (isEnabled)
                {
                    if (!isCopyItemsComplete)
                    {
                        logger.Info(nameof(VSResources.FUTD_AccelerationDisabledCopyItemsIncomplete));
                        return false;
                    }

                    if (duplicateCopyItemRelativeTargetPaths is not null)
                    {
                        logger.Info(nameof(VSResources.FUTD_AccelerationDisabledDuplicateCopyItemsIncomplete_1), string.Join(", ", duplicateCopyItemRelativeTargetPaths.Select(path => $"'{path}'")));
                        return false;
                    }

                    if (isEnabledInProject is not null)
                    {
                        // Don't log if isEnabledInProject is null, as we already log that status above.
                        logger.Info(nameof(VSResources.FUTD_BuildAccelerationEnabledViaProperty));
                    }

                    return true;
                }
                else
                {
                    // The project explicitly opts out.
                    logger.Info(nameof(VSResources.FUTD_AccelerationDisabledForProject));
                    return false;
                }
            }
        }
    }

    public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default)
    {
        return _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync(cancellationToken);
    }

    internal readonly struct TestAccessor(BuildUpToDateCheck check)
    {
        public void SetSubscription(ISubscription subscription) => check._subscription = subscription;
    }

    /// <summary>For unit testing only.</summary>
#pragma warning disable RS0043 // Do not call 'GetTestAccessor()'
    internal TestAccessor TestAccess => new(this);
#pragma warning restore RS0043
}
