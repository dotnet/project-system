// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Immutable collection of <see cref="IVsTargetFrameworkInfo"/> objects.
    /// </summary>
    internal class TargetFrameworks : ImmutablePropertyCollection<IVsTargetFrameworkInfo2>, IVsTargetFrameworks2
    {
        public TargetFrameworks(IEnumerable<IVsTargetFrameworkInfo3> items)
            : base(items, item => item.TargetFrameworkMoniker)
        {
        }
    }
}
