// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// An immutable snapshot of a single dependency.
/// </summary>
internal interface IDependency
{
    /// <summary>
    /// Gets a unique identifier for the dependency within its group (and slice, for configured dependencies).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the friendly name of the dependency, for use in the UI.
    /// </summary>
    /// <remarks>
    /// Although this is a display string, it rarely requires localization. It's usually just a verbatim name.
    /// </remarks>
    string Caption { get; }

    /// <summary>
    /// Gets the icon to display for this dependency.
    /// </summary>
    /// <remarks>
    /// Takes any diagnostic level and/or implicit state into account.
    /// </remarks>
    ProjectImageMoniker Icon { get; }

    /// <summary>
    /// Gets the set of <see cref="ProjectTreeFlags"/> applicable to this dependency.
    /// </summary>
    ProjectTreeFlags Flags { get; }

    /// <summary>
    /// Gets the severity level of any diagnostic associated with this dependency.
    /// </summary>
    DiagnosticLevel DiagnosticLevel { get; }
}

/// <summary>
/// Extends <see cref="IDependency"/>, adding properties that control how the dependency's browse
/// object should be obtained.
/// </summary>
/// <remarks>
/// A browse object determined which properties, values, descriptions, etc. are displayed in Visual Studio's
/// "Properties" window. An <see cref="IDependency"/> that implements this interface is able to populate
/// the property grid in that window with data.
/// </remarks>
internal interface IDependencyWithBrowseObject : IDependency
{
    /// <summary>
    /// Gets whether the browse object for this dependency represents a resolved reference.
    /// </summary>
    /// <remarks>
    /// Used in <see cref="IProjectTreeOperations.GetDependencyBrowseObjectRuleAsync"/> to determine the browse
    /// object rule for this dependency.
    /// </remarks>
    bool UseResolvedReferenceRule { get; }

    /// <summary>
    /// The resolved path of the dependency, where appropriate, otherwise <see langword="null"/>.
    /// </summary>
    string? FilePath { get; }

    /// <summary>
    /// Gets the name of the rule (also known as the schema name) that backs this dependency's browse object.
    /// </summary>
    /// <remarks>
    /// Used in <see cref="IProjectTreeOperations.GetDependencyBrowseObjectRuleAsync"/> to determine the browse
    /// object rule for this dependency.
    /// </remarks>
    string? SchemaName { get; }

    /// <summary>
    /// Gets the name of MSBuild item type (where relevant) that backs this dependency's browse object.
    /// </summary>
    /// <remarks>
    /// Used in <see cref="IProjectTreeOperations.GetDependencyBrowseObjectRuleAsync"/> to determine the browse
    /// object rule for this dependency.
    /// </remarks>
    string? SchemaItemType { get; }

    /// <summary>
    /// Gets the names and values of properties to use in the browse object.
    /// </summary>
    /// <remarks>
    /// Used in <see cref="IProjectTreeOperations.GetDependencyBrowseObjectRuleAsync"/> to determine the browse
    /// object rule for this dependency.
    /// </remarks>
    IImmutableDictionary<string, string> BrowseObjectProperties { get; }
}
