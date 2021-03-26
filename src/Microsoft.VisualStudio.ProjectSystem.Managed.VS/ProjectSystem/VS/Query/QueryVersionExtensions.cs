// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal static class QueryVersionExtensions
    {
        public static void ReportProjectVersion(this IQueryExecutionContext executionContext, UnconfiguredProject unconfiguredProject)
        {
            unconfiguredProject.GetQueryDataVersion(out string versionKey, out long versionNumber);
            executionContext.ReportInputDataVersion(versionKey, versionNumber);
        }

        public static void ReportProjectUpdate(this IQueryExecutionContext executionContext, UnconfiguredProject unconfiguredProject)
        {
            unconfiguredProject.GetQueryDataVersion(out string versionKey, out long versionNumber);
            executionContext.ReportUpdatedDataVersion(versionKey, versionNumber);
        }
    }
}
