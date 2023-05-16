// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// Defines data about a group of dependencies, such as the caption and icons
/// to use for their group nodes in the dependencies tree.
/// </summary>
internal sealed class DependencyGroupType : IEquatable<DependencyGroupType>
{
    public string Id { get; }
    [Localizable(true)]
    public string Caption { get; }
    public ProjectImageMoniker NormalGroupIcon { get; }
    public ProjectImageMoniker WarningGroupIcon { get; }
    public ProjectImageMoniker ErrorGroupIcon { get; }
    public ProjectTreeFlags GroupNodeFlags { get; }

    public DependencyGroupType(
        string id,
        [Localizable(true)] string caption,
        ProjectImageMoniker normalGroupIcon,
        ProjectImageMoniker warningGroupIcon,
        ProjectImageMoniker errorGroupIcon,
        ProjectTreeFlags groupNodeFlags)
    {
        Requires.NotNullOrEmpty(id);
        Requires.NotNullOrEmpty(caption);

        Id = id;
        Caption = caption;
        NormalGroupIcon = normalGroupIcon;
        WarningGroupIcon = warningGroupIcon;
        ErrorGroupIcon = errorGroupIcon;
        GroupNodeFlags = groupNodeFlags + ProjectTreeFlags.VirtualFolder + DependencyTreeFlags.DependencyGroup;
    }

    public override bool Equals(object? obj) => ReferenceEquals(obj, this) || (obj is DependencyGroupType other && Equals(other));

    public bool Equals(DependencyGroupType other) => StringComparer.Ordinal.Equals(Id, other.Id);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);

    public override string ToString() => Id;
}
