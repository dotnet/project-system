// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
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

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        private IProjectLockService ProjectLockService { get; set; }

        [Import(RequiredCreationPolicy = CreationPolicy.Shared)]
        private ConfiguredProject ConfiguredProject { get; set; }

        [Import(RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = true)]
        private IProjectItemSchemaService ProjectItemsSchema { get; set; }

        [Import(AllowDefault = true)]
        private Lazy<IFileTimestampCache> FileTimestampCache { get; set; }

        public async Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var timestampCache = FileTimestampCache != null ? FileTimestampCache.Value.TimestampCache : new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            if (buildAction != BuildAction.Build)
            {
                return false;
            }

            using (var access = await ProjectLockService.ReadLockAsync())
            {
                var configuredProject = await access.GetProjectAsync(ConfiguredProject);

                if (ProjectItemsSchema == null)
                {
                    TraceUtilities.TraceVerbose("Project '{0}' up to date check disabled because the '{1}' component could not be found.", configuredProject.FullPath, typeof(IProjectItemSchemaService).FullName);
                    Report.IfNotPresent(ProjectItemsSchema);
                    return false;
                }

                if (!String.IsNullOrEmpty(configuredProject.GetPropertyValue(DisableFastUpToDateCheckProperty)))
                {
                    TraceUtilities.TraceVerbose("Project '{0}' up to date check disabled because the '{1}' property is set to a non-empty value.", configuredProject.FullPath, DisableFastUpToDateCheckProperty);
                    return false;
                }

                List<string> allInputs = new List<string>();

                // add the project file
                if (!String.IsNullOrEmpty(configuredProject.FullPath))
                {
                    allInputs.Add(configuredProject.FullPath);
                }

                // add all imports except .user file - the debugger properties it contains should not affect the build
                IList<ResolvedImport> imports = configuredProject.Imports;
                string userFilePath = configuredProject.FullPath + ".user";

                allInputs.AddRange(imports.Select(i => i.ImportedProject.FullPath).Where(p => !String.IsNullOrEmpty(p) && !String.Equals(p, userFilePath, StringComparison.OrdinalIgnoreCase)));

                // add all project items (generally items seen in solution explorer) that are not excluded from UpToDate check
                // Skip items that are marked as excluded from build.
                var projectItemSchemaValue = (await ProjectItemsSchema.GetSchemaAsync()).Value;
                allInputs.AddRange(
                    projectItemSchemaValue
                    .GetKnownItemTypes()
                    .Select(name => projectItemSchemaValue.GetItemType(name))
                    .Where(item => item != null && item.UpToDateCheckInput && !String.Equals(item.Name, "None", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(item => configuredProject
                        .GetItems(item.Name)
                        .Where(f => !String.Equals(f.GetMetadataValue("ExcludedFromBuild"), "true", StringComparison.OrdinalIgnoreCase))
                        .Select(f => f.GetMetadataValue("FullPath"))));

                // UpToDateCheckInput is the special item group for customized projects to add explicit inputs
                allInputs.AddRange(configuredProject.GetItems("UpToDateCheckInput").Select(file => file.GetMetadataValue("FullPath")));

                // ensure no project item has been modified after last successful build finished
                foreach (string path in allInputs)
                {
                    //string pathUpper = path.ToUpperInvariant();
                    //if (buildInputs.FileIsExcludedFromDependencyCheck(pathUpper))
                    //{
                    //    TraceUtilities.TraceVerbose("Project '{0}' file '{1}' ignored since it is in the exclude path list.", configuredProject.FullPath, pathUpper);
                    //    continue;
                    //}

                    //DateTime lastWriteUtc = buildInputs.GetLastWriteTimeUtc(pathUpper);

                    //if (lastWriteUtc == DateTime.MinValue)
                    //{
                    //    TraceUtilities.TraceVerbose("Project '{0}' not up to date because build input '{1}' is missing.", configuredProject.FullPath, pathUpper);
                    //    OutputUpToDateMessage(logger, configuredProject.GetPropertyValue("ProjectName"), Strings.UpToDateMissingInput, path);
                    //    return false;
                    //}

                    //if (lastWriteUtc > lastBuildFinishedUtc)
                    //{
                    //    TraceUtilities.TraceVerbose(
                    //        String.Format(
                    //            CultureInfo.InvariantCulture,
                    //            "Project '{0}' not up to date because '{1}' was touched on {2}, which is after the last build on {3}.",
                    //            configuredProject.FullPath,
                    //            pathUpper,
                    //            lastWriteUtc.ToLocalTime(),
                    //            lastBuildFinishedUtc.ToLocalTime()));
                    //    return false;
                    //}
                }

                // all checks passed so project is up to date
                return true;
            }
        }

        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }
    }
}
