// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)]
    internal sealed class BuildUpToDateCheck : OnceInitializedOnceDisposed, IBuildUpToDateCheckProvider
    {
        private sealed class Logger
        {
            private TextWriter _logger;
            private LogLevel _requestedLogLevel;
            private string _fileName;

            public Logger(TextWriter logger, LogLevel requestedLogLevel, string projectPath)
            {
                _logger = logger;
                _requestedLogLevel = requestedLogLevel;
                _fileName = Path.GetFileNameWithoutExtension(projectPath);
            }

            private void Log(LogLevel level, string message, params object[] values)
            {
                if (level <= _requestedLogLevel)
                {
                    _logger?.WriteLine($"FastUpToDate: {string.Format(message, values)} ({_fileName})");
                }
            }

            public void Info(string message, params object[] values) => Log(LogLevel.Info, message, values);
            public void Verbose(string message, params object[] values) => Log(LogLevel.Verbose, message, values);
        }

        private const string TrueValue = "true";
        private const string FullPath = "FullPath";
        private const string ResolvedPath = "ResolvedPath";
        private const string CopyToOutputDirectory = "CopyToOutputDirectory";
        private const string Never = "Never";
        private const string OriginalPath = "OriginalPath";
        private const string TelemetryEventName = "UpToDateCheck";

        private static HashSet<string> KnownOutputGroups = new HashSet<string>
        {
            "Symbols",
            "Built",
            "ContentFiles",
            "Documentation",
            "LocalizedResourceDlls"
        };

        private static ImmutableHashSet<string> ReferenceSchemas => ImmutableHashSet<string>.Empty
            .Add(ResolvedAnalyzerReference.SchemaName)
            .Add(ResolvedCompilationReference.SchemaName);

        private static ImmutableHashSet<string> UpToDateSchemas => ImmutableHashSet<string>.Empty
            .Add(CopyUpToDateMarker.SchemaName)
            .Add(UpToDateCheckInput.SchemaName)
            .Add(UpToDateCheckOutput.SchemaName);

        private static ImmutableHashSet<string> ProjectPropertiesSchemas => ImmutableHashSet<string>.Empty
            .Add(ConfigurationGeneral.SchemaName)
            .Union(ReferenceSchemas)
            .Union(UpToDateSchemas);

        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly Lazy<IFileTimestampCache> _fileTimestampCache;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IProjectItemSchemaService _projectItemSchemaService;
        private readonly ITelemetryService _telemetryService;

        private IDisposable _link;
        private IComparable _lastVersionSeen;

        private bool _isDisabled = true;
        private bool _itemsChangedSinceLastCheck = true;
        private string _msBuildProjectFullPath;
        private string _markerFile;
        private HashSet<string> _imports = new HashSet<string>();
        private HashSet<string> _itemTypes = new HashSet<string>();
        private Dictionary<string, HashSet<string>> _items = new Dictionary<string, HashSet<string>>();
        private HashSet<string> _customInputs = new HashSet<string>();
        private HashSet<string> _customOutputs = new HashSet<string>();
        private HashSet<string> _analyzerReferences = new HashSet<string>();
        private HashSet<string> _compilationReferences = new HashSet<string>();
        private HashSet<string> _copyReferenceInputs = new HashSet<string>();
        private Dictionary<string, HashSet<string>> _outputGroups = new Dictionary<string, HashSet<string>>();

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            Lazy<IFileTimestampCache> fileTimestampCache,
            [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IProjectItemSchemaService projectItemSchemaService,
            ITelemetryService telemetryService)
        {
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _fileTimestampCache = fileTimestampCache;
            _tasksService = tasksService;
            _projectItemSchemaService = projectItemSchemaService;
            _telemetryService = telemetryService;
        }

        protected override void Initialize()
        {
            _link = ProjectDataSources.SyncLinkTo(
                _configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions(new StandardRuleDataflowLinkOptions { RuleNames = ProjectPropertiesSchemas }),
                _configuredProject.Services.ProjectSubscription.ImportTreeSource.SourceBlock.SyncLinkOptions(),
                _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                _configuredProject.Services.OutputGroups.SourceBlock.SyncLinkOptions(),
                _projectItemSchemaService.SourceBlock.SyncLinkOptions(),
                target: new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectImportTreeSnapshot, IProjectSubscriptionUpdate, IImmutableDictionary<string, IOutputGroup>, IProjectItemSchema>>>(e => OnChanged(e)),
                linkOptions: new DataflowLinkOptions { PropagateCompletion = true });
        }

        private void OnProjectChanged(IProjectSubscriptionUpdate e)
        {
            var disableFastUpToDateCheckString = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, null);
            _isDisabled = disableFastUpToDateCheckString != null && string.Equals(disableFastUpToDateCheckString, TrueValue, StringComparison.OrdinalIgnoreCase);

            _msBuildProjectFullPath = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, _msBuildProjectFullPath);

            if (e.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out var changes) &&
                changes.Difference.AnyChanges)
            {
                _analyzerReferences = new HashSet<string>(changes.After.Items.Select(item => item.Value[ResolvedPath]));
            }

            if (e.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out changes) &&
                changes.Difference.AnyChanges)
            {
                _compilationReferences.Clear();
                _copyReferenceInputs.Clear();

                foreach (var item in changes.After.Items)
                {
                    _compilationReferences.Add(item.Value[ResolvedPath]);
                    if (!string.IsNullOrWhiteSpace(item.Value[CopyUpToDateMarker.SchemaName]))
                    {
                        _copyReferenceInputs.Add(item.Value[CopyUpToDateMarker.SchemaName]);
                    }
                    if (!string.IsNullOrWhiteSpace(item.Value[OriginalPath]))
                    {
                        _copyReferenceInputs.Add(item.Value[OriginalPath]);
                    }
                }
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out var inputs) &&
                inputs.Difference.AnyChanges)
            {
                _customInputs = new HashSet<string>(inputs.After.Items.Select(item => item.Value[FullPath]));
            }

            if (e.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out var outputs) &&
                outputs.Difference.AnyChanges)
            {
                _customOutputs = new HashSet<string>(outputs.After.Items.Select(item => item.Value[FullPath]));
            }

            if (e.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out var upToDateMarkers) &&
                upToDateMarkers.Difference.AnyChanges)
            {
                _markerFile = upToDateMarkers.After.Items.Count == 1 ? upToDateMarkers.After.Items.Single().Value[FullPath] : null;
            }
        }

        private void OnProjectImportsChanged(IProjectImportTreeSnapshot e)
        {
            void AddImports(IReadOnlyList<IProjectImportSnapshot> value)
            {
                foreach (var import in value)
                {
                    _imports.Add(import.ProjectPath);
                    AddImports(import.Imports);
                }
            }

            _imports.Clear();
            AddImports(e.Value);
        }

        private void OnSourceItemChanged(IProjectSubscriptionUpdate e, IProjectItemSchema projectItemSchema)
        {
            var itemTypes = new HashSet<string>(projectItemSchema.GetKnownItemTypes().Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput));
            var itemTypesChanged = !_itemTypes.SetEquals(itemTypes);

            if (itemTypesChanged)
            {
                _itemTypes = itemTypes;
                _items = new Dictionary<string, HashSet<string>>();
            }

            foreach (var itemType in e.ProjectChanges.Where(changes => (itemTypesChanged || changes.Value.Difference.AnyChanges) && _itemTypes.Contains(changes.Key)))
            {
                var items = itemType.Value
                    .After.Items
                    .Select(item => item.Value[FullPath]);
                _items[itemType.Key] = new HashSet<string>(items);
                _itemsChangedSinceLastCheck = true;
            }
        }

        private void OnOutputGroupChanged(IImmutableDictionary<string, IOutputGroup> e)
        {
            foreach (var outputGroupPair in e.Where(pair => KnownOutputGroups.Contains(pair.Key)))
            {
                var outputs = outputGroupPair.Value
                    .Outputs
                    .Where(output => !output.Value.ContainsKey(CopyToOutputDirectory)
                        || !string.Equals(output.Value[CopyToOutputDirectory], Never, StringComparison.InvariantCultureIgnoreCase))
                    .Select(output => output.Key);
                _outputGroups[outputGroupPair.Key] = new HashSet<string>(outputs);
            }
        }

        private void OnChanged(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectImportTreeSnapshot, IProjectSubscriptionUpdate, IImmutableDictionary<string, IOutputGroup>, IProjectItemSchema>> e)
        {
            OnProjectChanged(e.Value.Item1);
            OnProjectImportsChanged(e.Value.Item2);
            OnSourceItemChanged(e.Value.Item3, e.Value.Item5);
            OnOutputGroupChanged(e.Value.Item4);
            _lastVersionSeen = e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
        }

        protected override void Dispose(bool disposing)
        {
            _link?.Dispose();
        }

        private DateTime? GetTimestamp(string path, IDictionary<string, DateTime> timestampCache)
        {
            if (!timestampCache.TryGetValue(path, out var time))
            {
                var info = new FileInfo(path);
                if (info.Exists)
                {
                    time = info.LastWriteTimeUtc;
                    timestampCache[path] = time;
                    return time;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return time;
            }
        }

        private void AddInput(Logger logger, HashSet<string> inputs, string path, string description)
        {
            logger.Verbose("Found input {0} '{1}'.", description, path);
            inputs.Add(path);
        }

        private void AddInputs(Logger logger, HashSet<string> inputs, IEnumerable<string> paths, string description)
        {
            foreach (var path in paths)
            {
                AddInput(logger, inputs, path, description);
            }
        }

        private void AddOutput(Logger logger, HashSet<string> outputs, string path, string description)
        {
            logger.Verbose("Found output {0} '{1}'.", description, path);
            outputs.Add(path);
        }

        private void AddOutputs(Logger logger, HashSet<string> outputs, IEnumerable<string> paths, string description)
        {
            foreach (var path in paths)
            {
                AddOutput(logger, outputs, path, description);
            }
        }

        private bool Fail(Logger logger, string message, string reason)
        {
            logger.Info(message);
            _telemetryService.PostProperty($"{TelemetryEventName}/Fail", "Reason", reason);
            return false;
        }

        private bool CheckGlobalConditions(BuildAction buildAction, Logger logger)
        {
            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            var itemsChangedSinceLastCheck = _itemsChangedSinceLastCheck;
            _itemsChangedSinceLastCheck = false;

            if (!_tasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build))
            {
                return Fail(logger, "Critical build tasks are running, skipping check.", "CriticalTasks");
            }

            if (_lastVersionSeen == null || _configuredProject.ProjectVersion.CompareTo(_lastVersionSeen) > 0)
            {
                return Fail(logger, "Project information is older than current project version, skipping check.", "ProjectInfoOutOfDate");
            }

            if (itemsChangedSinceLastCheck)
            {
                return Fail(logger, "The list of source items has changed since the last build.", "ItemInfoOutOfDate");
            }

            if (_isDisabled)
            {
                return Fail(logger, "The 'DisableFastUpToDateCheckProperty' property is true, skipping check.", "Disabled");
            }

            return true;
        }

        private HashSet<string> CollectInputs(Logger logger)
        {
            var inputs = new HashSet<string>();

            AddInput(logger, inputs, _msBuildProjectFullPath, "project file");

            AddInputs(logger, inputs, _imports, "import");

            foreach (var pair in _items)
            {
                AddInputs(logger, inputs, pair.Value, pair.Key);
            }

            AddInputs(logger, inputs, _analyzerReferences, ResolvedAnalyzerReference.SchemaName);
            AddInputs(logger, inputs, _compilationReferences, ResolvedCompilationReference.SchemaName);
            AddInputs(logger, inputs, _customInputs, UpToDateCheckInput.SchemaName);

            return inputs;
        }

        private HashSet<string> CollectOutputs(Logger logger)
        {
            var outputs = new HashSet<string>();

            foreach (var pair in _outputGroups)
            {
                AddOutputs(logger, outputs, pair.Value, pair.Key);
            }

            AddOutputs(logger, outputs, _customOutputs, UpToDateCheckOutput.SchemaName);

            return outputs;
        }

        private (DateTime? time, string path) GetLatestInput(HashSet<string> inputs, IDictionary<string, DateTime> timestampCache, bool ignoreMissing = false)
        {
            DateTime? latest = DateTime.MinValue;
            string latestPath = null;

            foreach (var input in inputs)
            {
                var time = GetTimestamp(input, timestampCache);
                if (latest != null && (time == null && !ignoreMissing || time > latest))
                {
                    latest = time;
                    latestPath = input;
                }
            }

            return (latest, latestPath);
        }

        private (DateTime? time, string path) GetEarliestOutput(HashSet<string> outputs, IDictionary<string, DateTime> timestampCache)
        { 
            DateTime? earliest = DateTime.MaxValue;
            string earliestPath = null;

            foreach (var output in outputs)
            {
                var time = GetTimestamp(output, timestampCache);
                if (earliest != null && (time == null || time < earliest))
                {
                    earliest = time;
                    earliestPath = output;
                }
            }

            return (earliest, earliestPath);
        }

        // Reference assembly copy markers are strange. The property is always going to be present on 
        // references to SDK-based projects, regardless of whether or not those referenced projects 
        // will actually produce a marker. And an item always will be present in an SDK-based project, 
        // regardless of whether or not the project produces a marker. So, basically, we only check 
        // here if the project actually produced a marker and we only check it against references that
        // actually produced a marker.
        private bool CheckMarkers(Logger logger, IDictionary<string, DateTime> timestampCache)
        {
            if (string.IsNullOrWhiteSpace(_markerFile) || !_copyReferenceInputs.Any())
            {
                return true;
            }

            foreach (var referenceMarkerFile in _copyReferenceInputs)
            {
                logger.Verbose("Found possible input marker '{0}'.", referenceMarkerFile);
            }

            logger.Verbose("Found possible output marker '{0}'.", _markerFile);

            var latestInputMarker = GetLatestInput(_copyReferenceInputs, timestampCache, true);
            var outputMarkerTime = GetTimestamp(_markerFile, timestampCache);

            if (latestInputMarker.path != null)
            {
                logger.Info("Latest write timestamp on input marker is {0} on '{1}'.", latestInputMarker.time.Value, latestInputMarker.path);
            }
            else
            {
                logger.Info("No input markers exist, skipping marker check.");
            }

            if (outputMarkerTime != null)
            {
                logger.Info("Write timestamp on output marker is {0} on '{1}'.", outputMarkerTime, _markerFile);
            }
            else
            {
                logger.Info("Output marker '{0}' does not exist, skipping marker check.", _markerFile);
            }

            return latestInputMarker.path == null || outputMarkerTime == null || outputMarkerTime > latestInputMarker.time;
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logWriter, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureInitialized();

            var requestedLogLevel = await _projectSystemOptions.GetFastUpToDateLoggingLevelAsync().ConfigureAwait(false);
            var logger = new Logger(logWriter, requestedLogLevel, _configuredProject.UnconfiguredProject.FullPath);

            if (!CheckGlobalConditions(buildAction, logger))
            {
                return false;
            }

            var timestampCache = _fileTimestampCache.Value.TimestampCache ??
                    new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var latestInput = GetLatestInput(CollectInputs(logger), timestampCache);
            var earliestOutput = GetEarliestOutput(CollectOutputs(logger), timestampCache);
            
            if (latestInput.time != null)
            {
                logger.Info("Latest write timestamp on input is {0} on '{1}'.", latestInput.time.Value, latestInput.path);
            }
            else
            {
                logger.Info("Input '{0}' does not exist.", latestInput.path);
            }

            if (earliestOutput.time != null)
            {
                logger.Info("Earliest write timestamp on output is {0} on '{1}'.", earliestOutput.time.Value, earliestOutput.path);
            }
            else
            {
                logger.Info("Output '{0}' does not exist.", earliestOutput.path);
            }

            // We are up to date if the earliest output write happened after the latest input write
            var markersUpToDate = CheckMarkers(logger, timestampCache);
            var outputsUpToDate = latestInput.time != null && earliestOutput.time != null && earliestOutput.time > latestInput.time;
            var isUpToDate = outputsUpToDate && markersUpToDate;

            if (!markersUpToDate)
            {
                _telemetryService.PostProperty($"{TelemetryEventName}/Fail", "Reason", "Marker");
            }
            else if (!outputsUpToDate)
            {
                _telemetryService.PostProperty($"{TelemetryEventName}/Fail", "Reason", "Outputs");
            }
            else
            {
                _telemetryService.PostEvent($"{TelemetryEventName}/Success");
            }

            logger.Info("Project is{0} up to date.", !isUpToDate ? " not" : "");

            return isUpToDate;
        }

        public async Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
            await _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync().ConfigureAwait(false);
    }
}
