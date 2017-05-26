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

namespace Microsoft.VisualStudio.ProjectSystem
{
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    internal sealed class BuildUpToDateCheck : OnceInitializedOnceDisposed, IBuildUpToDateCheckProvider
    {
        private const string TrueValue = "true";
        private const string FullPath = "FullPath";
        private const string ResolvedPath = "ResolvedPath";

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
            .Add(ResolvedAssemblyReference.SchemaName)
            .Add(ResolvedCOMReference.SchemaName)
            .Add(ResolvedProjectReference.SchemaName);

        private static ImmutableHashSet<string> ItemSchemas => ImmutableHashSet<string>.Empty
            .Add(AdditionalFiles.SchemaName)
            .Add(Compile.SchemaName)
            .Add(Content.SchemaName)
            .Add(EmbeddedResource.SchemaName)
            .Add(None.SchemaName);

        private static ImmutableHashSet<string> ProjectPropertiesSchemas => ImmutableHashSet<string>.Empty
            .Add(ConfigurationGeneral.SchemaName)
            .Union(ReferenceSchemas);

        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly Lazy<IFileTimestampCache> _fileTimestampCache;

        private IDisposable _link;
        private IComparable _lastVersionSeen;

        private bool _isDisabled = true;
        private string _msBuildProjectFullPath;
        private HashSet<string> _imports = new HashSet<string>();
        private Dictionary<string, HashSet<string>> _items = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> _references = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> _outputGroups = new Dictionary<string, HashSet<string>>();

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            Lazy<IFileTimestampCache> fileTimestampCache)
        {
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _fileTimestampCache = fileTimestampCache;
        }

        protected override void Initialize()
        {
            _link = ProjectDataSources.SyncLinkTo(
                _configuredProject.Services.ProjectSubscription.JointRuleSource.SourceBlock.SyncLinkOptions(new StandardRuleDataflowLinkOptions { RuleNames = ProjectPropertiesSchemas }),
                _configuredProject.Services.ProjectSubscription.ImportTreeSource.SourceBlock.SyncLinkOptions(),
                _configuredProject.Services.ProjectSubscription.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(new StandardRuleDataflowLinkOptions { RuleNames = ItemSchemas }),
                _configuredProject.Services.OutputGroups.SourceBlock.SyncLinkOptions(),
                target: new ActionBlock<IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectImportTreeSnapshot, IProjectSubscriptionUpdate, IImmutableDictionary<string, IOutputGroup>>>>(e => OnChanged(e)),
                linkOptions: new DataflowLinkOptions { PropagateCompletion = true });
        }

        private void OnProjectChanged(IProjectSubscriptionUpdate e)
        {
            var disableFastUpToDateCheckString = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, null);
            if (disableFastUpToDateCheckString != null)
            {
                _isDisabled = string.Equals(disableFastUpToDateCheckString, TrueValue, StringComparison.OrdinalIgnoreCase);
            }

