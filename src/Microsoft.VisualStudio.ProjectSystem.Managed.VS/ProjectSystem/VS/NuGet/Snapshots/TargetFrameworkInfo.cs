// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo
    {
        public TargetFrameworkInfo(string targetFrameworkMoniker, IVsReferenceItems packageReferences, IVsReferenceItems projectReferences, IVsProjectProperties properties)
        {
            TargetFrameworkMoniker = targetFrameworkMoniker;
            PackageReferences = packageReferences;
            ProjectReferences = projectReferences;
            Properties = properties;
        }

        public string TargetFrameworkMoniker { get; }

        public IVsReferenceItems PackageReferences { get; }

        public IVsReferenceItems ProjectReferences { get; }

        public IVsProjectProperties Properties { get; }
    }
}
