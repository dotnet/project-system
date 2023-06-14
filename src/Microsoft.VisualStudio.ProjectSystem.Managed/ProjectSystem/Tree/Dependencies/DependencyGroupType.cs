// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// Defines data about a group of dependencies, such as the caption and icons
/// to use for their group nodes in the dependencies tree.
/// </summary>
internal sealed class DependencyGroupType : IEquatable<DependencyGroupType>
{
    /// <summary>
    /// Uniquely identifies this dependency type within the dependencies tree.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The display string to show for the group node in the tree.
    /// </summary>
    [Localizable(true)]
    public string Caption { get; }

    /// <summary>
    /// The icon to use for the group node when no special status applies.
    /// </summary>
    public ProjectImageMoniker NormalGroupIcon { get; }

    /// <summary>
    /// The icon to use for the group node when a warning exists within the group.
    /// </summary>
    public ProjectImageMoniker WarningGroupIcon { get; }

    /// <summary>
    /// The icon to use for the group node when an error exists within the group.
    /// </summary>
    public ProjectImageMoniker ErrorGroupIcon { get; }

    /// <summary>
    /// Flags that apply to the group node itself.
    /// </summary>
    /// <remarks>
    /// These flags are not applied to children.
    /// </remarks>
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
