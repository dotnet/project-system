// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

/// <summary>
/// Subclass of <see cref="Dependency"/> that adds data specific to dependencies modelled via MSBuild.
/// </summary>
internal sealed class MSBuildDependency : Dependency
{
    private readonly MSBuildDependencyFactoryBase _factory;

    // NOTE the "ID" for MSBuild dependencies is the item spec of the evaluated (unresolved) item.

    public MSBuildDependency(
        MSBuildDependencyFactoryBase factory,
        string id,
        string caption,
        ProjectImageMoniker icon,
        ProjectTreeFlags flags,
        DiagnosticLevel diagnosticLevel,
        bool? isResolved,
        bool isImplicit,
        string? filePath,
        IImmutableDictionary<string, string> browseObjectProperties)
        : base(
            id,
            caption,
            icon,
            flags,
            diagnosticLevel,
            filePath,
            useResolvedReferenceRule: isResolved ?? false,
            isResolved is true ? factory.ResolvedRuleName : factory.UnresolvedRuleName,
            factory.SchemaItemType,
            browseObjectProperties)
    {
        Requires.NotNullOrWhiteSpace(id);
        Requires.NotNullOrWhiteSpace(caption);

        _factory = factory;
        IsResolved = isResolved;
        IsImplicit = isImplicit;

        System.Diagnostics.Debug.Assert(Flags.Contains(isResolved is true ? ProjectTreeFlags.ResolvedReference : ProjectTreeFlags.BrokenReference), "Inconsistent BrokenReference/ResolvedReference flags on MSBuildDependency.");
        System.Diagnostics.Debug.Assert(isImplicit != Flags.Contains(DependencyTreeFlags.SupportsRemove), "Inconsistent SupportsRemove flag on MSBuildDependency.");
    }

    /// <summary>
    /// Gets whether the dependency is resolved or not.
    /// </summary>
    /// <remarks>
    /// Has three states:
    /// <list type="number">
    ///   <item>
    ///     <see langword="null"/> means the dependency has only been observed in evaluation data, so we don't know whether
    ///     it should be considered resolved or not. This allows us to give the dependency the benefit of the doubt
    ///     and avoid marking it as unresolved (showing a yellow triangle) when the project is loading.
    ///   </item>
    ///   <item>
    ///     <see langword="true"/> means the dependency has been observed in resolved dependency data.
    ///   </item>
    ///   <item>
    ///     <see langword="false"/> means resolved data was observed that should have contained this dependency, but did not,
    ///     at which point we consider the dependency as unresolved and will show a corresponding warning (yellow triangle).
    ///   </item>
    /// </list>
    /// </remarks>
    public bool? IsResolved { get; }

    /// <summary>
    /// Gets whether this dependency is declared in an imported project file.
    /// </summary>
    /// <remarks>
    /// If <see langword="true"/>, the dependency was declared outside of the main project file.
    /// It cannot be removed from the project, and should be displayed with an overlay on its icon.
    /// </remarks>
    public bool IsImplicit { get; }

    internal MSBuildDependency With(bool? isResolved, bool isImplicit, DiagnosticLevel diagnosticLevel, string caption, ProjectImageMoniker icon, ProjectTreeFlags flags, string? filePath, IImmutableDictionary<string, string> browseObjectProperties)
    {
        if (isResolved != IsResolved ||
            isImplicit != IsImplicit ||
            diagnosticLevel != DiagnosticLevel ||
            caption != Caption ||
            icon != Icon ||
            flags != Flags ||
            filePath != FilePath ||
            browseObjectProperties != BrowseObjectProperties)
        {
            return new MSBuildDependency(
                _factory,
                Id,
                caption,
                icon,
                flags,
                diagnosticLevel,
                isResolved,
                isImplicit,
                filePath,
                browseObjectProperties);
        }

        return this;
    }

    internal MSBuildDependency With(bool isResolved, ProjectImageMoniker icon, ProjectTreeFlags flags, DiagnosticLevel diagnosticLevel)
    {
        if (isResolved != IsResolved ||
            diagnosticLevel != DiagnosticLevel ||
            icon != Icon ||
            flags != Flags)
        {
            return new MSBuildDependency(
                _factory,
                Id,
                Caption,
                icon,
                flags,
                diagnosticLevel,
                isResolved,
                IsImplicit,
                FilePath,
                BrowseObjectProperties);
        }

        return this;
    }

    public override string ToString()
    {
        // Used for debugging only
        var sb = PooledStringBuilder.GetInstance();
        sb.Append("Id=\"");
        sb.Append(Id);
        sb.Append('"');

        if (IsResolved is true)
            sb.Append(" Resolved");
        else if (IsResolved is null)
            sb.Append(" Resolved?");
        else if (IsResolved is false)
            sb.Append(" Unresolved");

        if (IsImplicit)
            sb.Append(" Implicit");

        return sb.ToStringAndFree();
    }
}
