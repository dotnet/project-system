// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Concrete implementation of <see cref="IVsProjectRestoreInfo"/> that will be passed to 
    ///     <see cref="IVsSolutionRestoreService.NominateProjectAsync(string, IVsProjectRestoreInfo, System.Threading.CancellationToken)"/>.
    /// </summary>
    internal class ProjectRestoreInfo : IVsProjectRestoreInfo
    {
        public ProjectRestoreInfo(string msbuildProjectExtensionsPath, string originalTargetFrameworks, IVsTargetFrameworks targetFrameworks, IVsReferenceItems toolReferences)
        {
            Requires.NotNullOrEmpty(msbuildProjectExtensionsPath, nameof(msbuildProjectExtensionsPath));
            Requires.NotNull(originalTargetFrameworks, nameof(originalTargetFrameworks));
            Requires.NotNull(targetFrameworks, nameof(targetFrameworks));
            Requires.NotNull(toolReferences, nameof(toolReferences));

            MSBuildProjectExtensionsPath = msbuildProjectExtensionsPath;
            OriginalTargetFrameworks = originalTargetFrameworks;
            TargetFrameworks = targetFrameworks;
            ToolReferences = toolReferences;
        }

        public string MSBuildProjectExtensionsPath { get; }

        public string OriginalTargetFrameworks { get; }

        public IVsTargetFrameworks TargetFrameworks { get; }

        public IVsReferenceItems ToolReferences { get; }

        // We "rename" BaseIntermediatePath to avoid confusion for our usage, 
        // because it actually represents "MSBuildProjectExtensionsPath"
        string IVsProjectRestoreInfo.BaseIntermediatePath
        {
            get { return MSBuildProjectExtensionsPath; }
        }
    }
}