            _msBuildProjectFullPath = e.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, _msBuildProjectFullPath);

            foreach (var referenceSchema in ReferenceSchemas)
            {
                if (e.CurrentState.TryGetValue(referenceSchema, out var snapshot))
                {
                    _references[referenceSchema] = new HashSet<string>(snapshot.Items.Select(item => item.Value[ResolvedPath]));
                }
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

        private void OnSourceItemChanged(IProjectSubscriptionUpdate e)
        {
            foreach (var itemType in e.ProjectChanges)
            {
                _items[itemType.Key] = new HashSet<string>(itemType.Value.After.Items.Select(item => item.Value[FullPath]));
            }
        }

        private void OnOutputGroupChanged(IImmutableDictionary<string, IOutputGroup> e)
        {
            foreach (var outputGroupPair in e.Where(pair => KnownOutputGroups.Contains(pair.Key)))
            {
                _outputGroups[outputGroupPair.Key] = new HashSet<string>(outputGroupPair.Value.Outputs.Select(output => output.Key));
            }
        }

        private void OnChanged(IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectImportTreeSnapshot, IProjectSubscriptionUpdate, IImmutableDictionary<string, IOutputGroup>>> e)
        {
            OnProjectChanged(e.Value.Item1);
            OnProjectImportsChanged(e.Value.Item2);
            OnSourceItemChanged(e.Value.Item3);
            OnOutputGroupChanged(e.Value.Item4);
            _lastVersionSeen = e.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
        }

        protected override void Dispose(bool disposing)
        {
            _link?.Dispose();
        }

        private DateTime? GetTimestamp(TextWriter logger, string path, IDictionary<string, DateTime> timestampCache)
        {
            if (!timestampCache.TryGetValue(path, out var time))
            {
                var info = new FileInfo(path);
                if (info.Exists)
                {
                    time = info.LastWriteTimeUtc;
                    timestampCache[path] = time;
                    Log(logger, "Path '{0}' had live timestamp '{1}'.", path, time);
                    return time;
                }
                else
                {
                    Log(logger, "Path '{0}' did not exist.", path);
                    return null;
                }
            }
            else
            {
                Log(logger, "Path '{0}' had cached timestamp '{1}'.", path, time);
                return time;
            }
        }

        private void Log(TextWriter logger, string message, params object[] values) =>
            logger?.WriteLine("FastUpToDate [" + _configuredProject.UnconfiguredProject.FullPath + "]: " + string.Format(message, values));

        private void AddInput(TextWriter logger, HashSet<string> inputs, string path, string description)
        {
            Log(logger, "Found input {0} '{1}'.", description, path);
            inputs.Add(path);
        }

        private void AddInputs(TextWriter logger, HashSet<string> inputs, IEnumerable<string> paths, string description)
        {
            foreach (var path in paths)
            {
                AddInput(logger, inputs, path, description);
            }
        }

        private void AddOutput(TextWriter logger, HashSet<string> outputs, string path, string description)
        {
            Log(logger, "Found output {0} '{1}'.", description, path);
            outputs.Add(path);
        }

        private void AddOutputs(TextWriter logger, HashSet<string> outputs, IEnumerable<string> paths, string description)
        {
            foreach (var path in paths)
            {
                AddOutput(logger, outputs, path, description);
            }
        }

        private bool CheckGlobalConditions(BuildAction buildAction, TextWriter logger)
        {
            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            if (_lastVersionSeen == null || _configuredProject.ProjectVersion.CompareTo(_lastVersionSeen) > 0)
            {
                Log(logger, "Project information is older than current project version, skipping check.");
                return false;
            }

            if (_isDisabled)
            {
                Log(logger, "The 'DisableFastUpToDateCheckProperty' property is true, skipping check.");
                return false;
            }

            return true;
        }

        private HashSet<string> CollectInputs(TextWriter logger)
        {
            var inputs = new HashSet<string>();

            AddInput(logger, inputs, _msBuildProjectFullPath, "project file");
            AddInputs(logger, inputs, _imports, "import");

            foreach (var pair in _items)
            {
                AddInputs(logger, inputs, pair.Value, pair.Key);
            }

            foreach (var pair in _references)
            {
                AddInputs(logger, inputs, pair.Value, pair.Key);
            }

            return inputs;
        }

        private HashSet<string> CollectOutputs(TextWriter logger)
        {
            var outputs = new HashSet<string>();

            foreach (var pair in _outputGroups)
            {
                AddOutputs(logger, outputs, pair.Value, pair.Key);
            }

            return outputs;
        }

        private DateTime? GetLatestInput(TextWriter logger, HashSet<string> inputs, IDictionary<string, DateTime> timestampCache)
        {
            DateTime? latest = DateTime.MinValue;

            foreach (var input in inputs)
            {
                var time = GetTimestamp(logger, input, timestampCache);
                if (latest != null && (time == null || time > latest))
                {
                    latest = time;
                }
            }

            return latest;
        }

        private DateTime? GetEarliestOutput(TextWriter logger, HashSet<string> outputs, IDictionary<string, DateTime> timestampCache)
        { 
            DateTime? earliest = DateTime.MaxValue;

            foreach (var output in outputs)
            {
                var time = GetTimestamp(logger, output, timestampCache);
                if (earliest != null && (time == null || time < earliest))
                {
                    earliest = time;
                }
            }

            return earliest;
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureInitialized();

            if (!await _projectSystemOptions.GetVerboseFastUpToDateLoggingAsync().ConfigureAwait(false))
            {
                logger = null;
            }

            if (!CheckGlobalConditions(buildAction, logger))
            {
                return false;
            }

            var timestampCache = _fileTimestampCache.Value.TimestampCache ??
                    new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var latestInput = GetLatestInput(logger, CollectInputs(logger), timestampCache);
            var earliestOutput = GetEarliestOutput(logger, CollectOutputs(logger), timestampCache);

            if (latestInput != null)
            {
                Log(logger, "Lastest write timestamp on input is {0}.", latestInput.Value);
            }
            else
            {
                Log(logger, "One or more inputs do not exist.");
            }

            if (earliestOutput != null)
            {
                Log(logger, "Earliest write timestamp on output is {0}.", earliestOutput.Value);
            }
            else
            {
                Log(logger, "One or more outputs do not exist.");
            }

            // We are up to date if the earliest output write happened before the latest input write
            var isUpToDate = latestInput != null && earliestOutput != null && earliestOutput > latestInput;
            Log(logger, "Project is{0} up to date.", (!isUpToDate ? " not" : ""));

            return isUpToDate;
        }

        public async Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
            await _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync().ConfigureAwait(false);
    }
}
