﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

/// <summary>
/// Names of metadata found on project items.
/// </summary>
internal static class ProjectItemMetadata
{
    public const string Name = "Name";
    public const string Type = "Type";
    public const string Version = "Version";
    public const string IsImplicitlyDefined = "IsImplicitlyDefined";
    public const string DefiningProjectFullPath = "DefiningProjectFullPath";
    public const string OriginalItemSpec = "OriginalItemSpec";
    public const string Visible = "Visible";
    public const string DiagnosticLevel = "DiagnosticLevel";
}
