// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
/// Represents a reference item involved in package restore, with its associated metadata.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
internal sealed class ReferenceItem : IRestoreState<ReferenceItem>
{
    // If additional state is added to this class, please update RestoreHasher

    public ReferenceItem(string name, IImmutableDictionary<string, string> metadata)
    {
        Requires.NotNullOrEmpty(name);

        Name = name;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the name (item spec) of the reference.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name/value pair metadata associated with the reference.
    /// </summary>
    public IImmutableDictionary<string, string> Metadata { get; }

    public void AddToHash(IncrementalHasher hasher)
    {
        hasher.AppendProperty(nameof(Name), Name);

        foreach ((string key, string value) in Metadata)
        {
            hasher.AppendProperty(key, value);
        }
    }

    public void DescribeChanges(RestoreStateComparisonBuilder builder, ReferenceItem after)
    {
        builder.PushScope(Name);

        builder.CompareString(Name, after.Name, name: "%(Identity)");

        builder.CompareDictionary(Metadata, after.Metadata);

        builder.PopScope();
    }
}
