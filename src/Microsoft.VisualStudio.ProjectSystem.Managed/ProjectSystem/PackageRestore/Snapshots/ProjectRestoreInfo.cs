// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Concrete implementation of <see cref="IVsProjectRestoreInfo2"/> that will be passed to
    ///     <see cref="IVsSolutionRestoreService3.NominateProjectAsync(string, IVsProjectRestoreInfo2, System.Threading.CancellationToken)"/>.
    /// </summary>
    internal class ProjectRestoreInfo : IVsProjectRestoreInfo2
    {
        // If additional fields/properties are added to this class, please update RestoreHasher

        public ProjectRestoreInfo(string msbuildProjectExtensionsPath, string projectAssetsFilePath, string originalTargetFrameworks, IVsTargetFrameworks2 targetFrameworks, IVsReferenceItems toolReferences)
        {
            MSBuildProjectExtensionsPath = msbuildProjectExtensionsPath;
            ProjectAssetsFilePath = projectAssetsFilePath;
            OriginalTargetFrameworks = originalTargetFrameworks;
            TargetFrameworks = targetFrameworks;
            ToolReferences = toolReferences;
        }

        public string MSBuildProjectExtensionsPath { get; }

        public string ProjectAssetsFilePath { get; }

        public string OriginalTargetFrameworks { get; }

        public IVsTargetFrameworks2 TargetFrameworks { get; }

        public IVsReferenceItems ToolReferences { get; }

        // We "rename" BaseIntermediatePath to avoid confusion for our usage, 
        // because it actually represents "MSBuildProjectExtensionsPath"
        string IVsProjectRestoreInfo2.BaseIntermediatePath
        {
            get { return MSBuildProjectExtensionsPath; }
        }
    }
}
