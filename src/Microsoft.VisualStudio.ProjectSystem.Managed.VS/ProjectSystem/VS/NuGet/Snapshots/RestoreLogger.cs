// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal static class RestoreLogger
    {
        public static void BeginNominateRestore(IProjectLogger logger, string fullPath, IVsProjectRestoreInfo projectRestoreInfo)
        {
            if (logger.IsEnabled)
            {
                using (IProjectLoggerBatch batch = logger.BeginBatch())
                {
                    batch.WriteLine();
                    batch.WriteLine("------------------------------------------");
                    batch.WriteLine($"BEGIN Nominate Restore for {fullPath}");
                    batch.IndentLevel++;

                    batch.WriteLine($"MSBuildProjectExtensionsPath:     {projectRestoreInfo.BaseIntermediatePath}");
                    batch.WriteLine($"OriginalTargetFrameworks:         {projectRestoreInfo.OriginalTargetFrameworks}");
                    LogTargetFrameworks(batch, projectRestoreInfo.TargetFrameworks);
                    LogReferenceItems(batch, "Tool References", projectRestoreInfo.ToolReferences);

                    batch.IndentLevel--;
                    batch.WriteLine();
                }
            }
        }

        public static void EndNominateRestore(IProjectLogger logger, string fullPath)
        {
            if (logger.IsEnabled)
            {
                using (IProjectLoggerBatch batch = logger.BeginBatch())
                {
                    batch.WriteLine();
                    batch.WriteLine("------------------------------------------");
                    batch.WriteLine($"COMPLETED Nominate Restore for {fullPath}");
                    batch.WriteLine();
                }
            }
        }

        private static void LogTargetFrameworks(IProjectLoggerBatch logger, IVsTargetFrameworks targetFrameworks)
        {
            logger.WriteLine($"Target Frameworks ({targetFrameworks.Count})");
            logger.IndentLevel++;

            foreach (IVsTargetFrameworkInfo tf in targetFrameworks)
            {
                LogTargetFramework(logger, tf);
            }
            logger.IndentLevel--;
        }

        private static void LogTargetFramework(IProjectLoggerBatch logger, IVsTargetFrameworkInfo targetFrameworkInfo)
        {
            logger.WriteLine(targetFrameworkInfo.TargetFrameworkMoniker);
            logger.IndentLevel++;

            LogReferenceItems(logger, "Project References", targetFrameworkInfo.ProjectReferences);
            LogReferenceItems(logger, "Package References", targetFrameworkInfo.PackageReferences);
            LogProperties(logger, "Target Framework Properties", targetFrameworkInfo.Properties);

            logger.IndentLevel--;
        }

        private static void LogProperties(IProjectLoggerBatch logger, string heading, IVsProjectProperties projectProperties)
        {
            IEnumerable<string> properties = projectProperties.Cast<ProjectProperty>()
                    .Select(prop => $"{prop.Name}:{prop.Value}");
            logger.WriteLine($"{heading} -- ({string.Join(" | ", properties)})");
        }

        private static void LogReferenceItems(IProjectLoggerBatch logger, string heading, IVsReferenceItems references)
        {
            logger.WriteLine(heading);
            logger.IndentLevel++;

            foreach (IVsReferenceItem reference in references)
            {
                IEnumerable<string> properties = reference.Properties.Cast<IVsReferenceProperty>()
                                                                     .Select(prop => $"{prop.Name}:{prop.Value}");

                logger.WriteLine($"{reference.Name} -- ({string.Join(" | ", properties)})");
            }

            logger.IndentLevel--;
        }
    }
}
