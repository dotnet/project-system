// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Migration report holder. Copied from
    /// https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectJsonMigration/Report/ProjectMigrationReport.cs
    /// </summary>
    internal class ProjectMigrationReport
    {
        [JsonProperty]
        public string ProjectDirectory { get; private set; }

        [JsonProperty]
        public string ProjectName { get; private set; }

        [JsonProperty]
        public string OutputMSBuildProject { get; private set; }

        [JsonProperty]
        public IReadOnlyList<MigrationError> Errors { get; private set; }

        [JsonProperty]
        public IReadOnlyList<string> Warnings { get; private set; }

        [JsonProperty]
        public IReadOnlyList<string> PreExistingCsprojDependencies { get; private set; }

        [JsonProperty]
        public bool Skipped { get; private set; }

        [JsonProperty]
        public bool Failed { get; private set; }

        [JsonProperty]
        public bool Succeeded { get; private set; }

        public ProjectMigrationReport() { }

        public ProjectMigrationReport(bool succeeded, string outputMSBuildProject, string projectDirectory, string projectName, List<string> warnings, List<MigrationError> errors)
        {
            Succeeded = succeeded;
            Failed = !succeeded;
            ProjectDirectory = projectDirectory;
            ProjectName = projectName;
            Warnings = warnings;
            Errors = errors;
            OutputMSBuildProject = outputMSBuildProject;
        }

        public override bool Equals(object obj)
        {
            if (obj is ProjectMigrationReport report)
            {
                return Equals(report.ProjectDirectory, ProjectDirectory) &&
                    Equals(report.ProjectName, ProjectName) &&
                    Equals(report.OutputMSBuildProject, OutputMSBuildProject) &&
                    (report.Errors?.SequenceEqual(Errors) ?? Errors == null) &&
                    (report.Warnings?.SequenceEqual(Warnings) ?? Warnings == null) &&
                    (report.PreExistingCsprojDependencies?.SequenceEqual(PreExistingCsprojDependencies) ?? PreExistingCsprojDependencies == null) &&
                    report.Skipped == Skipped &&
                    report.Failed == Failed &&
                    report.Succeeded == Succeeded;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = ProjectDirectory?.GetHashCode() ?? 1 * 23;
            hash += (ProjectName?.GetHashCode() ?? 1 * 23);
            hash += (OutputMSBuildProject?.GetHashCode() ?? 1 * 23);
            hash += (Errors?.Sum(e => e.GetHashCode() * 17) ?? 1 * 23);
            hash += (Warnings?.Sum(w => w.GetHashCode() * 17) ?? 1 * 23);
            hash += (PreExistingCsprojDependencies?.Sum(p => p.GetHashCode() * 17) ?? 1 * 23);
            hash += (Skipped.GetHashCode() * 23);
            hash += (Failed.GetHashCode() * 23);
            hash += (Succeeded.GetHashCode() * 23);
            return hash;
        }
    }
}
