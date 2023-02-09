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
internal class TargetFrameworkInfoWrapper : IVsTargetFrameworkInfo3
{
    private readonly TargetFrameworkInfo _targetFrameworkInfo;
    
    private ReferenceItemsWrapper? _packageDownloads;
    private ReferenceItemsWrapper? _frameworkReferences;
    private ReferenceItemsWrapper? _projectReferences;
    private ReferenceItemsWrapper? _packageReferences;
    private ReferenceItemsWrapper? _centralPackageVersions;
    private ProjectPropertiesWrapper? _properties;

    public TargetFrameworkInfoWrapper(TargetFrameworkInfo targetFrameworkInfo)
    {
        _targetFrameworkInfo = targetFrameworkInfo;
    }

    public IVsReferenceItems PackageDownloads => _packageDownloads ??= new ReferenceItemsWrapper(_targetFrameworkInfo.PackageDownloads);

    public IVsReferenceItems FrameworkReferences => _frameworkReferences ??= new ReferenceItemsWrapper(_targetFrameworkInfo.FrameworkReferences);

    public string TargetFrameworkMoniker => _targetFrameworkInfo.TargetFrameworkMoniker;

    public IVsReferenceItems ProjectReferences => _projectReferences ??= new ReferenceItemsWrapper(_targetFrameworkInfo.ProjectReferences);

    public IVsReferenceItems PackageReferences => _packageReferences ??= new ReferenceItemsWrapper(_targetFrameworkInfo.PackageReferences);

    public IVsProjectProperties Properties => _properties ??= new ProjectPropertiesWrapper(_targetFrameworkInfo.Properties);

    public IVsReferenceItems CentralPackageVersions => _centralPackageVersions ??= new ReferenceItemsWrapper(_targetFrameworkInfo.CentralPackageVersions);
}
