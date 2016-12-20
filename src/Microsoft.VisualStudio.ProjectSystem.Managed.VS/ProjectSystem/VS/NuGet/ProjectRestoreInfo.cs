// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ProjectRestoreInfo : IVsProjectRestoreInfo
    {
        public string BaseIntermediatePath { get; set; }

        public string OriginalTargetFrameworks { get; set; }

        public IVsTargetFrameworks TargetFrameworks { get; set; }

        public IVsReferenceItems ToolReferences { get; set; }
    }
}
