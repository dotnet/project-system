// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Migration report holder. Copied from
    /// https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectJsonMigration/Report/MigrationReport.cs
    /// </summary>
    internal class MigrationReport
    {
        [JsonProperty]
        public IReadOnlyList<ProjectMigrationReport> ProjectMigrationReports { get; private set; }

        [JsonProperty]
        public int MigratedProjectsCount { get; private set; }

        [JsonProperty]
        public int SucceededProjectsCount { get; private set; }

        [JsonProperty]
        public int FailedProjectsCount { get; private set; }

        [JsonProperty]
        public bool AllSucceeded { get; private set; }

        public MigrationReport() { }

        public MigrationReport(int succeededProjectsCount, List<ProjectMigrationReport> reports)
        {
            SucceededProjectsCount = succeededProjectsCount;
            ProjectMigrationReports = reports;
        }

        public override bool Equals(object obj)
        {
            if (obj is MigrationReport report)
            {
                return (report.ProjectMigrationReports?.SequenceEqual(ProjectMigrationReports) ?? ProjectMigrationReports == null) &&
                    Equals(MigratedProjectsCount, report.MigratedProjectsCount) &&
                    Equals(SucceededProjectsCount, report.SucceededProjectsCount) &&
                    Equals(FailedProjectsCount, report.FailedProjectsCount) &&
                    Equals(AllSucceeded, report.AllSucceeded);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = ProjectMigrationReports?.Sum(p => p.GetHashCode() * 17) ?? 1 * 67;
            hash += (MigratedProjectsCount * 67);
            hash += (SucceededProjectsCount * 67);
            hash += (FailedProjectsCount * 67);
            hash += (AllSucceeded.GetHashCode() * 67);
            return hash;
        }
    }
}
