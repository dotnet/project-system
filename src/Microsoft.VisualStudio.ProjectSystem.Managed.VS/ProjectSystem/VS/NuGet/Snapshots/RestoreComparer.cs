// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal static partial class RestoreComparer
    {
        public readonly static IEqualityComparer<IVsProjectRestoreInfo> RestoreInfos = new ProjectRestoreInfoEqualityComparer();
        public readonly static IEqualityComparer<IVsReferenceItem> ReferenceItems = new ReferenceItemEqualityComparer();
        public readonly static IEqualityComparer<IVsProjectProperty> ProjectProperties = new ProjectPropertyEqualityComparer();
        public readonly static IEqualityComparer<IVsReferenceProperty> ReferenceProperties = new ReferencePropertyEqualityComparer();
        public readonly static IEqualityComparer<IVsTargetFrameworkInfo> TargetFrameworks = new TargetFrameworkInfoEqualityComparer();
    }
}
