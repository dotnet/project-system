// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a <see cref="ProjectRestoreInfo"/> instance to implement the <see cref="IVsProjectRestoreInfo2"/>
///     interface for NuGet.
/// </summary>
internal class VsProjectRestoreInfo : IVsProjectRestoreInfo2
{
    private readonly ProjectRestoreInfo _info;
    
    private VsTargetFrameworks? _targetFrameworks;
    private VsReferenceItems? _toolReferences;

    public VsProjectRestoreInfo(ProjectRestoreInfo info)
    {
        _info = info;
    }

    public string BaseIntermediatePath => _info.MSBuildProjectExtensionsPath;

    public IVsTargetFrameworks2 TargetFrameworks => _targetFrameworks ??= new VsTargetFrameworks(_info.TargetFrameworks);

    public IVsReferenceItems ToolReferences => _toolReferences ??= new VsReferenceItems(_info.ToolReferences);

    public string OriginalTargetFrameworks => _info.OriginalTargetFrameworks;
}
