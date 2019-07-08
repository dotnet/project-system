// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents the restore data for a single target framework in <see cref="UnconfiguredProject"/>.
    /// </summary>
    [DebuggerDisplay("TargetFrameworkMoniker = {TargetFrameworkMoniker}")]
    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo, IVsTargetFrameworkInfo2
    {
        public TargetFrameworkInfo(string targetFrameworkMoniker, IVsReferenceItems frameworkReferences, IVsReferenceItems packageDownloads, IVsReferenceItems projectReferences, IVsReferenceItems packageReferences, IVsProjectProperties properties)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            FrameworkReferences = frameworkReferences;
            PackageDownloads = packageDownloads;
            ProjectReferences = projectReferences;
            PackageReferences = packageReferences;
            Properties = properties;
        }

        public string TargetFrameworkMoniker { get; }

        public IVsReferenceItems FrameworkReferences { get; }

        public IVsReferenceItems PackageDownloads { get; }

        public IVsReferenceItems PackageReferences { get; }

        public IVsReferenceItems ProjectReferences { get; }

        public IVsProjectProperties Properties { get; }
    }
}
