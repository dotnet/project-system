// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static partial class RestoreComparer
    {
        public static readonly IEqualityComparer<IVsReferenceItem?> ReferenceItems = new ReferenceItemEqualityComparer();
        public static readonly IEqualityComparer<IVsReferenceProperty?> ReferenceProperties = new ReferencePropertyEqualityComparer();
    }
}
