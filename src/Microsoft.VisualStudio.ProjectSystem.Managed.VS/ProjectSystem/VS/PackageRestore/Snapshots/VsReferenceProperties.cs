// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a collection of <see cref="ReferenceProperty"/> instances to implement the <see cref="IVsReferenceProperties"/>
///     interface for NuGet.
/// </summary>
internal class VsReferenceProperties : ImmutablePropertyCollection<IVsReferenceProperty, ReferenceProperty>, IVsReferenceProperties
{
    public VsReferenceProperties(ImmutableArray<ReferenceProperty> properties)
        : base(properties, item => item.Name, item => new VsReferenceProperty(item))
    {
    }
}
