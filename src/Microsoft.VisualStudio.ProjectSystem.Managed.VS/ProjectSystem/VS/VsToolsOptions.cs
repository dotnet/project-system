// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS;

/// <summary>
///     Provides Pages, Categories, and Options related to VS menu Tools | Options.
/// </summary>
internal static class VsToolsOptions
{
    /// <summary>
    /// Category Environment in Projects and Solutions
    /// </summary>
    public const string CategoryEnvironment = "Environment";

    /// <summary>
    /// Page Projects and Solutions in Tools | Options
    /// </summary>
    public const string PageProjectsAndSolution = "ProjectsAndSolution";

    /// <summary>
    /// "Enable symbolic renaming when renaming files" in Tools | Options | Projects and Solutions
    /// </summary>
    public const string OptionEnableSymbolicRename = "SolutionNavigator.EnableSymbolicRename";

    /// <summary>
    /// "Prompt for symbolic renaming when renaming files" in Tools | Options | Projects and Solutions
    /// </summary>
    public const string OptionPromptRenameSymbol = "PromptForRenameSymbol";

    /// <summary>
    /// "Prompt to update namespace when moving files" in Tools | Options | Projects and Solutions
    /// </summary>
    public const string OptionPromptNamespaceUpdate = "SolutionNavigator.PromptNamespaceUpdate";

    /// <summary>
    /// "Enable namespace update when moving files" in Tools | Options | Projects and Solutions
    /// </summary>
    public const string OptionEnableNamespaceUpdate = "SolutionNavigator.EnableNamespaceUpdate";
}
