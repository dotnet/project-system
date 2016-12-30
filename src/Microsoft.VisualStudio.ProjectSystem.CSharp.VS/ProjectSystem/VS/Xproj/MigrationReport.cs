// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Migration report holder. Copied from
    /// https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectJsonMigration/Report/MigrationReport.cs
    /// </summary>
    internal class MigrationReport
    {
        public List<ProjectMigrationReport> ProjectMigrationReports { get; set; }

        public int MigratedProjectsCount { get; set; }

        public int SucceededProjectsCount { get; set; }

        public int FailedProjectsCount{ get; set; }

        public bool AllSucceeded { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is MigrationReport report)
            {
                return Equals(ProjectMigrationReports, report.ProjectMigrationReports) &&
                    Equals(MigratedProjectsCount, report.MigratedProjectsCount) &&
                    Equals(SucceededProjectsCount, report.SucceededProjectsCount) &&
                    Equals(FailedProjectsCount, report.FailedProjectsCount) &&
                    Equals(AllSucceeded, report.AllSucceeded);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = ProjectMigrationReports?.GetHashCode() ?? 1 * 67;
            hash += (MigratedProjectsCount * 67);
            hash += (SucceededProjectsCount * 67);
            hash += (FailedProjectsCount * 67);
            hash += (AllSucceeded.GetHashCode() * 67);
            return hash;
        }
    }
}
