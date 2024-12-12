﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
///     Handles changes to source items and applies them to a
///     <see cref="IWorkspaceProjectContext"/> instance.
/// </summary>
internal interface ISourceItemsHandler : IWorkspaceUpdateHandler
{
    /// <summary>
    ///     Handles the specified set of changes to source items, and applies them
    ///     to the underlying <see cref="IWorkspaceProjectContext"/>.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="IWorkspaceProjectContext"/> to update.
    /// </param>
    /// <param name="projectChanges">
    ///     A dictionary of <see cref="IProjectChangeDescription"/> representing the set of
    ///     changes made to the project, keyed by their item type.
    /// </param>
    /// <param name="state">
    ///     A <see cref="ContextState"/> describing the state of the <see cref="IWorkspaceProjectContext"/>.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IManagedProjectDiagnosticOutputService"/> for logging to the log.
    /// </param>
    void Handle(IWorkspaceProjectContext context, IImmutableDictionary<string, IProjectChangeDescription> projectChanges, ContextState state, IManagedProjectDiagnosticOutputService logger);
}
