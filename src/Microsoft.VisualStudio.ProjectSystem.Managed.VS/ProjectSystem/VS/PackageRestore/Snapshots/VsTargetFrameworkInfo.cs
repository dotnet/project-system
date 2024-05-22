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

    public IReadOnlyDictionary<string, string?> Properties => _properties ??= _targetFrameworkInfo.Properties;

    public IReadOnlyDictionary<string, IReadOnlyList<IVsReferenceItem2>> Items
    {
        get
        {
            if (_items is null)
            {
                _items = ImmutableDictionary.CreateRange(
                [
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("FrameworkReference", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.FrameworkReferences)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("NuGetAuditSuppress", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.NuGetAuditSuppress)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageDownload", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.PackageDownloads)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageReference", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.PackageReferences)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageVersion", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.CentralPackageVersions)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("ProjectReference", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.ProjectReferences)),
                ]);
            }
            return _items;
        }
    }

    private static IReadOnlyList<IVsReferenceItem2> CreateImmutableVsReferenceItemList(ImmutableArray<ReferenceItem> referenceItems)
    {
        var builder = ImmutableArray.CreateBuilder<IVsReferenceItem2>(referenceItems.Length);
        foreach (var referenceItem in referenceItems)
        {
            VsReferenceItem vsReferenceItem = new VsReferenceItem(referenceItem);
            builder.Add(vsReferenceItem);
        }
        return builder.MoveToImmutable();
    }
}
