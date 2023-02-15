// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

/// <summary>
///     Wraps a <see cref="TargetFrameworkInfo"/> instance to implement the <see cref="IVsTargetFrameworkInfo3"/>
///     interface for NuGet;
/// </summary>
[DebuggerDisplay("TargetFrameworkMoniker = {TargetFrameworkMoniker}")]
internal class VsTargetFrameworkInfo : IVsTargetFrameworkInfo3
{
    private readonly TargetFrameworkInfo _targetFrameworkInfo;
    
    private VsReferenceItems? _packageDownloads;
    private VsReferenceItems? _frameworkReferences;
    private VsReferenceItems? _projectReferences;
    private VsReferenceItems? _packageReferences;
    private VsReferenceItems? _centralPackageVersions;
    private VsProjectProperties? _properties;

    public VsTargetFrameworkInfo(TargetFrameworkInfo targetFrameworkInfo)
    {
        _targetFrameworkInfo = targetFrameworkInfo;
    }

    public IVsReferenceItems PackageDownloads => _packageDownloads ??= new VsReferenceItems(_targetFrameworkInfo.PackageDownloads);

    public IVsReferenceItems FrameworkReferences => _frameworkReferences ??= new VsReferenceItems(_targetFrameworkInfo.FrameworkReferences);

    public string TargetFrameworkMoniker => _targetFrameworkInfo.TargetFrameworkMoniker;

    public IVsReferenceItems ProjectReferences => _projectReferences ??= new VsReferenceItems(_targetFrameworkInfo.ProjectReferences);

    public IVsReferenceItems PackageReferences => _packageReferences ??= new VsReferenceItems(_targetFrameworkInfo.PackageReferences);

    public IVsProjectProperties Properties => _properties ??= new VsProjectProperties(_targetFrameworkInfo.Properties);

    public IVsReferenceItems CentralPackageVersions => _centralPackageVersions ??= new VsReferenceItems(_targetFrameworkInfo.CentralPackageVersions);
}
