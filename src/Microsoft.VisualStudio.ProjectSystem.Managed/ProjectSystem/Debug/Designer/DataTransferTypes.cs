// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Debug.Designer
{
    internal sealed record PropertyValueChangeRequest(
        string PropertyName,
        object Value);

    internal sealed record ProfileType(
        string CommandName,
        string DisplayName,
        string? HelpUrl,
        ImageMoniker Icon);

    /// <summary>
    /// Represents a launch profile and all associated properties and values.
    /// </summary>
    /// <remarks>
    /// The design of this type (as well as ILaunchProfileEditorClientSession"
    /// assumes that the debugger command name associated with a Profile never changes.
    /// The set of properties associated with a profile depends on the debugger command;
    /// if the command can change than the set of properties on a profile can change,
    /// not just their values as in the current design. If we adopt an alternative
    /// design that allows changing the command we will also need to update ILaunchProfileClientEditorSession"
    /// to support adding/removing properties.
    /// </remarks>
    /// <param name="Name">
    /// The unique name of the profile.
    /// </param>
    /// <param name="CommandName">
    /// The debugger command associated with this profile. Corresponds to ProfileType.CommandName.
    /// </param>
    /// <param name="Properties">
    /// The properties and values for this launch profile.
    /// </param>
    internal sealed record Profile(
        string Name,
        string CommandName,
        ImmutableArray<Property> Properties);

    internal sealed record Category(
        string Name,
        string DisplayName,
        int Priority);

    internal sealed record PropertyEditor(
        string Name,
        ImmutableDictionary<string, string> Metadata);

    internal sealed record PropertyMetadata(
        string Name,
        string DisplayName,
        Category Category,
        string? DependsOn,
        string? Description,
        int Priority,
        PropertyEditor? Editor,
        string? SearchTerms,
        string? HelpUrl,
        string? VisibilityCondition);

    internal sealed record SupportedValue(
        string DisplayName,
        object Value);

    internal sealed record Property(
        PropertyMetadata Metadata,
        object? Value,
        ImmutableArray<SupportedValue> SupportedValues);

    internal sealed record PropertyUpdate(
        string PropertyName,
        object? Value);
}
