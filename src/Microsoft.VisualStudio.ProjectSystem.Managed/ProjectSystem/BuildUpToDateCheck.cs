// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.References;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    [Export(typeof(IBuildUpToDateCheckProvider))]
    internal class BuildUpToDateCheck : IBuildUpToDateCheckProvider
    {
        private static string[] KnownOutputGroups = { "Symbols", "Built", "ContentFiles", "Documentation", "LocalizedResourceDlls" };

        private readonly IProjectLockService _projectLockService;
        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ConfiguredProject _configuredProject;
        private readonly ProjectProperties _projectProperties;
        private readonly IProjectItemProvider _projectItemProvider;
        private readonly IProjectItemSchemaService _projectItemsSchema;
        private readonly Lazy<IFileTimestampCache> _fileTimestampCache;

        [ImportingConstructor]
        public BuildUpToDateCheck(
            IProjectLockService projectLockService,
            IProjectSystemOptions projectSystemOptions,
            ConfiguredProject configuredProject,
            ProjectProperties projectProperties,
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider projectItemProvider,
            IProjectItemSchemaService projectItemSchema,
            Lazy<IFileTimestampCache> fileTimestampCache)
        {
            _projectLockService = projectLockService;
            _projectSystemOptions = projectSystemOptions;
            _configuredProject = configuredProject;
            _projectProperties = projectProperties;
            _projectItemProvider = projectItemProvider;
            _projectItemsSchema = projectItemSchema;
            _fileTimestampCache = fileTimestampCache;
        }

        private void Log(TextWriter logger, string message, params object[] values) => logger?.WriteLine("FastUpToDate: " + string.Format(message, values));

        private DateTime? GetLatestTimestamp(TextWriter logger, IEnumerable<string> paths, IDictionary<string, DateTime> timestampCache)
        {
            var latestTime = DateTime.MinValue;

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
                }

                if (latestTime < time)
                {
                    latestTime = time;
                }
            }

            return latestTime;
        }

        private async Task CheckReferencesAsync<TUnresolvedReference, TResolvedReference>(TextWriter logger, string name, IResolvableReferencesService<TUnresolvedReference, TResolvedReference> service, HashSet<string> inputs)
            where TUnresolvedReference : IProjectItem, TResolvedReference
            where TResolvedReference : class, IReference
        {
            if (service != null)
            {
                Log(logger, "Checking reference type '{0}'.", name);

                foreach (var resolvedReference in await service.GetResolvedReferencesAsync())
                {
                    var fullPath = await resolvedReference.GetFullPathAsync();
                    Log(logger, "Adding input reference '{0}'.", fullPath);
                    inputs.Add(fullPath);
                }
            }
        }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            EventHandler designTimeBuildNotifier = (sender, e) => { Log(logger, "Design time build started!"); };
            cancellationToken.ThrowIfCancellationRequested();

            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            if (!await _projectSystemOptions.GetVerboseFastUpToDateLoggingAsync())
            {
                logger = null;
            }

            using (var access = await _projectLockService.ReadLockAsync(cancellationToken))
            {
                var project = await access.GetProjectAsync(_configuredProject, cancellationToken);
                var timestampCache = _fileTimestampCache != null ? _fileTimestampCache.Value.TimestampCache : new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

                Log(logger, "Starting check for project '{0}'.", project.FullPath);

                ConfigurationGeneral general = await _projectProperties.GetConfigurationGeneralPropertiesAsync();

                if ((bool?)await general.DisableFastUpToDateCheck.GetValueAsync() == true)
                {
                    Log(logger, "Disabled because the 'DisableFastUpToDateCheckProperty' property is true.");
                    return false;
                }

                var inputs = new HashSet<string>();

                // add the project file
                if (!string.IsNullOrEmpty(_configuredProject.UnconfiguredProject.FullPath))
                {
                    inputs.Add(project.FullPath);
                    Log(logger, "Adding input project file {0}.", project.FullPath);
                }

                IEnumerable<string> imports = project.Imports.Select(i => i.ImportedProject.FullPath);
                foreach (var import in imports)
                {
                    Log(logger, "Adding input project import {0}.", import);
                    inputs.Add(import);
                }

                var projectItemSchemaValue = (await _projectItemsSchema.GetSchemaAsync(cancellationToken)).Value;

                foreach (var item in await _projectItemProvider.GetItemsAsync())
                {
                    var itemType = projectItemSchemaValue.GetItemType(item);
                    if (itemType != null && itemType.UpToDateCheckInput)
                    {
                        var path = item.EvaluatedIncludeAsFullPath;
                        Log(logger, "Input item type '{0}' path '{1}'.", itemType.Name, path);
                        inputs.Add(path);
                    }
                }

                await CheckReferencesAsync(logger, nameof(_configuredProject.Services.AssemblyReferences), _configuredProject.Services.AssemblyReferences, inputs);
                await CheckReferencesAsync(logger, nameof(_configuredProject.Services.ComReferences), _configuredProject.Services.ComReferences, inputs);
                await CheckReferencesAsync(logger, nameof(_configuredProject.Services.PackageReferences), _configuredProject.Services.PackageReferences, inputs);
                await CheckReferencesAsync(logger, nameof(_configuredProject.Services.ProjectReferences), _configuredProject.Services.ProjectReferences, inputs);
                await CheckReferencesAsync(logger, nameof(_configuredProject.Services.SdkReferences), _configuredProject.Services.SdkReferences, inputs);
                await CheckReferencesAsync(logger, nameof(_configuredProject.Services.WinRTReferences), _configuredProject.Services.WinRTReferences, inputs);

                // UpToDateCheckInput is the special item group for customized projects to add explicit inputs
                var upToDateCheckInputItems = project.GetItems("UpToDateCheckInput").Select(file => file.GetMetadataValue("FullPath"));

                Log(logger, "Checking item type 'UpToDateCheckInput'.");
                foreach (var upToDateCheckInputItem in upToDateCheckInputItems)
                {
                    Log(logger, "Input item path '{0}'.", upToDateCheckInputItem);
                    inputs.Add(upToDateCheckInputItem);
                }

                var latestInput = GetLatestTimestamp(logger, inputs, timestampCache);
                if (latestInput != null)
                {
                    Log(logger, "Lastest write timestamp on input is {0}.", latestInput.Value);
                }

                var outputs = new HashSet<string>();

                outputs.AddRange(project.GetItems("UpToDateCheckOutput")
                    .Select(file => file.GetMetadataValue("FullPath")));

                IOutputGroupsService outputGroupsService = _configuredProject.Services.OutputGroups;
                foreach (var outputGroup in KnownOutputGroups)
                {
                    Log(logger, "Checking known output group {0}.", outputGroup);

                    foreach (var output in (await outputGroupsService.GetOutputGroupAsync(outputGroup, cancellationToken)).Outputs)
                    {
                        Log(logger, "Output path '{0}'.", output.Key);
                        outputs.Add(output.Key);
                    }
                }

                var latestOutput = GetLatestTimestamp(logger, outputs, timestampCache);
                if (latestOutput != null)
                {
                    Log(logger, "Lastest write timestamp on output is {0}.", latestOutput.Value);
                }

                var isUpToDate = latestOutput != null && latestInput != null && latestOutput.Value >= latestInput.Value;
                Log(logger, "Project is{0} up to date.", (!isUpToDate ? " not" : ""));

                return isUpToDate;
            }
        }

        public async Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
            await _projectSystemOptions.GetIsFastUpToDateCheckEnabledAsync();
    }
}
