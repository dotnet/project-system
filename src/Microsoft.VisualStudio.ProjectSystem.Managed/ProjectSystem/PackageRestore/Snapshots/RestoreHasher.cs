// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static class RestoreHasher
    {
        public static byte[] CalculateHash(ProjectRestoreInfo restoreInfo)
        {
            Requires.NotNull(restoreInfo, nameof(restoreInfo));

            using var hasher = new IncrementalHasher();

            AppendProperty(hasher, nameof(restoreInfo.ProjectAssetsFilePath),           restoreInfo.ProjectAssetsFilePath);
            AppendProperty(hasher, nameof(restoreInfo.MSBuildProjectExtensionsPath),    restoreInfo.MSBuildProjectExtensionsPath);
            AppendProperty(hasher, nameof(restoreInfo.OriginalTargetFrameworks),        restoreInfo.OriginalTargetFrameworks);

            foreach (IVsTargetFrameworkInfo3 framework in restoreInfo.TargetFrameworks)
            {
                AppendProperty(hasher, nameof(framework.TargetFrameworkMoniker), framework.TargetFrameworkMoniker);
                AppendFrameworkProperties(hasher, framework);
                AppendReferences(hasher, framework.ProjectReferences);
                AppendReferences(hasher, framework.PackageReferences);
                AppendReferences(hasher, framework.FrameworkReferences);
                AppendReferences(hasher, framework.PackageDownloads);
                AppendReferences(hasher, framework.CentralPackageVersions);
            }

            AppendReferences(hasher, restoreInfo.ToolReferences);

            return hasher.GetHashAndReset();
        }

        private static void AppendFrameworkProperties(IncrementalHasher hasher, IVsTargetFrameworkInfo2 framework)
        {
            foreach (IVsProjectProperty property in framework.Properties)
            {
                AppendProperty(hasher, property.Name, property.Value);
            }
        }

        private static void AppendReferences(IncrementalHasher hasher, IVsReferenceItems references)
        {
            foreach (IVsReferenceItem reference in references)
            {
                AppendProperty(hasher, nameof(reference.Name), reference.Name);
                AppendReferenceProperties(hasher, reference);
            }
        }

        private static void AppendReferenceProperties(IncrementalHasher hasher, IVsReferenceItem reference)
        {
            foreach (IVsReferenceProperty property in reference.Properties)
            {
                AppendProperty(hasher, property.Name, property.Value);
            }
        }

        private static void AppendProperty(IncrementalHasher hasher, string name, string value)
        {
            hasher.Append(name);
            hasher.Append("|");
            hasher.Append(value);
        }
    }
}
