// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.ProjectSystem.References;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    internal class BuildUpToDateCheck : IBuildUpToDateCheckProvider
    {
        private const string DisableFastUpToDateCheckProperty = "DisableFastUpToDateCheck";

        private static string[] KnownOutputGroups = { "Symbols", "Built", "ContentFiles", "Documentation", "LocalizedResourceDlls" };

        private readonly IProjectLockService _projectLockService;
        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly IProjectLogger _projectLogger;
        private readonly IProjectItemSchemaService _projectItemsSchema;
        private readonly Lazy<IFileTimestampCache> _fileTimestampCache;

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectLockService projectLockService,
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            IProjectLogger projectLogger,
            [Import(AllowDefault = true)] IProjectItemSchemaService projectItemSchema,
            [Import(AllowDefault = true)] Lazy<IFileTimestampCache> fileTimestampCache)
        {
            _projectLockService = projectLockService;
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _projectLogger = projectLogger;
            _projectItemsSchema = projectItemSchema;
            _fileTimestampCache = fileTimestampCache;
        }

        private void Log(string traceMessage) => _projectLogger.WriteLine($"FastUpToDate: {traceMessage}");

        private DateTime GetLatestTimestamp(List<string> paths, IDictionary<string, DateTime> timestampCache)
        {
            var latestTime = DateTime.MinValue;

            foreach (var path in paths)
            {
                if (!timestampCache.TryGetValue(path, out var time))
                {
                    if (File.Exists(path))
                    {
                        time = File.GetLastWriteTimeUtc(path);
                        timestampCache[path] = time;
                        Log($"Path '{path}' had live timestamp '{time}'.");
                    }
                    else
                    {
                        Log($"Path '{path}' did not exist.");
                    }
                }
                else
                {
                    Log($"Path '{path}' had cached timestamp '{time}'.");
                }

                if (latestTime < time)
                {
                    latestTime = time;
                }
            }

            return latestTime;
        }

        async Task CheckReferencesAsync<TUnresolvedReference, TResolvedReference>(string name, IResolvableReferencesService<TUnresolvedReference, TResolvedReference> service, List<string> inputs)
            where TUnresolvedReference : IProjectItem, TResolvedReference
            where TResolvedReference : class, IReference
        {
            Log($"Checking reference type '{name}'.");
            if (service != null)
            {
                foreach (var resolvedReference in await service.GetResolvedReferencesAsync())
                {
                    var fullPath = await resolvedReference.GetFullPathAsync();
                    Log($"Adding input reference ${fullPath}");
                    inputs.Add(fullPath);
                }
            }
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            using (var access = await _projectLockService.ReadLockAsync(cancellationToken))
            {
                var project = await access.GetProjectAsync(_configuredProject, cancellationToken);

                Log($"Starting check for project '{project.FullPath}'.");

                var timestampCache = _fileTimestampCache != null ? _fileTimestampCache.Value.TimestampCache : new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

                if (_projectItemsSchema == null)
                {
                    Log($"Skipping because '{typeof(IProjectItemSchemaService).FullName}' component could not be found.");
                    Report.IfNotPresent(_projectItemsSchema);
                    return false;
                }

                if (!string.IsNullOrEmpty(project.GetPropertyValue(DisableFastUpToDateCheckProperty)))
                {
                    Log($"Disabled because the '{DisableFastUpToDateCheckProperty}' property is set to a non-empty value.");
                    return false;
                }

                var inputs = new List<string>();

                // add the project file
                if (!string.IsNullOrEmpty(_configuredProject.UnconfiguredProject.FullPath))
                {
                    inputs.Add(project.FullPath);
                    Log($"Adding input project file {project.FullPath}.");
                }

                // add all project items (generally items seen in solution explorer) that are not excluded from UpToDate check
                // Skip items that are marked as excluded from build.
                var projectItemSchemaValue = (await _projectItemsSchema.GetSchemaAsync(cancellationToken)).Value;
                var itemTypes = projectItemSchemaValue
                    .GetKnownItemTypes()
                    .Select(name => projectItemSchemaValue.GetItemType(name))
                    .Where(item => item != null && item.UpToDateCheckInput && !string.Equals(item.Name, "None", StringComparison.OrdinalIgnoreCase));

                foreach (var itemType in itemTypes)
                {
                    Log($"Checking known item type '{itemType.Name}'.");

                    var items = project.GetItems(itemType.Name)
                            .Where(item => !string.Equals(item.GetMetadataValue("ExcludedFromBuild"), "true", StringComparison.OrdinalIgnoreCase))
                            .Select(item => item.GetMetadataValue("FullPath"));

                    foreach (var item in items)
                    {
                        Log($"Input item path '{item}'.");
                        inputs.Add(item);
                    }
                }

                await CheckReferencesAsync(nameof(_configuredProject.Services.AssemblyReferences), _configuredProject.Services.AssemblyReferences, inputs);
                await CheckReferencesAsync(nameof(_configuredProject.Services.ComReferences), _configuredProject.Services.ComReferences, inputs);
                await CheckReferencesAsync(nameof(_configuredProject.Services.PackageReferences), _configuredProject.Services.PackageReferences, inputs);
                await CheckReferencesAsync(nameof(_configuredProject.Services.ProjectReferences), _configuredProject.Services.ProjectReferences, inputs);
                await CheckReferencesAsync(nameof(_configuredProject.Services.SdkReferences), _configuredProject.Services.SdkReferences, inputs);
                await CheckReferencesAsync(nameof(_configuredProject.Services.WinRTReferences), _configuredProject.Services.WinRTReferences, inputs);

                // UpToDateCheckInput is the special item group for customized projects to add explicit inputs
                var upToDateCheckInputItems = project.GetItems("UpToDateCheckInput").Select(file => file.GetMetadataValue("FullPath"));

                Log("Checking item type 'UpToDateCheckInput'.");
                foreach (var upToDateCheckInputItem in upToDateCheckInputItems)
                {
                    Log($"Input item path '{upToDateCheckInputItem}'.");
                    inputs.Add(upToDateCheckInputItem);
                }

                var latestInput = GetLatestTimestamp(inputs, timestampCache);
                Log($"Lastest write timestamp on input is {latestInput}.");

                var outputs = new List<string>();

                outputs.AddRange(project.GetItems("UpToDateCheckOutput")
                    .Select(file => file.GetMetadataValue("FullPath")));

                IOutputGroupsService outputGroupsService = _configuredProject.Services.OutputGroups;
                foreach (var outputGroup in KnownOutputGroups)
                {
                    Log($"Checking known output group {outputGroup}.");

                    foreach (var output in (await outputGroupsService.GetOutputGroupAsync(outputGroup, cancellationToken)).Outputs)
                    {
                        Log($"Output path '{output.Key}'.");
                        outputs.Add(output.Key);
                    }
                }

                var latestOutput = GetLatestTimestamp(outputs, timestampCache);
                Log($"Lastest write timestamp on output is {latestOutput}.");

                var isUpToDate = latestOutput >= latestInput;
                Log($"Project is{(!isUpToDate ? " not" : "")} up to date.");

                return isUpToDate;
            }
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
            Task.FromResult(!_projectSystemOptions.IsFastUpToDateCheckDisabled);
    }
}
