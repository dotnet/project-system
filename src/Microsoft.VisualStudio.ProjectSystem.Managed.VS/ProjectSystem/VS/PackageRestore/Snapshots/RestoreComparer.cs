// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static partial class RestoreComparer
    {
        public static readonly IEqualityComparer<IVsReferenceItem?> ReferenceItems = new ReferenceItemEqualityComparer();
        public static readonly IEqualityComparer<IVsReferenceProperty?> ReferenceProperties = new ReferencePropertyEqualityComparer();
    }
}
