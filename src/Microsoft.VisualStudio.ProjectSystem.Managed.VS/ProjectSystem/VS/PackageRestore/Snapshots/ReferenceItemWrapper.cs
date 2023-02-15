// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a <see cref="ReferenceItem"/> instance to implement the <see cref="IVsReferenceItem"/>
///     interface for NuGet.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
internal class VsReferenceItem : IVsReferenceItem
{
    private readonly ReferenceItem _referenceItem;

    private VsReferenceProperties? _properties;

    public VsReferenceItem(ReferenceItem referenceItem)
    {
        _referenceItem = referenceItem;
    }

    public string Name => _referenceItem.Name;

    public IVsReferenceProperties Properties => _properties ??= new VsReferenceProperties(_referenceItem.Properties);
}
