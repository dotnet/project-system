// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Immutable collection of <see cref="IVsProjectProperty"/> objects.
    /// </summary>
    internal class ProjectProperties : ImmutablePropertyCollection<IVsProjectProperty>, IVsProjectProperties
    {
        public ProjectProperties(IEnumerable<IVsProjectProperty> items)
            : base(items, item => item.Name)
        {
        }
    }
}
