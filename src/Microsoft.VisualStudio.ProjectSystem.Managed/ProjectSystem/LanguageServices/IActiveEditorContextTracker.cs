// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Tracks the "active" <see cref="IWorkspaceProjectContext"/> for the editor within an <see cref="UnconfiguredProject"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The "active" context for the editor is the one that Roslyn uses to drive IntelliSense, refactorings
    ///         and code fixes. This is typically controlled by the user via the project drop down in the top-left
    ///         of the editor, but can be changed in reaction to other factors.
    ///     </para>
    ///     <para>
    ///         NOTE: This is distinct from the "active" context for an <see cref="UnconfiguredProject"/> which is
    ///         tracked via <see cref="IActiveWorkspaceProjectContextHost"/>.
    ///     </para>
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IActiveEditorContextTracker
    {
        /// <summary>
        ///     Returns a value indicating whether the specified <see cref="IWorkspaceProjectContext"/> is the active one for the editor.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contextId"/> is <see langword="null" />
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contextId"/> is an empty string ("").
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="contextId"/> has not been registered or has already been unregistered.
        /// </exception>
        bool IsActiveEditorContext(string contextId);

        /// <summary>
        ///     Registers the <see cref="IWorkspaceProjectContext"/> with the tracker.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contextId"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contextId"/> is an empty string ("").
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="contextId"/> has already been registered.
        /// </exception>
        void RegisterContext(string contextId);

        /// <summary>
        ///     Unregisters the <see cref="IWorkspaceProjectContext"/> with the tracker.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contextId"/> is <see langword="null" />
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contextId"/> is an empty string ("").
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="contextId"/> has not been registered or has already been unregistered.
        /// </exception>
        void UnregisterContext(string contextId);
    }
}
