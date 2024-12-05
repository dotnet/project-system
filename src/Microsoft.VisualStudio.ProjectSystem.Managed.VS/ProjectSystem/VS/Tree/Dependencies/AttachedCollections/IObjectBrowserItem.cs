// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

/// <summary>
/// Used by implementations of <see cref="IRelatableItem"/> that support opening in Visual Studio's Object Browser via their context menu.
/// </summary>
public interface IObjectBrowserItem : IRelatableItem
{
    /// <summary>
    /// Gets the absolute path to an assembly that should be opened in the Object Browser.
    /// </summary>
    string? AssemblyPath { get; }
}
