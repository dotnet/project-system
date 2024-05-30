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
internal class VsTargetFrameworkInfo(TargetFrameworkInfo targetFrameworkInfo) : IVsTargetFrameworkInfo4
{
    private IReadOnlyDictionary<string, string>? _properties;
    private IReadOnlyDictionary<string, IReadOnlyList<IVsReferenceItem2>>? _items;

    public string TargetFrameworkMoniker => targetFrameworkInfo.TargetFrameworkMoniker;

    public IReadOnlyDictionary<string, string> Properties => _properties ??= targetFrameworkInfo.Properties;

    public IReadOnlyDictionary<string, IReadOnlyList<IVsReferenceItem2>> Items
    {
        get
        {
            _items ??= ImmutableDictionary.CreateRange(
                StringComparers.ItemNames,
                [
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("FrameworkReference", ToItems(targetFrameworkInfo.FrameworkReferences)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("NuGetAuditSuppress", ToItems(targetFrameworkInfo.NuGetAuditSuppress)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageDownload", ToItems(targetFrameworkInfo.PackageDownloads)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageReference", ToItems(targetFrameworkInfo.PackageReferences)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("PackageVersion", ToItems(targetFrameworkInfo.CentralPackageVersions)),
                    new KeyValuePair<string, IReadOnlyList<IVsReferenceItem2>>("ProjectReference", ToItems(targetFrameworkInfo.ProjectReferences)),
                ]);
            return _items;

            static IReadOnlyList<IVsReferenceItem2> ToItems(ImmutableArray<ReferenceItem> referenceItems)
            {
                return referenceItems.SelectImmutableArray(static r => new VsReferenceItem(r));
            }
        }
    }
}
