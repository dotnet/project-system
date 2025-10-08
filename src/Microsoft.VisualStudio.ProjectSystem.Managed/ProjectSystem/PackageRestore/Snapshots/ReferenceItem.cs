// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
/// Represents a reference item involved in package restore, with its associated metadata.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
internal class ReferenceItem
{
    // If additional state is added to this class, please update RestoreHasher

    public ReferenceItem(string name, IImmutableDictionary<string, string> properties)
    {
        Requires.NotNullOrEmpty(name);

        Name = name;
        Properties = properties;
    }

    /// <summary>
    /// Gets the name (item spec) of the reference.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name/value pair metadata associated with the reference.
    /// </summary>
    public IImmutableDictionary<string, string> Properties { get; }
}
