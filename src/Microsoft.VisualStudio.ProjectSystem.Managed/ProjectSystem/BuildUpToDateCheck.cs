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
    internal sealed class BuildUpToDateCheck : IBuildUpToDateCheckProvider, IDisposable
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
            if (e.ProjectChanges.TryGetValue(ConfigurationGeneral.SchemaName, out var projectChange))
            {
                if (projectChange.After.Properties.TryGetValue(ConfigurationGeneral.DisableFastUpToDateCheckProperty, out var disableFastUpToDateCheckString))
                {
                    _isDisabled = string.Equals(disableFastUpToDateCheckString, TrueValue, StringComparison.OrdinalIgnoreCase);
                }

                projectChange.After.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectFullPathProperty, out _msBuildProjectFullPath);
            }

            foreach (var referenceSchema in ReferenceSchemas)
            {
                if (e.ProjectChanges.TryGetValue(referenceSchema, out projectChange))
                {
                    _references[referenceSchema] = new HashSet<string>(projectChange.After.Items.Select(item => item.Value[ResolvedPath]));
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

        public void Dispose()
        {
            _link?.Dispose();
        }

        private bool GetTimestamps(TextWriter logger, IEnumerable<string> paths, Action<DateTime> consume, IDictionary<string, DateTime> timestampCache)
        {
            var allPathsExist = true;

            foreach (var path in paths)
            {
                if (!timestampCache.TryGetValue(path, out var time))
                {
                    var info = new FileInfo(path);
                    if (info.Exists)
                    {
                        time = info.LastWriteTimeUtc;
                        timestampCache[path] = time;
                        Log(logger, "Path '{0}' had live timestamp '{1}'.", path, time);
                        consume(time);
                    }
                    else
                    {
                        Log(logger, "Path '{0}' did not exist.", path);
                        allPathsExist = false;
                    }
                }
                else
                {
                    Log(logger, "Path '{0}' had cached timestamp '{1}'.", path, time);
                    consume(time);
                }
            }

            return allPathsExist;
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

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            if (!await _projectSystemOptions.GetVerboseFastUpToDateLoggingAsync().ConfigureAwait(false))
            {
                logger = null;
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

            var timestampCache = 
                _fileTimestampCache != null 
                    ? _fileTimestampCache.Value.TimestampCache 
                    : new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var inputs = new HashSet<string>();
            var outputs = new HashSet<string>();

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

            foreach (var pair in _outputGroups)
            {
                AddOutputs(logger, outputs, pair.Value, pair.Key);
            }

            var latestInput = DateTime.MinValue;
            var allInputsExist = GetTimestamps(logger, inputs, time =>
            {
                if (time > latestInput)
                {
                    latestInput = time;
                }
            }, timestampCache);

            var earliestOutput = DateTime.MaxValue;
            var allOutputsExist = GetTimestamps(logger, outputs, time =>
            {
                if (time < earliestOutput)
                {
                    earliestOutput = time;
                }
            }, timestampCache);

            if (allInputsExist)
            {
                Log(logger, "Lastest write timestamp on input is {0}.", latestInput);
            }
            else
            {
                Log(logger, "One or more inputs do not exist.");
            }

            if (allOutputsExist)
            {
                Log(logger, "Earliest write timestamp on output is {0}.", earliestOutput);
            }
            else
            {
                Log(logger, "One or more outputs do not exist.");
            }

            // We are up to date if the earliest output write happened before the latest input write
            var isUpToDate = allInputsExist && allOutputsExist && earliestOutput > latestInput;
            Log(logger, "Project is{0} up to date.", (!isUpToDate ? " not" : ""));

            return isUpToDate;
        }

        public async Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
            await _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync().ConfigureAwait(false);
    }
}
