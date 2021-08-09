// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [AppliesTo(AppliesToExpression)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [Export(typeof(IActiveConfigurationComponent))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed partial class BuildUpToDateCheck : IBuildUpToDateCheckProvider2, IActiveConfigurationComponent, IDisposable
    {
        internal const string AppliesToExpression = ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject;

        internal const string FastUpToDateCheckIgnoresKindsGlobalPropertyName = "FastUpToDateCheckIgnoresKinds";

        internal const string Link = "Link";
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
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly ITelemetryService _telemetryService;
        private readonly IFileSystem _fileSystem;
        private readonly IUpToDateCheckHost _upToDateCheckHost;

        private ISubscription _subscription;

        private int _isDisposed;

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IUpToDateCheckConfiguredInputDataSource inputDataSource,
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            ITelemetryService telemetryService,
            IFileSystem fileSystem,
            IUpToDateCheckHost upToDateCheckHost)
        {
            _inputDataSource = inputDataSource;
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _tasksService = tasksService;
            _telemetryService = telemetryService;
            _fileSystem = fileSystem;
            _upToDateCheckHost = upToDateCheckHost;
            _subscription = new Subscription(inputDataSource, configuredProject, upToDateCheckHost);
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
            ISubscription subscription = Interlocked.Exchange(ref _subscription, new Subscription(_inputDataSource, _configuredProject, _upToDateCheckHost));

            subscription.Dispose();
        }

        private bool CheckGlobalConditions(Log log, DateTime lastCheckedAtUtc, UpToDateCheckImplicitConfiguredInput state)
        {
            if (!_tasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build))
            {
                return log.Fail("CriticalTasks", "Critical build tasks are running, not up to date.");
            }

            if (state.IsDisabled)
            {
                return log.Fail("Disabled", "The 'DisableFastUpToDateCheck' property is true, not up to date.");
            }

            if (!state.WasStateRestored && lastCheckedAtUtc == DateTime.MinValue)
            {
                // We had no persisted state, and this is the first run. We cannot know if the project is up-to-date
                // or not, so schedule a build.
                return log.Fail("FirstRun", "The up-to-date check has not yet run for this project. Not up-to-date.");
            }

            foreach ((_, ImmutableArray<(string Path, string? TargetPath, CopyType CopyType)> items) in state.ItemsByItemType)
            {
                foreach ((string path, _, CopyType copyType) in items)
                {
                    if (copyType == CopyType.CopyAlways)
                    {
                        return log.Fail("CopyAlwaysItemExists", "Item '{0}' has CopyToOutputDirectory set to 'Always', not up to date.", _configuredProject.UnconfiguredProject.MakeRooted(path));
                    }
                }
            }

            return true;
        }

        private bool CheckInputsAndOutputs(Log log, DateTime lastCheckedAtUtc, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, HashSet<string>? ignoreKinds, CancellationToken token)
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
                log.Verbose("Comparing timestamps of inputs and outputs in set '{0}':", setName);

                if (!CheckInputsAndOutputs(CollectSetInputs(setName), CollectSetOutputs(setName), timestampCache, setName))
                {
                    return false;
                }
            }

            // Validation passed
            return true;

            bool CheckInputsAndOutputs(IEnumerable<(string Path, bool IsRequired)> inputs, IEnumerable<string> outputs, in TimestampCache timestampCache, string setName)
            {
                // We assume there are fewer outputs than inputs, so perform a full scan of outputs to find the earliest.
                // This increases the chance that we may return sooner in the case we are not up to date.
                DateTime earliestOutputTime = DateTime.MaxValue;
                string? earliestOutputPath = null;
                bool hasOutput = false;
                bool hasInput = false;

                foreach (string output in outputs)
                {
                    token.ThrowIfCancellationRequested();

                    DateTime? outputTime = timestampCache.GetTimestampUtc(output);

                    if (outputTime == null)
                    {
                        return log.Fail("Outputs", "Output '{0}' does not exist, not up to date.", output);
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
                    log.Info(setName == DefaultSetName ? "No build outputs defined." : "No build outputs defined in set '{0}'.", setName);

                    return true;
                }

                Assumes.NotNull(earliestOutputPath);

                if (earliestOutputTime < state.LastItemsChangedAtUtc)
                {
                    log.Fail("Outputs", "The set of project items was changed more recently ({0}) than the earliest output '{1}' ({2}), not up to date.", state.LastItemsChangedAtUtc, earliestOutputPath, earliestOutputTime);

                    if (log.Level <= LogLevel.Info)
                    {
                        foreach ((bool isAdd, string itemType, string path, string? targetPath, CopyType copyType) in state.LastItemChanges.OrderBy(change => change.ItemType).ThenBy(change => change.Path))
                        {
                            if (Strings.IsNullOrEmpty(targetPath))
                                log.Info("    {0} item {1} '{2}' (CopyType={3})", itemType, isAdd ? "added" : "removed", path, copyType);
                            else
                                log.Info("    {0} item {1} '{2}' (CopyType={3}, TargetPath='{4}')", itemType, isAdd ? "added" : "removed", path, copyType, targetPath);
                        }
                    }

                    return false;
                }

#if FALSE // https://github.com/dotnet/project-system/issues/6227

                if (_enableAdditionalDependentFile && earliestOutputTime < state.LastAdditionalDependentFileTimesChangedAtUtc)
                {
                    return log.Fail("Outputs", "The set of AdditionalDependentFileTimes was changed more recently ({0}) than the earliest output '{1}' ({2}), not up to date.", state.LastAdditionalDependentFileTimesChangedAtUtc, earliestOutputPath, earliestOutputTime);
                }
#endif

                foreach ((string input, bool isRequired) in inputs)
                {
                    token.ThrowIfCancellationRequested();

                    DateTime? inputTime = timestampCache.GetTimestampUtc(input);

                    if (inputTime == null)
                    {
                        if (isRequired)
                        {
                            return log.Fail("Outputs", "Input '{0}' does not exist and is required, not up to date.", input);
                        }
                        else
                        {
                            log.Verbose("Input '{0}' does not exist, but is not required.", input);
                        }
                    }

                    if (inputTime > earliestOutputTime)
                    {
                        return log.Fail("Outputs", "Input '{0}' is newer ({1}) than earliest output '{2}' ({3}), not up to date.", input, inputTime.Value, earliestOutputPath, earliestOutputTime);
                    }

                    if (inputTime > lastCheckedAtUtc && lastCheckedAtUtc != DateTime.MinValue)
                    {
                        // Bypass this test if no check has yet been performed. We handle that in CheckGlobalConditions.
                        return log.Fail("Outputs", "Input '{0}' ({1}) has been modified since the last up-to-date check ({2}), not up to date.", input, inputTime.Value, lastCheckedAtUtc);
                    }

                    hasInput = true;
                }

                if (log.Level <= LogLevel.Info)
                {
                    if (!hasInput)
                    {
                        log.Info(setName == DefaultSetName ? "No inputs defined." : "No inputs defined in set '{0}'.", setName);
                    }
                    else if (setName == DefaultSetName)
                    {
                        log.Info("No inputs are newer than earliest output '{0}' ({1}).", earliestOutputPath, earliestOutputTime);
                    }
                    else
                    {
                        log.Info("In set '{0}', no inputs are newer than earliest output '{1}' ({2}).", setName, earliestOutputPath, earliestOutputTime);
                    }
                }

                return true;
            }

            IEnumerable<(string Path, bool IsRequired)> CollectDefaultInputs()
            {
                if (state.MSBuildProjectFullPath != null)
                {
                    log.Verbose("Adding project file inputs:");
                    log.Verbose("    '{0}'", state.MSBuildProjectFullPath);
                    yield return (Path: state.MSBuildProjectFullPath, IsRequired: true);
                }

                if (state.NewestImportInput != null)
                {
                    log.Verbose("Adding newest import input:");
                    log.Verbose("    '{0}'", state.NewestImportInput);
                    yield return (Path: state.NewestImportInput, IsRequired: true);
                }

                foreach ((string itemType, ImmutableArray<(string path, string? targetPath, CopyType copyType)> changes) in state.ItemsByItemType)
                {
                    if (!NonCompilationItemTypes.Contains(itemType))
                    {
                        log.Verbose("Adding {0} inputs:", itemType);

                        foreach ((string path, _, _) in changes)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return (Path: absolutePath, IsRequired: true);
                        }
                    }
                }

                if (!state.ResolvedAnalyzerReferencePaths.IsEmpty)
                {
                    log.Verbose("Adding " + ResolvedAnalyzerReference.SchemaName + " inputs:");
                    foreach (string path in state.ResolvedAnalyzerReferencePaths)
                    {
                        string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                        log.Verbose("    '{0}'", absolutePath);
                        yield return (Path: absolutePath, IsRequired: true);
                    }
                }

                if (!state.ResolvedCompilationReferencePaths.IsEmpty)
                {
                    log.Verbose("Adding " + ResolvedCompilationReference.SchemaName + " inputs:");
                    foreach (string path in state.ResolvedCompilationReferencePaths)
                    {
                        System.Diagnostics.Debug.Assert(Path.IsPathRooted(path), "ResolvedCompilationReference path should be rooted");
                        log.Verbose("    '{0}'", path);
                        yield return (Path: path, IsRequired: true);
                    }
                }

                if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
                {
                    log.Verbose("Adding " + UpToDateCheckInput.SchemaName + " inputs:");
                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckInputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return (Path: absolutePath, IsRequired: true);
                        }
                    }
                }

