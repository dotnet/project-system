// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug.Designer
{
    internal sealed record PropertyValueChangeRequest(
        string ProfileName,
        string PropertyName,
        object Value);

    internal sealed record Profile(string Name, string DisplayName);

    internal sealed record Category(string Name, string DisplayName);

    internal sealed record PropertyEditor(string Name, ImmutableDictionary<string, string> Metadata);

    internal sealed record PropertyMetadata(
        string Name,
        string DisplayName,
        Profile Profile,
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
        string ProfileName,
        string PropertyName,
        object? Value);
}
