// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a <see cref="ProjectRestoreInfo"/> instance to implement the <see cref="IVsProjectRestoreInfo3"/>
///     interface for NuGet.
/// </summary>
internal class VsProjectRestoreInfo : IVsProjectRestoreInfo3
{
    private readonly ProjectRestoreInfo _info;
    
    private IReadOnlyList<IVsTargetFrameworkInfo4>? _targetFrameworks;
    private IReadOnlyList<IVsReferenceItem2>? _toolReferences;

    public VsProjectRestoreInfo(ProjectRestoreInfo info)
    {
        _info = info;
    }

    public string MSBuildProjectExtensionsPath => _info.MSBuildProjectExtensionsPath;

    public IReadOnlyList<IVsTargetFrameworkInfo4> TargetFrameworks => _targetFrameworks ??= _info.TargetFrameworks.SelectImmutableArray(r => new VsTargetFrameworkInfo(r));

    public IReadOnlyList<IVsReferenceItem2> ToolReferences => _toolReferences ??= _info.ToolReferences.SelectImmutableArray(r => new VsReferenceItem(r));

    public string OriginalTargetFrameworks => _info.OriginalTargetFrameworks;
}
