// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

/// <summary>
/// Base class for all built-in dependency types of the .NET Project System.
/// </summary>
internal class Dependency : IDependency, IDependencyWithBrowseObject
{
    public Dependency(
        string id,
        string caption,
        ProjectImageMoniker icon,
        ProjectTreeFlags flags,
        DiagnosticLevel diagnosticLevel,
        string? filePath,
        bool useResolvedReferenceRule,
        string? schemaName = null,
        string? schemaItemType = null,
        IImmutableDictionary<string, string>? browseObjectProperties = null)
    {
        Id = id;
        Caption = caption;
        Icon = icon;
        Flags = flags;
        DiagnosticLevel = diagnosticLevel;
        FilePath = filePath;
        UseResolvedReferenceRule = useResolvedReferenceRule;
        SchemaName = schemaName ?? Folder.SchemaName;
        SchemaItemType = schemaItemType ?? Folder.PrimaryDataSourceItemType;
        BrowseObjectProperties = browseObjectProperties ?? ImmutableStringDictionary<string>.EmptyOrdinal
            .Add(Folder.IdentityProperty, Caption)
            .Add(Folder.FullPathProperty, FilePath ?? string.Empty);
    }

    #region IDependency

    public string Id { get; }

    public string Caption { get; }

    public ProjectImageMoniker Icon { get; }

    public ProjectTreeFlags Flags { get; }

    public DiagnosticLevel DiagnosticLevel { get; }

    #endregion

    #region IDependencyWithBrowseObject

    public string? FilePath { get; }

    public bool UseResolvedReferenceRule { get; }

    public string SchemaName { get; }

    public string SchemaItemType { get; }

    public IImmutableDictionary<string, string> BrowseObjectProperties { get; }

    #endregion

    public override string ToString()
    {
        // Used for debugging only
        var sb = PooledStringBuilder.GetInstance();

        sb.Append("Id=\"");
        sb.Append(Id);
        sb.Append('"');

        if (!StringComparers.ItemNames.Equals(Id, Caption))
        {
            sb.Append(" Caption=\"");
            sb.Append(Caption);
            sb.Append('"');
        }

        if (DiagnosticLevel != DiagnosticLevel.None)
        {
            sb.Append(" ");
            sb.Append(DiagnosticLevel.ToString());
        }

        return sb.ToStringAndFree();
    }
}
