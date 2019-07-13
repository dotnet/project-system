// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed partial class BuildUpToDateCheck : OnceInitializedOnceDisposedUnderLockAsync, IBuildUpToDateCheckProvider, IProjectDynamicLoadComponent
    {
        private const string CopyToOutputDirectory = "CopyToOutputDirectory";
        private const string PreserveNewest = "PreserveNewest";
        private const string Always = "Always";
        private const string Link = "Link";

        private static ImmutableHashSet<string> ReferenceSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(ResolvedAnalyzerReference.SchemaName)
            .Add(ResolvedCompilationReference.SchemaName);

        private static ImmutableHashSet<string> UpToDateSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(CopyUpToDateMarker.SchemaName)
            .Add(UpToDateCheckInput.SchemaName)
            .Add(UpToDateCheckOutput.SchemaName)
            .Add(UpToDateCheckBuilt.SchemaName);

        private static ImmutableHashSet<string> ProjectPropertiesSchemas => ImmutableStringHashSet.EmptyOrdinal
            .Add(ConfigurationGeneral.SchemaName)
            .Union(ReferenceSchemas)
            .Union(UpToDateSchemas);

        private static ImmutableHashSet<string> NonCompilationItemTypes => ImmutableHashSet<string>.Empty
            .WithComparer(StringComparers.ItemTypes)
            .Add(None.SchemaName)
            .Add(Content.SchemaName);

        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IProjectItemSchemaService _projectItemSchemaService;
        private readonly ITelemetryService _telemetryService;
        private readonly IFileSystem _fileSystem;

        private State _state = State.Empty;

        private IDisposable? _link;

        private bool _itemsChangedSinceLastCheck = true;

        internal DateTime LastCheckTimeUtc { get; private set; } = DateTime.MinValue;

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IProjectItemSchemaService projectItemSchemaService,
            ITelemetryService telemetryService,
            IProjectThreadingService threadingService,
            IFileSystem fileSystem)
            : base(threadingService.JoinableTaskContext)
        {
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _tasksService = tasksService;
            _projectItemSchemaService = projectItemSchemaService;
            _telemetryService = telemetryService;
            _fileSystem = fileSystem;
        }

        public Task LoadAsync()
        {
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            return ExecuteUnderLockAsync(_ =>
            {
                _link?.Dispose();
                _link = null;

                _state = State.Empty;

                LastCheckTimeUtc = DateTime.MinValue;
                _itemsChangedSinceLastCheck = true;

                return Task.CompletedTask;
            });
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _link = ProjectDataSources.SyncLinkTo(
                _configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectPropertiesSchemas)),
                _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                _projectItemSchemaService.SourceBlock.SyncLinkOptions(),
                target: DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectItemSchema>>>(OnChanged),
                linkOptions: DataflowOption.PropagateCompletion);

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreUnderLockAsync(bool initialized)
        {
            _link?.Dispose();

            return Task.CompletedTask;
        }

        private static string? GetLink(IImmutableDictionary<string, string> itemMetadata) =>
            itemMetadata.TryGetValue(Link, out string link) ? link : null;

        private static CopyToOutputDirectoryType GetCopyType(IImmutableDictionary<string, string> itemMetadata)
        {
            if (itemMetadata.TryGetValue(CopyToOutputDirectory, out string value))
            {
                if (string.Equals(value, Always, StringComparison.OrdinalIgnoreCase))
                {
                    return CopyToOutputDirectoryType.CopyAlways;
                }

                if (string.Equals(value, PreserveNewest, StringComparison.OrdinalIgnoreCase))
                {
                    return CopyToOutputDirectoryType.CopyIfNewer;
                }
            }

            return CopyToOutputDirectoryType.CopyNever;
        }

        internal void OnChanged(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectItemSchema>> e)
        {
            _state = _state.Update(
                jointRuleUpdate: e.Value.Item1,
                sourceItemsUpdate: e.Value.Item2,
                projectItemSchema: e.Value.Item3,
                configuredProjectVersion: e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion],
                out bool itemsChanged);

            if (itemsChanged)
            {
                _itemsChangedSinceLastCheck = true;
            }
        }

        private DateTime? GetTimestampUtc(string path, IDictionary<string, DateTime> timestampCache)
        {
            if (!timestampCache.TryGetValue(path, out DateTime time))
            {
                if (!_fileSystem.FileExists(path))
                {
                    return null;
                }
                time = _fileSystem.LastFileWriteTimeUtc(path);
                timestampCache[path] = time;
            }

            return time;
        }

        private bool Fail(BuildUpToDateCheckLogger logger, string reason, string message, params object[] values)
        {
            logger.Info(message, values);
            _telemetryService.PostProperty(TelemetryEventName.UpToDateCheckFail, TelemetryPropertyName.UpToDateCheckFailReason, reason);
            return false;
        }

        private bool CheckGlobalConditions(BuildAction buildAction, BuildUpToDateCheckLogger logger, State state)
        {
            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            bool itemsChangedSinceLastCheck = _itemsChangedSinceLastCheck;
            _itemsChangedSinceLastCheck = false;

            if (!_tasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build))
            {
                return Fail(logger, "CriticalTasks", "Critical build tasks are running, not up to date.");
            }

            if (state.LastVersionSeen == null || _configuredProject.ProjectVersion.CompareTo(state.LastVersionSeen) > 0)
            {
                return Fail(logger, "ProjectInfoOutOfDate", "Project information is older than current project version, not up to date.");
            }

            if (itemsChangedSinceLastCheck)
            {
                return Fail(logger, "ItemInfoOutOfDate", "The list of source items has changed since the last build, not up to date.");
            }

            if (state.IsDisabled)
            {
                return Fail(logger, "Disabled", "The 'DisableFastUpToDateCheck' property is true, not up to date.");
            }

            string copyAlwaysItemPath = state.Items.SelectMany(kvp => kvp.Value).FirstOrDefault(item => item.copyType == CopyToOutputDirectoryType.CopyAlways).path;

            if (copyAlwaysItemPath != null)
            {
                return Fail(logger, "CopyAlwaysItemExists", "Item '{0}' has CopyToOutputDirectory set to 'Always', not up to date.", _configuredProject.UnconfiguredProject.MakeRooted(copyAlwaysItemPath));
            }

            return true;
        }

        private IEnumerable<string> CollectInputs(BuildUpToDateCheckLogger logger, State state)
        {
            if (state.MSBuildProjectFullPath != null)
            {
                logger.Verbose("Adding project file inputs:");
                logger.Verbose("    '{0}'", state.MSBuildProjectFullPath);
                yield return state.MSBuildProjectFullPath;
            }

            if (state.NewestImportInput != null)
            {
                logger.Verbose("Adding newest import input:");
                logger.Verbose("    '{0}'", state.NewestImportInput);
                yield return state.NewestImportInput;
            }

            foreach ((string itemType, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)> changes) in state.Items)
            {
                if (!NonCompilationItemTypes.Contains(itemType))
                {
                    logger.Verbose("Adding {0} inputs:", itemType);

                    foreach (string input in changes.Select(item => _configuredProject.UnconfiguredProject.MakeRooted(item.path)))
                    {
                        logger.Verbose("    '{0}'", input);
                        yield return input;
                    }
                }
            }

            if (state.AnalyzerReferences.Count != 0)
            {
                logger.Verbose("Adding " + ResolvedAnalyzerReference.SchemaName + " inputs:");
                foreach (string input in state.AnalyzerReferences)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            if (state.CompilationReferences.Count != 0)
            {
                logger.Verbose("Adding " + ResolvedCompilationReference.SchemaName + " inputs:");
                foreach (string input in state.CompilationReferences)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            if (state.CustomInputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckInput.SchemaName + " inputs:");
                // TODO remove pragmas when https://github.com/dotnet/roslyn/issues/37040 is fixed
#pragma warning disable CS8622
                foreach (string input in state.CustomInputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
#pragma warning restore CS8622
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }
        }

        private IEnumerable<string> CollectOutputs(BuildUpToDateCheckLogger logger, State state)
        {
            if (state.CustomOutputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckOutput.SchemaName + " outputs:");

                // TODO remove pragmas when https://github.com/dotnet/roslyn/issues/37040 is fixed
#pragma warning disable CS8622
                foreach (string output in state.CustomOutputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
#pragma warning restore CS8622
                {
                    logger.Verbose("    '{0}'", output);
                    yield return output;
                }
            }

            if (state.BuiltOutputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckBuilt.SchemaName + " outputs:");

                // TODO remove pragmas when https://github.com/dotnet/roslyn/issues/37040 is fixed
#pragma warning disable CS8622
                foreach (string output in state.BuiltOutputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
#pragma warning restore CS8622
                {
                    logger.Verbose("    '{0}'", output);
                    yield return output;
                }
            }
        }

        private (DateTime time, string? path) GetLatestInput(IEnumerable<string> inputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime latest = DateTime.MinValue;
            string? latestPath = null;

            foreach (string input in inputs)
            {
                DateTime? time = GetTimestampUtc(input, timestampCache);

                if (time > latest)
                {
                    // TODO remove pragmas when https://github.com/dotnet/roslyn/issues/37039 is fixed
#pragma warning disable CS8629 // Nullable value type may be null
                    latest = time.Value;
#pragma warning restore CS8629
                    latestPath = input;
                }
            }

            return (latest, latestPath);
        }

        /// <summary>
        /// Returns one of:
        /// <list type="bullet">
        ///     <item><c>(time, path)</c> describing the earliest output when all were found</item>
        ///     <item><c>(null, path)</c> where <c>path</c> is the first output that could not be found</item>
        ///     <item><c>(null, null)</c> when there were no outputs</item>
        /// </list>
        /// </summary>
        private (DateTime? time, string? path) GetEarliestOutput(IEnumerable<string> outputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime? earliest = DateTime.MaxValue;
            string? earliestPath = null;
            bool hasOutput = false;

            foreach (string output in outputs)
            {
                DateTime? time = GetTimestampUtc(output, timestampCache);

                if (time == null)
                {
                    return (null, output);
                }

                if (time < earliest)
                {
                    earliest = time;
                    earliestPath = output;
                }

                hasOutput = true;
            }

            return hasOutput
                ? (earliest, earliestPath)
                : (null, null);
        }

        private bool CheckOutputs(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache, State state)
        {
            // We assume there are fewer outputs than inputs, so perform a full scan of outputs to find the earliest
            (DateTime? outputTime, string? outputPath) = GetEarliestOutput(CollectOutputs(logger, state), timestampCache);

            if (outputTime != null)
            {
                Assumes.NotNull(outputPath);

                // Search for an input that's either missing or newer than the earliest output.
                // As soon as we find one, we can stop the scan.
                foreach (string input in CollectInputs(logger, state))
                {
                    DateTime? time = GetTimestampUtc(input, timestampCache);

                    if (time == null)
                    {
                        return Fail(logger, "Outputs", "Input '{0}' does not exist, not up to date.", input);
                    }

                    if (time > outputTime)
                    {
                        return Fail(logger, "Outputs", "Input '{0}' is newer ({1}) than earliest output '{2}' ({3}), not up to date.", input, time.Value, outputPath!, outputTime.Value);
                    }

                    if (time > LastCheckTimeUtc)
                    {
                        return Fail(logger, "Outputs", "Input '{0}' ({1}) has been modified since the last up-to-date check ({2}), not up to date.", input, time.Value, LastCheckTimeUtc);
                    }
                }

                logger.Info("No inputs are newer than earliest output '{0}' ({1}).", outputPath!, outputTime.Value);
            }
            else if (outputPath != null)
            {
                return Fail(logger, "Outputs", "Output '{0}' does not exist, not up to date.", outputPath);
            }
            else
            {
                logger.Info("No build outputs defined.");
            }

            return true;
        }

        // Reference assembly copy markers are strange. The property is always going to be present on
        // references to SDK-based projects, regardless of whether or not those referenced projects
        // will actually produce a marker. And an item always will be present in an SDK-based project,
        // regardless of whether or not the project produces a marker. So, basically, we only check
        // here if the project actually produced a marker and we only check it against references that
        // actually produced a marker.
        private bool CheckMarkers(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache, State state)
        {
            if (string.IsNullOrWhiteSpace(state.MarkerFile) || !state.CopyReferenceInputs.Any())
            {
                return true;
            }

            string markerFile = _configuredProject.UnconfiguredProject.MakeRooted(state.MarkerFile);

            logger.Verbose("Adding input reference copy markers:");

            foreach (string referenceMarkerFile in state.CopyReferenceInputs)
            {
                logger.Verbose("    '{0}'", referenceMarkerFile);
            }

            logger.Verbose("Adding output reference copy marker:");
            logger.Verbose("    '{0}'", markerFile);

            (DateTime latestInputMarkerTime, string? latestInputMarkerPath) = GetLatestInput(state.CopyReferenceInputs, timestampCache);

            if (latestInputMarkerPath != null)
            {
                logger.Info("Latest write timestamp on input marker is {0} on '{1}'.", latestInputMarkerTime, latestInputMarkerPath);
            }
            else
            {
                logger.Info("No input markers exist, skipping marker check.");
                return true;
            }

            DateTime? outputMarkerTime = GetTimestampUtc(markerFile, timestampCache);

            if (outputMarkerTime != null)
            {
                logger.Info("Write timestamp on output marker is {0} on '{1}'.", outputMarkerTime, markerFile);
            }
            else
            {
                logger.Info("Output marker '{0}' does not exist, skipping marker check.", markerFile);
                return true;
            }

            if (outputMarkerTime < latestInputMarkerTime)
            {
                return Fail(logger, "Marker", "Input marker is newer than output marker, not up to date.");
            }

            return true;
        }

        private bool CheckCopiedOutputFiles(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache, State state)
        {
            foreach ((string destinationRelative, string sourceRelative) in state.CopiedOutputFiles)
            {
                string source = _configuredProject.UnconfiguredProject.MakeRooted(sourceRelative);
                string destination = _configuredProject.UnconfiguredProject.MakeRooted(destinationRelative);

                logger.Info("Checking copied output (" + UpToDateCheckBuilt.SchemaName + " with " + UpToDateCheckBuilt.OriginalProperty + " property) file '{0}':", source);

                DateTime? sourceTime = GetTimestampUtc(source, timestampCache);

                if (sourceTime != null)
                {
                    logger.Info("    Source {0}: '{1}'.", sourceTime, source);
                }
                else
                {
                    return Fail(logger, "CopyOutput", "Source '{0}' does not exist, not up to date.", source);
                }

                DateTime? destinationTime = GetTimestampUtc(destination, timestampCache);

                if (destinationTime != null)
                {
                    logger.Info("    Destination {0}: '{1}'.", destinationTime, destination);
                }
                else
                {
                    return Fail(logger, "CopyOutput", "Destination '{0}' does not exist, not up to date.", destination);
                }

                if (destinationTime < sourceTime)
                {
                    return Fail(logger, "CopyOutput", "Source is newer than build output destination, not up to date.");
                }
            }

            return true;
        }

        private bool CheckCopyToOutputDirectoryFiles(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache, State state)
        {
            IEnumerable<(string path, string? link, CopyToOutputDirectoryType copyType)> items = state.Items.SelectMany(kvp => kvp.Value).Where(item => item.copyType == CopyToOutputDirectoryType.CopyIfNewer);

            string outputFullPath = Path.Combine(state.MSBuildProjectDirectory, state.OutputRelativeOrFullPath);

            foreach ((string path, string? link, _) in items)
            {
                string rootedPath = _configuredProject.UnconfiguredProject.MakeRooted(path);
                string filename = string.IsNullOrEmpty(link) ? rootedPath : link;

                if (string.IsNullOrEmpty(filename))
                {
                    continue;
                }

                filename = _configuredProject.UnconfiguredProject.MakeRelative(filename);

                logger.Info("Checking PreserveNewest file '{0}':", rootedPath);

                DateTime? itemTime = GetTimestampUtc(rootedPath, timestampCache);

                if (itemTime != null)
                {
                    logger.Info("    Source {0}: '{1}'.", itemTime, rootedPath);
                }
                else
                {
                    return Fail(logger, "CopyToOutputDirectory", "Source '{0}' does not exist, not up to date.", rootedPath);
                }

                string outputItem = Path.Combine(outputFullPath, filename);
                DateTime? outputItemTime = GetTimestampUtc(outputItem, timestampCache);

                if (outputItemTime != null)
                {
                    logger.Info("    Destination {0}: '{1}'.", outputItemTime, outputItem);
                }
                else
                {
                    return Fail(logger, "CopyToOutputDirectory", "Destination '{0}' does not exist, not up to date.", outputItem);
                }

                if (outputItemTime < itemTime)
                {
                    return Fail(logger, "CopyToOutputDirectory", "PreserveNewest source is newer than destination, not up to date.");
                }
            }

            return true;
        }

        public Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ExecuteUnderLockAsync(IsUpToDateInternalAsync, cancellationToken);

            async Task<bool> IsUpToDateInternalAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var sw = Stopwatch.StartNew();

                await InitializeAsync(token);

                LogLevel requestedLogLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(token);
                var logger = new BuildUpToDateCheckLogger(logWriter, requestedLogLevel, _configuredProject.UnconfiguredProject.FullPath);

                try
                {
                    State state = _state;

                    if (!CheckGlobalConditions(buildAction, logger, state))
                    {
                        return false;
                    }

                    // Short-lived cache of timestamp by path
                    var timestampCache = new Dictionary<string, DateTime>(StringComparers.Paths);

                    if (!CheckOutputs(logger, timestampCache, state) ||
                        !CheckMarkers(logger, timestampCache, state) ||
                        !CheckCopyToOutputDirectoryFiles(logger, timestampCache, state) ||
                        !CheckCopiedOutputFiles(logger, timestampCache, state))
                    {
                        return false;
                    }

                    _telemetryService.PostEvent(TelemetryEventName.UpToDateCheckSuccess);
                    logger.Info("Project is up to date.");
                    return true;
                }
                finally
                {
                    LastCheckTimeUtc = DateTime.UtcNow;
                    logger.Verbose("Up to date check completed in {0:#,##0.#} ms", sw.Elapsed.TotalMilliseconds);
                }
            }
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default) =>
            _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync(cancellationToken);
    }
}
