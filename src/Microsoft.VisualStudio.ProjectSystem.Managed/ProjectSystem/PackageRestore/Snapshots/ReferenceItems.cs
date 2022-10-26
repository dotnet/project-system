// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Immutable collection of <see cref="IVsReferenceItem"/> objects.
    /// </summary>
    internal class ReferenceItems : ImmutablePropertyCollection<IVsReferenceItem>, IVsReferenceItems
    {
        public ReferenceItems(IEnumerable<IVsReferenceItem> items)
            : base(items, item => item.Name)
        {
        }
    }
}
