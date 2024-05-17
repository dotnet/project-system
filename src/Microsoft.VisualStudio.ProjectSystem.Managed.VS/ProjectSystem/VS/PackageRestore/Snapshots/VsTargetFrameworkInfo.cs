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
internal class VsTargetFrameworkInfo : IVsTargetFrameworkInfo4
{
    private readonly TargetFrameworkInfo _targetFrameworkInfo;
    
    private IReadOnlyDictionary<string, string?>? _properties;
    private IReadOnlyDictionary<string, IReadOnlyList<IVsReferenceItem2>>? _items;

    public VsTargetFrameworkInfo(TargetFrameworkInfo targetFrameworkInfo)
    {
        _targetFrameworkInfo = targetFrameworkInfo;
    }

    public string TargetFrameworkMoniker => _targetFrameworkInfo.TargetFrameworkMoniker;

    public IReadOnlyDictionary<string, string?> Properties => _properties ??= _targetFrameworkInfo.Properties!;

    public IReadOnlyDictionary<string, IReadOnlyList<IVsReferenceItem2>> Items => _items ??= ImmutableDictionary.CreateRange(
        [
            new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("FrameworkReference", ImmutableList.CreateRange(_targetFrameworkInfo.FrameworkReferences.Select(r => new VsReferenceItem(r)))),
            new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageDownload", ImmutableList.CreateRange(_targetFrameworkInfo.PackageDownloads.Select(r => new VsReferenceItem(r)))),
            new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageReference", ImmutableList.CreateRange(_targetFrameworkInfo.PackageReferences.Select(r => new VsReferenceItem(r)))),
            new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageVersion", ImmutableList.CreateRange(_targetFrameworkInfo.CentralPackageVersions.Select(r => new VsReferenceItem(r)))),
            new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("ProjectReference", ImmutableList.CreateRange(_targetFrameworkInfo.ProjectReferences.Select(r => new VsReferenceItem(r)))),
        ]);
}
