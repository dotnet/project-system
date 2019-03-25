// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class TargetFrameworkInfo : IVsTargetFrameworkInfo
    {
        public TargetFrameworkInfo(string targetFrameworkMoniker, IEnumerable<IVsReferenceItem> projectReferences, IEnumerable<IVsReferenceItem> packageReferences, IEnumerable<IVsProjectProperty> properties)
        {
            Requires.NotNullOrEmpty(targetFrameworkMoniker, nameof(targetFrameworkMoniker));
            Requires.NotNull(projectReferences, nameof(projectReferences));
            Requires.NotNull(packageReferences, nameof(packageReferences));
            Requires.NotNull(properties, nameof(properties));

            TargetFrameworkMoniker = targetFrameworkMoniker;
            ProjectReferences = new ReferenceItems(projectReferences);
            PackageReferences = new ReferenceItems(packageReferences);
            Properties = new ProjectProperties(properties);
        }

        public string TargetFrameworkMoniker { get; }

        public IVsReferenceItems PackageReferences { get; }

        public IVsReferenceItems ProjectReferences { get; }

        public IVsProjectProperties Properties { get; }
    }
}
