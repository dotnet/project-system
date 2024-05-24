// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static class RestoreLogger
    {
        public static void BeginNominateRestore(IManagedProjectDiagnosticOutputService logger, string fullPath, ProjectRestoreInfo projectRestoreInfo)
        {
            if (logger.IsEnabled)
            {
                using var batch = new BatchLogger(logger);
                batch.WriteLine();
                batch.WriteLine("------------------------------------------");
                batch.WriteLine($"BEGIN Nominate Restore for {fullPath}");
                batch.IndentLevel++;

                batch.WriteLine($"MSBuildProjectExtensionsPath:     {projectRestoreInfo.MSBuildProjectExtensionsPath}");
                batch.WriteLine($"OriginalTargetFrameworks:         {projectRestoreInfo.OriginalTargetFrameworks}");
                LogTargetFrameworks(batch, projectRestoreInfo.TargetFrameworks);
                LogReferenceItems(batch, "Tool References", projectRestoreInfo.ToolReferences);

                batch.IndentLevel--;
                batch.WriteLine();
            }
        }

        public static void EndNominateRestore(IManagedProjectDiagnosticOutputService logger, string fullPath)
        {
            if (logger.IsEnabled)
            {
                using var batch = new BatchLogger(logger);
                batch.WriteLine();
                batch.WriteLine("------------------------------------------");
                batch.WriteLine($"COMPLETED Nominate Restore for {fullPath}");
                batch.WriteLine();
            }
        }

        private static void LogTargetFrameworks(BatchLogger logger, ImmutableArray<TargetFrameworkInfo> targetFrameworks)
        {
            logger.WriteLine($"Target Frameworks ({targetFrameworks.Length})");
            logger.IndentLevel++;

            foreach (TargetFrameworkInfo tf in targetFrameworks)
            {
                LogTargetFramework(logger, tf);
            }

            logger.IndentLevel--;
        }

        private static void LogTargetFramework(BatchLogger logger, TargetFrameworkInfo targetFrameworkInfo)
        {
            logger.WriteLine(targetFrameworkInfo.TargetFrameworkMoniker);
            logger.IndentLevel++;

            LogReferenceItems(logger, "Framework References", targetFrameworkInfo.FrameworkReferences);
            LogReferenceItems(logger, "Package Downloads", targetFrameworkInfo.PackageDownloads);
            LogReferenceItems(logger, "Project References", targetFrameworkInfo.ProjectReferences);
            LogReferenceItems(logger, "Package References", targetFrameworkInfo.PackageReferences);
            LogReferenceItems(logger, "NuGet Audit Suppressions", targetFrameworkInfo.NuGetAuditSuppress);

            // CPM typically adds a lot of items, normally the same set for all projects in the solution, and for individual projects
            // many of the items aren't referenced by the project. While this is diagnostic logging, PackageVersions are particularly
            // spammy, so we'll only output the count for basic problem investigation, rather than the full list.
            logger.WriteLine($"Package Versions -- {targetFrameworkInfo.CentralPackageVersions.Length} (count only)");

            LogProperties(logger, "Target Framework Properties", targetFrameworkInfo.Properties);

            logger.IndentLevel--;
        }

        private static void LogProperties(BatchLogger logger, string heading, IImmutableDictionary<string, string> projectProperties)
        {
            IEnumerable<string> properties = projectProperties.Select(prop => $"{prop.Key}:{prop.Value}");
            logger.WriteLine($"{heading} -- ({string.Join(" | ", properties)})");
        }

        private static void LogReferenceItems(BatchLogger logger, string heading, ImmutableArray<ReferenceItem> references)
        {
            logger.WriteLine(heading);
            logger.IndentLevel++;

            foreach (ReferenceItem reference in references)
            {
                IEnumerable<string> properties = reference.Properties.Select(prop => $"{prop.Key}:{prop.Value}");

                logger.WriteLine($"{reference.Name} -- ({string.Join(" | ", properties)})");
            }

            logger.IndentLevel--;
        }
    }
}
