// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Tracks the "active" <see cref="IWorkspaceProjectContext"/> for an <see cref="UnconfiguredProject"/>.
    /// </summary>
    /// <remarks>
    ///     The "active" <see cref="IWorkspaceProjectContext"/> is the one that Roslyn uses to drive IntelliSense, refactoring
    ///     and code fixes. This is typically controlled by the user via the project drop down in the top-left of the editor, but
    ///     can be changed in reaction to other factors.
    /// </remarks>
    internal interface IActiveWorkspaceProjectContextTracker
    {
        /// <summary>
        ///     Returns a value indicating whether the specified <see cref="IWorkspaceProjectContext"/> is the active one.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null" />
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="context"/> has not been registered or has already been unregistered.
        /// </exception>
        bool IsActiveContext(IWorkspaceProjectContext context);

        /// <summary>
        ///     Registers the <see cref="IWorkspaceProjectContext"/> with the tracker.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null" />.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="contextId"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contextId"/> is an empty string ("").
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="context"/> has already been been registered.
        /// </exception>
        void RegisterContext(IWorkspaceProjectContext context, string contextId);

        /// <summary>
        ///     Unregisters the <see cref="IWorkspaceProjectContext"/> with the tracker.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null" />
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="context"/> has not been registered or has already been unregistered.
        /// </exception>
        void UnregisterContext(IWorkspaceProjectContext context);
    }
}
