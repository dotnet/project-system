// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a <see cref="ProjectProperty"/> instance to implement the <see cref="IVsProjectProperty"/>
///     interface for NuGet.
/// </summary>
[DebuggerDisplay("{Name}: {Value}")]
internal class VsProjectProperty : IVsProjectProperty
{
    private readonly ProjectProperty _property;

    public VsProjectProperty(ProjectProperty property)
    {
        _property = property;
    }

    public string Name => _property.Name;

    public string Value => _property.Value;
}
