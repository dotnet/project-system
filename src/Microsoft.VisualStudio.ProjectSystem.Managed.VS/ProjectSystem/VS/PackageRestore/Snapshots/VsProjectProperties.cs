// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a collection of <see cref="ProjectProperty"/> instances to implement the <see cref="IVsProjectProperties"/>
///     interface for NuGet.
/// </summary>
internal class VsProjectProperties : ImmutablePropertyCollection<IVsProjectProperty, ProjectProperty>, IVsProjectProperties
{
    public VsProjectProperties(ImmutableArray<ProjectProperty> properties)
        : base(properties, item => item.Name, item => new VsProjectProperty(item))
    {
    }
}
