// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static class RestoreHasher
    {
        public static Hash CalculateHash(ProjectRestoreInfo restoreInfo)
        {
            Requires.NotNull(restoreInfo);

            using var hasher = new IncrementalHasher();

            hasher.AppendProperty(nameof(restoreInfo.ProjectAssetsFilePath),           restoreInfo.ProjectAssetsFilePath);
            hasher.AppendProperty(nameof(restoreInfo.MSBuildProjectExtensionsPath),    restoreInfo.MSBuildProjectExtensionsPath);
            hasher.AppendProperty(nameof(restoreInfo.OriginalTargetFrameworks),        restoreInfo.OriginalTargetFrameworks);

            foreach (TargetFrameworkInfo framework in restoreInfo.TargetFrameworks)
            {
                hasher.AppendProperty(nameof(framework.TargetFrameworkMoniker), framework.TargetFrameworkMoniker);
                hasher.AppendFrameworkProperties(framework);
                hasher.AppendReferences(framework.ProjectReferences);
                hasher.AppendReferences(framework.PackageReferences);
                hasher.AppendReferences(framework.FrameworkReferences);
                hasher.AppendReferences(framework.PackageDownloads);
                hasher.AppendReferences(framework.CentralPackageVersions);
                hasher.AppendReferences(framework.NuGetAuditSuppress);
            }

            AppendReferences(hasher, restoreInfo.ToolReferences);

            return hasher.GetHashAndReset();
        }

        private static void AppendFrameworkProperties(this IncrementalHasher hasher, TargetFrameworkInfo framework)
        {
            foreach ((string key, string value) in framework.Properties)
            {
                AppendProperty(hasher, key, value);
            }
        }

        private static void AppendReferences(this IncrementalHasher hasher, ImmutableArray<ReferenceItem> references)
        {
            foreach (ReferenceItem reference in references)
            {
                AppendProperty(hasher, nameof(reference.Name), reference.Name);
                AppendReferenceProperties(hasher, reference);
            }
        }

        private static void AppendReferenceProperties(this IncrementalHasher hasher, ReferenceItem reference)
        {
            foreach ((string key, string value) in reference.Properties)
            {
                AppendProperty(hasher, key, value);
            }
        }

        private static void AppendProperty(this IncrementalHasher hasher, string name, string value)
        {
            hasher.Append(name);
            hasher.Append("|");
            hasher.Append(value);
        }
    }
}
