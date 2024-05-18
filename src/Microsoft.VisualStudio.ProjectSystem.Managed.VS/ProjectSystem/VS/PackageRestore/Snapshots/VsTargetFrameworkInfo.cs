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

    public IReadOnlyDictionary<string, IReadOnlyList<IVsReferenceItem2>> Items
    {
        get
        {
            if (_items is null)
            {
                Span<int> lengths = stackalloc int[]
                {
                    _targetFrameworkInfo.FrameworkReferences.Length,
                    _targetFrameworkInfo.NuGetAuditSuppress.Length,
                    _targetFrameworkInfo.PackageDownloads.Length,
                    _targetFrameworkInfo.PackageReferences.Length,
                    _targetFrameworkInfo.CentralPackageVersions.Length,
                    _targetFrameworkInfo.ProjectReferences.Length,
                };
                int capacity = lengths[0];
                for (int i = 1; i < lengths.Length; i++)
                {
                    if (lengths[i] > capacity)
                        capacity = lengths[i];
                }

                ImmutableArray<IVsReferenceItem2>.Builder builder = ImmutableArray.CreateBuilder<IVsReferenceItem2>(capacity);
                _items = ImmutableDictionary.CreateRange(
                [
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("FrameworkReference", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.FrameworkReferences, builder)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("NuGetAuditSuppress", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.NuGetAuditSuppress, builder)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageDownload", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.PackageDownloads, builder)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageReference", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.PackageReferences, builder)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageVersion", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.CentralPackageVersions, builder)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("ProjectReference", CreateImmutableVsReferenceItemList(_targetFrameworkInfo.ProjectReferences, builder)),
                ]);
            }
            return _items;
        }
    }

    private static IReadOnlyList<IVsReferenceItem2> CreateImmutableVsReferenceItemList(ImmutableArray<ReferenceItem> referenceItems, ImmutableArray<IVsReferenceItem2>.Builder builder)
    {
        builder.Clear();
        foreach (var referenceItem in referenceItems)
        {
            VsReferenceItem vsReferenceItem = new VsReferenceItem(referenceItem);
            builder.Add(vsReferenceItem);
        }
        return builder.ToImmutable();
    }
}
