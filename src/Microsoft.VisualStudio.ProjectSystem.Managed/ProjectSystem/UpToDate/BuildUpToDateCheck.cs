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
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed class BuildUpToDateCheck : OnceInitializedOnceDisposed, IBuildUpToDateCheckProvider
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

        private IDisposable _link;
        private IComparable _lastVersionSeen;

        private bool _isDisabled = true;
        private bool _itemsChangedSinceLastCheck = true;
        private string _msBuildProjectFullPath;
        private string _msBuildProjectDirectory;
        private string _markerFile;
        private string _outputRelativeOrFullPath;
        private string _newestImportInput;

        private DateTime _lastCheckTimeUtc = DateTime.MinValue;

        private readonly HashSet<string> _itemTypes = new HashSet<string>(StringComparers.ItemTypes);
        private readonly Dictionary<string, HashSet<(string path, string link, CopyToOutputDirectoryType copyType)>> _items = new Dictionary<string, HashSet<(string, string, CopyToOutputDirectoryType)>>(StringComparers.ItemTypes);
        private readonly HashSet<string> _customInputs = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _customOutputs = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _builtOutputs = new HashSet<string>(StringComparers.Paths);

        /// <summary>Key is destination, value is source.</summary>
        private readonly Dictionary<string, string> _copiedOutputFiles = new Dictionary<string, string>(StringComparers.Paths);

        private readonly HashSet<string> _analyzerReferences = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _compilationReferences = new HashSet<string>(StringComparers.Paths);
        private readonly HashSet<string> _copyReferenceInputs = new HashSet<string>(StringComparers.Paths);

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IProjectItemSchemaService projectItemSchemaService,
            ITelemetryService telemetryService,
            IFileSystem fileSystem)
        {
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _tasksService = tasksService;
            _projectItemSchemaService = projectItemSchemaService;
            _telemetryService = telemetryService;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Called on project load.
        /// </summary>
#pragma warning disable RS0030 // symbol ConfiguredProjectAutoLoad is banned
        [ConfiguredProjectAutoLoad]
#pragma warning restore RS0030 // symbol ConfiguredProjectAutoLoad is banned
        [AppliesTo(ProjectCapability.DotNet + "+ !" + ProjectCapabilities.SharedAssetsProject)]
        internal void Load()
        {
            EnsureInitialized();
        }

        protected override void Initialize()
        {
            _link = ProjectDataSources.SyncLinkTo(
                _configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectPropertiesSchemas)),
                _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                _projectItemSchemaService.SourceBlock.SyncLinkOptions(),
                target: new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectItemSchema>>>(e => OnChanged(e)),
                linkOptions: DataflowOption.PropagateCompletion);
        }

        private void OnProjectChanged(IProjectSubscriptionUpdate e)
        {
            _isDisabled = e.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

            _msBuildProjectFullPath = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, _msBuildProjectFullPath);
            _msBuildProjectDirectory = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, _msBuildProjectDirectory);
            _outputRelativeOrFullPath = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, _outputRelativeOrFullPath);
            string msBuildAllProjects = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, "");

            // The first item in this semicolon-separated list of project files will always be the one
            // with the newest timestamp. As we are only interested in timestamps on these files, we can
            // save memory and time by only considering this first path (dotnet/project-system#4333).
            _newestImportInput = new LazyStringSplit(msBuildAllProjects, ';').FirstOrDefault();

            if (e.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription changes) &&
                changes.Difference.AnyChanges)
            {
                _analyzerReferences.Clear();
                _analyzerReferences.AddRange(changes.After.Items.Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty]));
            }

            if (e.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out changes) &&
                changes.Difference.AnyChanges)
            {
                _compilationReferences.Clear();
                _copyReferenceInputs.Clear();

                foreach (IImmutableDictionary<string, string> item in changes.After.Items.Values)
                {
                    _compilationReferences.Add(item[ResolvedCompilationReference.ResolvedPathProperty]);
                    if (!string.IsNullOrWhiteSpace(item[CopyUpToDateMarker.SchemaName]))
                    {
                        _copyReferenceInputs.Add(item[CopyUpToDateMarker.SchemaName]);
                    }
                    if (!string.IsNullOrWhiteSpace(item[ResolvedCompilationReference.OriginalPathProperty]))
                    {
                        _copyReferenceInputs.Add(item[ResolvedCompilationReference.OriginalPathProperty]);
                    }
                }
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out IProjectChangeDescription inputs) &&
                inputs.Difference.AnyChanges)
            {
                _customInputs.Clear();
                _customInputs.AddRange(inputs.After.Items.Keys);
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out IProjectChangeDescription outputs) &&
                outputs.Difference.AnyChanges)
            {
                _customOutputs.Clear();
                _customOutputs.AddRange(outputs.After.Items.Keys);
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out IProjectChangeDescription built) &&
                built.Difference.AnyChanges)
            {
                _copiedOutputFiles.Clear();
                _builtOutputs.Clear();

                foreach ((string destination, IImmutableDictionary<string, string> properties) in built.After.Items)
                {
                    if (properties.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) &&
                        !string.IsNullOrEmpty(source))
                    {
                        // This file is copied, not built
                        // Remember the `Original` source for later
                        _copiedOutputFiles[destination] = source;
                    }
                    else
                    {
                        // This file is built, not copied
                        _builtOutputs.Add(destination);
                    }
                }
            }

            if (e.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out IProjectChangeDescription upToDateMarkers) &&
                upToDateMarkers.Difference.AnyChanges)
            {
                _markerFile = upToDateMarkers.After.Items.Count == 1 ? upToDateMarkers.After.Items.Single().Key : null;
            }
        }

        private static string GetLink(IImmutableDictionary<string, string> itemMetadata) =>
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

        private void OnSourceItemChanged(IProjectSubscriptionUpdate e, IProjectItemSchema projectItemSchema)
        {
            string[] itemTypes = projectItemSchema.GetKnownItemTypes().Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput).ToArray();
            bool itemTypesChanged = !_itemTypes.SetEquals(itemTypes);

            if (itemTypesChanged)
            {
                _itemTypes.Clear();
                _itemTypes.AddRange(itemTypes);
                _items.Clear();
            }

            foreach ((string itemType, IProjectChangeDescription changes) in e.ProjectChanges)
            {
                if (!_itemTypes.Contains(itemType))
                    continue;
                if (!itemTypesChanged && !changes.Difference.AnyChanges)
                    continue;

                _items[itemType] = new HashSet<(string path, string link, CopyToOutputDirectoryType copyType)>(
                    changes.After.Items.Select(item => (item.Key, GetLink(item.Value), GetCopyType(item.Value))),
                    UpToDateCheckItemComparer.Instance);
                _itemsChangedSinceLastCheck = true;
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out IProjectChangeDescription outputs) &&
                outputs.Difference.AnyChanges)
            {
                _customOutputs.Clear();
                _customOutputs.AddRange(outputs.After.Items.Keys);
            }
        }

        internal void OnChanged(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectSubscriptionUpdate, IProjectItemSchema>> e)
        {
            OnProjectChanged(e.Value.Item1);
            OnSourceItemChanged(e.Value.Item2, e.Value.Item3);
            _lastVersionSeen = e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
        }

        protected override void Dispose(bool disposing)
        {
            _link?.Dispose();
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

        private bool CheckGlobalConditions(BuildAction buildAction, BuildUpToDateCheckLogger logger)
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

            if (_lastVersionSeen == null || _configuredProject.ProjectVersion.CompareTo(_lastVersionSeen) > 0)
            {
                return Fail(logger, "ProjectInfoOutOfDate", "Project information is older than current project version, not up to date.");
            }

            if (itemsChangedSinceLastCheck)
            {
                return Fail(logger, "ItemInfoOutOfDate", "The list of source items has changed since the last build, not up to date.");
            }

            if (_isDisabled)
            {
                return Fail(logger, "Disabled", "The 'DisableFastUpToDateCheck' property is true, not up to date.");
            }

            string copyAlwaysItemPath = _items.SelectMany(kvp => kvp.Value).FirstOrDefault(item => item.copyType == CopyToOutputDirectoryType.CopyAlways).path;

            if (copyAlwaysItemPath != null)
            {
                return Fail(logger, "CopyAlwaysItemExists", "Item '{0}' has CopyToOutputDirectory set to 'Always', not up to date.", _configuredProject.UnconfiguredProject.MakeRooted(copyAlwaysItemPath));
            }

            return true;
        }

        private IEnumerable<string> CollectInputs(BuildUpToDateCheckLogger logger)
        {
            logger.Verbose("Adding project file inputs:");
            logger.Verbose("    '{0}'", _msBuildProjectFullPath);
            yield return _msBuildProjectFullPath;

            if (_newestImportInput != null)
            {
                logger.Verbose("Adding newest import input:");
                logger.Verbose("    '{0}'", _newestImportInput);
                yield return _newestImportInput;
            }

            foreach ((string itemType, HashSet<(string path, string link, CopyToOutputDirectoryType copyType)> changes) in _items)
            {
                if (changes.Count != 0 && !NonCompilationItemTypes.Contains(itemType))
                {
                    logger.Verbose("Adding {0} inputs:", itemType);

                    foreach (string input in changes.Select(item => _configuredProject.UnconfiguredProject.MakeRooted(item.path)))
                    {
                        logger.Verbose("    '{0}'", input);
                        yield return input;
                    }
                }
            }

            if (_analyzerReferences.Count != 0)
            {
                logger.Verbose("Adding " + ResolvedAnalyzerReference.SchemaName + " inputs:");
                foreach (string input in _analyzerReferences)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            if (_compilationReferences.Count != 0)
            {
                logger.Verbose("Adding " + ResolvedCompilationReference.SchemaName + " inputs:");
                foreach (string input in _compilationReferences)
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }

            if (_customInputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckInput.SchemaName + " inputs:");
                foreach (string input in _customInputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
                {
                    logger.Verbose("    '{0}'", input);
                    yield return input;
                }
            }
        }

        private IEnumerable<string> CollectOutputs(BuildUpToDateCheckLogger logger)
        {
            if (_customOutputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckOutput.SchemaName + " outputs:");

                foreach (string output in _customOutputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
                {
                    logger.Verbose("    '{0}'", output);
                    yield return output;
                }
            }

            if (_builtOutputs.Count != 0)
            {
                logger.Verbose("Adding " + UpToDateCheckBuilt.SchemaName + " outputs:");

                foreach (string output in _builtOutputs.Select(_configuredProject.UnconfiguredProject.MakeRooted))
                {
                    logger.Verbose("    '{0}'", output);
                    yield return output;
                }
            }
        }

        private (DateTime time, string path) GetLatestInput(IEnumerable<string> inputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime latest = DateTime.MinValue;
            string latestPath = null;

            foreach (string input in inputs)
            {
                DateTime? time = GetTimestampUtc(input, timestampCache);

                if (time > latest)
                {
                    latest = time.Value;
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
        private (DateTime? time, string path) GetEarliestOutput(IEnumerable<string> outputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime? earliest = DateTime.MaxValue;
            string earliestPath = null;
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

        private bool CheckOutputs(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            // We assume there are fewer outputs than inputs, so perform a full scan of outputs to find the earliest
            (DateTime? outputTime, string outputPath) = GetEarliestOutput(CollectOutputs(logger), timestampCache);

            if (outputTime != null)
            {
                // Search for an input that's either missing or newer than the earliest output.
                // As soon as we find one, we can stop the scan.
                foreach (string input in CollectInputs(logger))
                {
                    DateTime? time = GetTimestampUtc(input, timestampCache);

                    if (time == null)
                    {
                        return Fail(logger, "Outputs", "Input '{0}' does not exist, not up to date.", input);
                    }

                    if (time > outputTime)
                    {
                        return Fail(logger, "Outputs", "Input '{0}' is newer ({1}) than earliest output '{2}' ({3}), not up to date.", input, time.Value, outputPath, outputTime.Value);
                    }

                    if (time > _lastCheckTimeUtc)
                    {
                        return Fail(logger, "Outputs", "Input '{0}' has been modified since the last up-to-date check, not up to date.", input, time.Value);
                    }
                }
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
        private bool CheckMarkers(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            if (string.IsNullOrWhiteSpace(_markerFile) || !_copyReferenceInputs.Any())
            {
                return true;
            }

            string markerFile = _configuredProject.UnconfiguredProject.MakeRooted(_markerFile);

            logger.Verbose("Adding input reference copy markers:");

            foreach (string referenceMarkerFile in _copyReferenceInputs)
            {
                logger.Verbose("    '{0}'", referenceMarkerFile);
            }

            logger.Verbose("Adding output reference copy marker:");
            logger.Verbose("    '{0}'", markerFile);

            (DateTime latestInputMarkerTime, string latestInputMarkerPath) = GetLatestInput(_copyReferenceInputs, timestampCache);

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

        private bool CheckCopiedOutputFiles(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            foreach ((string destinationRelative, string sourceRelative) in _copiedOutputFiles)
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

        private bool CheckCopyToOutputDirectoryFiles(BuildUpToDateCheckLogger logger, IDictionary<string, DateTime> timestampCache)
        {
            IEnumerable<(string path, string link, CopyToOutputDirectoryType copyType)> items = _items.SelectMany(kvp => kvp.Value).Where(item => item.copyType == CopyToOutputDirectoryType.CopyIfNewer);

            string outputFullPath = Path.Combine(_msBuildProjectDirectory, _outputRelativeOrFullPath);

            foreach ((string path, string link, _) in items)
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
                    return Fail(logger, "CopyToOutputDirectory", "PreserveNewest destination is newer than source, not up to date.");
                }
            }

            return true;
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();

            EnsureInitialized();

            LogLevel requestedLogLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync(cancellationToken);
            var logger = new BuildUpToDateCheckLogger(logWriter, requestedLogLevel, _configuredProject.UnconfiguredProject.FullPath);

            try
            {
                if (!CheckGlobalConditions(buildAction, logger))
                {
                    return false;
                }

                // Short-lived cache of timestamp by path
                var timestampCache = new Dictionary<string, DateTime>(StringComparers.Paths);

                if (!CheckOutputs(logger, timestampCache) ||
                    !CheckMarkers(logger, timestampCache) ||
                    !CheckCopyToOutputDirectoryFiles(logger, timestampCache) ||
                    !CheckCopiedOutputFiles(logger, timestampCache))
                {
                    return false;
                }

                _telemetryService.PostEvent(TelemetryEventName.UpToDateCheckSuccess);
                logger.Info("Project is up to date.");
                return true;
            }
            finally
            {
                _lastCheckTimeUtc = DateTime.UtcNow;
                logger.Verbose("Up to date check completed in {0:#,##0.#} ms", sw.Elapsed.TotalMilliseconds);
            }
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default) =>
            _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync(cancellationToken);
    }
}
