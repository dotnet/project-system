// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Migration report holder. Copied from
    /// https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectJsonMigration/Report/ProjectMigrationReport.cs
    /// </summary>
    internal class ProjectMigrationReport
    {
        public string ProjectDirectory { get; set; }

        public string ProjectName { get; set; }

        public string OutputMSBuildProject { get; set; }

        public List<MigrationError> Errors { get; set; }

        public List<string> Warnings { get; set; }

        public List<string> PreExistingCsprojDependencies { get; set; }

        public bool Skipped { get; set; }

        public bool Failed { get; set; }

        public bool Succeeded { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is ProjectMigrationReport report)
            {
                return Equals(report.ProjectDirectory, ProjectDirectory) &&
                    Equals(report.ProjectName, ProjectName) &&
                    Equals(report.OutputMSBuildProject, OutputMSBuildProject) &&
                    Equals(report.Errors, Errors) &&
                    Equals(report.Warnings, Warnings) &&
                    Equals(report.PreExistingCsprojDependencies, PreExistingCsprojDependencies) &&
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
            hash += (Errors?.GetHashCode() ?? 1 * 23);
            hash += (Warnings?.GetHashCode() ?? 1 * 23);
            hash += (PreExistingCsprojDependencies?.GetHashCode() ?? 1 * 23);
            hash += (Skipped.GetHashCode() * 23);
            hash += (Failed.GetHashCode() * 23);
            hash += (Succeeded.GetHashCode() * 23);
            return hash;
        }
    }
}
