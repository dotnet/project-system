// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Handles changes to the project system, and applies them to an
    ///     <see cref="IWorkspaceProjectContext"/> instance.
    /// </summary>
    internal abstract class AbstractContextHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AbstractContextHandler"/> class.
        /// </summary>
        protected AbstractContextHandler()
        {
        }

        /// <summary>
        ///     Gets the <see cref="IWorkspaceProjectContext"/> that 
        ///     this handler applies changes.
        /// </summary>
        public IWorkspaceProjectContext Context
        {
            get;
            private set;
        }

        /// <summary>
        ///     Initializes the <see cref="AbstractContextHandler"/> with the specified
        ///     <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="IWorkspaceProjectContext"/> that the handler
        ///     should use to apply changes.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Initialize(IWorkspaceProjectContext)"/> has already been 
        ///     previously called.
        /// </exception>
        public void Initialize(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            if (Context != null)
                throw new InvalidOperationException();

            Context = context;
        }

        /// <summary>
        ///     Throws an <see cref="InvalidOperationException"/> if 
        ///     <see cref="Initialize(IWorkspaceProjectContext)"/> has not been called.
        /// </summary>
        protected void EnsureInitialized()
        {
            if (Context == null)
                throw new InvalidOperationException();
        }
    }
}
