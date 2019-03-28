// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Represents the restore data for a single target framework in <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo
    {
        public TargetFrameworkInfo(string targetFrameworkMoniker, IVsReferenceItems projectReferences, IVsReferenceItems packageReferences, IVsProjectProperties properties)
        {
            Requires.NotNullOrEmpty(targetFrameworkMoniker, nameof(targetFrameworkMoniker));
            Requires.NotNull(projectReferences, nameof(projectReferences));
            Requires.NotNull(packageReferences, nameof(packageReferences));
            Requires.NotNull(properties, nameof(properties));

            TargetFrameworkMoniker = targetFrameworkMoniker;
            ProjectReferences = projectReferences;
            PackageReferences = packageReferences;
            Properties = properties;
        }

        public string TargetFrameworkMoniker { get; }

        public IVsReferenceItems PackageReferences { get; }

        public IVsReferenceItems ProjectReferences { get; }

        public IVsProjectProperties Properties { get; }
    }
}
