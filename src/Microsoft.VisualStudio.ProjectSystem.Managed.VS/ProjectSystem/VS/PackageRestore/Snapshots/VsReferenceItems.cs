// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a collection of <see cref="ReferenceItem"/> instances to implement the <see cref="IVsReferenceItems"/>
///     interface for NuGet.
/// </summary>
internal class VsReferenceItems : ImmutablePropertyCollection<IVsReferenceItem, ReferenceItem>, IVsReferenceItems
{
    public VsReferenceItems(ImmutableArray<ReferenceItem> referenceItems)
        : base(referenceItems, item => item.Name, item => new VsReferenceItem(item))
    {
    }
}