#if FALSE // https://github.com/dotnet/project-system/issues/6227

                if (_enableAdditionalDependentFile && state.AdditionalDependentFileTimes.Count != 0)
                {
                    log.Verbose("Adding " + nameof(state.AdditionalDependentFileTimes) + " inputs:");
                    foreach ((string path, DateTime _) in state.AdditionalDependentFileTimes)
                    {
                        System.Diagnostics.Debug.Assert(Path.IsPathRooted(path), "AdditionalDependentFileTimes path should be rooted");
                        log.Verbose("    '{0}'", path);
                        yield return (Path: path, IsRequired: false);
                    }
                }
#endif
            }

            IEnumerable<string> CollectDefaultOutputs()
            {
                if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
                {
                    log.Verbose("Adding " + UpToDateCheckOutput.SchemaName + " outputs:");

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckOutputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return absolutePath;
                        }
                    }
                }

                if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(DefaultSetName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
                {
                    log.Verbose("Adding " + UpToDateCheckBuilt.SchemaName + " outputs:");

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckBuiltItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return absolutePath;
                        }
                    }
                }
            }

            IEnumerable<(string Path, bool IsRequired)> CollectSetInputs(string setName)
            {
                if (state.UpToDateCheckInputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckInputItems))
                {
                    log.Verbose("Adding " + UpToDateCheckInput.SchemaName + " inputs in set '{0}':", setName);
                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckInputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return (Path: absolutePath, IsRequired: true);
                        }
                    }
                }
            }

            IEnumerable<string> CollectSetOutputs(string setName)
            {
                if (state.UpToDateCheckOutputItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckOutputItems))
                {
                    log.Verbose("Adding " + UpToDateCheckOutput.SchemaName + " outputs in set '{0}':", setName);

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckOutputItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return absolutePath;
                        }
                    }
                }

                if (state.UpToDateCheckBuiltItemsByKindBySetName.TryGetValue(setName, out ImmutableDictionary<string, ImmutableArray<string>>? upToDateCheckBuiltItems))
                {
                    log.Verbose("Adding " + UpToDateCheckBuilt.SchemaName + " outputs in set '{0}':", setName);

                    foreach ((string kind, ImmutableArray<string> items) in upToDateCheckBuiltItems)
                    {
                        if (ShouldIgnoreItems(kind, items))
                        {
                            continue;
                        }

                        foreach (string path in items)
                        {
                            string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                            log.Verbose("    '{0}'", absolutePath);
                            yield return absolutePath;
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

                if (log.Level == LogLevel.Verbose)
                {
                    foreach (string path in items)
                    {
                        string absolutePath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                        log.Verbose("    Skipping '{0}' with ignored kind '{1}'", absolutePath, kind);
                    }
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

            string markerFile = _configuredProject.UnconfiguredProject.MakeRooted(state.CopyUpToDateMarkerItem);

            if (log.Level <= LogLevel.Verbose)
            {
                log.Verbose("Adding input reference copy markers:");

                foreach (string referenceMarkerFile in state.CopyReferenceInputs)
                {
                    log.Verbose("    '{0}'", referenceMarkerFile);
                }

                log.Verbose("Adding output reference copy marker:");
                log.Verbose("    '{0}'", markerFile);
            }

            if (timestampCache.TryGetLatestInput(state.CopyReferenceInputs, out string? latestInputMarkerPath, out DateTime latestInputMarkerTime))
            {
                log.Info("Latest write timestamp on input marker is {0} on '{1}'.", latestInputMarkerTime, latestInputMarkerPath);
            }
            else
            {
                log.Info("No input markers exist, skipping marker check.");
                return true;
            }

            DateTime? outputMarkerTime = timestampCache.GetTimestampUtc(markerFile);

            if (outputMarkerTime != null)
            {
                log.Info("Write timestamp on output marker is {0} on '{1}'.", outputMarkerTime, markerFile);
            }
            else
            {
                log.Info("Output marker '{0}' does not exist, skipping marker check.", markerFile);
                return true;
            }

            if (outputMarkerTime < latestInputMarkerTime)
            {
                return log.Fail("Marker", "Input marker is newer than output marker, not up to date.");
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

                log.Info("Checking copied output (" + UpToDateCheckBuilt.SchemaName + " with " + UpToDateCheckBuilt.OriginalProperty + " property) file '{0}':", source);

                DateTime? sourceTime = timestampCache.GetTimestampUtc(source);

                if (sourceTime != null)
                {
                    log.Info("    Source {0}: '{1}'.", sourceTime, source);
                }
                else
                {
                    return log.Fail("CopyOutput", "Source '{0}' does not exist, not up to date.", source);
                }

                DateTime? destinationTime = timestampCache.GetTimestampUtc(destination);

                if (destinationTime != null)
                {
                    log.Info("    Destination {0}: '{1}'.", destinationTime, destination);
                }
                else
                {
                    return log.Fail("CopyOutput", "Destination '{0}' does not exist, not up to date.", destination);
                }

                if (destinationTime < sourceTime)
                {
                    return log.Fail("CopyOutput", "Source is newer than build output destination, not up to date.");
                }
            }

            return true;
        }

        private bool CheckCopyToOutputDirectoryFiles(Log log, in TimestampCache timestampCache, UpToDateCheckImplicitConfiguredInput state, CancellationToken token)
        {
            string outputFullPath = Path.Combine(state.MSBuildProjectDirectory, state.OutputRelativeOrFullPath);

            foreach ((_, ImmutableArray<(string Path, string? TargetPath, CopyType CopyType)> items) in state.ItemsByItemType)
            {
                foreach ((string path, string? targetPath, CopyType copyType) in items)
                {
                    // Only consider items with CopyType of CopyIfNewer
                    if (copyType != CopyType.CopyIfNewer)
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    string rootedPath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                    string filename = Strings.IsNullOrEmpty(targetPath) ? rootedPath : targetPath;

                    if (string.IsNullOrEmpty(filename))
                    {
                        continue;
                    }

                    filename = _configuredProject.UnconfiguredProject.MakeRelative(filename);

                    log.Info("Checking PreserveNewest file '{0}':", rootedPath);

                    DateTime? itemTime = timestampCache.GetTimestampUtc(rootedPath);

                    if (itemTime != null)
                    {
                        log.Info("    Source {0}: '{1}'.", itemTime, rootedPath);
                    }
                    else
                    {
                        return log.Fail("CopyToOutputDirectory", "Source '{0}' does not exist, not up to date.", rootedPath);
                    }

                    string destination = Path.Combine(outputFullPath, filename);
                    DateTime? destinationTime = timestampCache.GetTimestampUtc(destination);

                    if (destinationTime != null)
                    {
                        log.Info("    Destination {0}: '{1}'.", destinationTime, destination);
                    }
                    else
                    {
                        return log.Fail("CopyToOutputDirectory", "Destination '{0}' does not exist, not up to date.", destination);
                    }

                    if (destinationTime < itemTime)
                    {
                        return log.Fail("CopyToOutputDirectory", "PreserveNewest source '{0}' is newer than destination '{1}', not up to date.", rootedPath, destination);
                    }
                }
            }

            return true;
        }

        Task<bool> IBuildUpToDateCheckProvider.IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken)
        {
            return IsUpToDateAsync(buildAction, logWriter, ImmutableDictionary<string, string>.Empty, cancellationToken);
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, IImmutableDictionary<string, string> globalProperties, CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                throw new ObjectDisposedException(nameof(BuildUpToDateCheck));
            }

            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Start the stopwatch now, so we include any lock acquisition in the timing
            var sw = Stopwatch.StartNew();

            ISubscription subscription = Volatile.Read(ref _subscription);

            return await subscription.RunAsync(IsUpToDateInternalAsync, cancellationToken);

            async Task<bool> IsUpToDateInternalAsync(UpToDateCheckConfiguredInput state, DateTime lastCheckedAtUtc, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // Short-lived cache of timestamp by path
                var timestampCache = new TimestampCache(_fileSystem);

                LogLevel requestedLogLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(token);
                var logger = new Log(logWriter, requestedLogLevel, sw, timestampCache, _configuredProject.UnconfiguredProject.FullPath ?? "", _telemetryService, state);

                try
                {
                    HashSet<string>? ignoreKinds = null;
                    if (globalProperties.TryGetValue(FastUpToDateCheckIgnoresKindsGlobalPropertyName, out string? ignoreKindsString))
                    {
                        ignoreKinds = new HashSet<string>(new LazyStringSplit(ignoreKindsString, ';'), StringComparer.OrdinalIgnoreCase);

                        if (requestedLogLevel >= LogLevel.Info && ignoreKinds.Count != 0)
                        {
                            logger.Info("Ignoring up-to-date check items with kinds: {0}", ignoreKindsString);
                        }
                    }

                    foreach (UpToDateCheckImplicitConfiguredInput implicitState in state.ImplicitInputs)
                    {
                        if (!CheckGlobalConditions(logger, lastCheckedAtUtc, implicitState) ||
                            !CheckInputsAndOutputs(logger, lastCheckedAtUtc, timestampCache, implicitState, ignoreKinds, token) ||
                            !CheckMarkers(logger, timestampCache, implicitState) ||
                            !CheckCopyToOutputDirectoryFiles(logger, timestampCache, implicitState, token) ||
                            !CheckCopiedOutputFiles(logger, timestampCache, implicitState, token))
                        {
                            return false;
                        }
                    }

                    logger.UpToDate();
                    return true;
                }
                finally
                {
                    logger.Verbose("Up to date check completed in {0:N1} ms", sw.Elapsed.TotalMilliseconds);
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
