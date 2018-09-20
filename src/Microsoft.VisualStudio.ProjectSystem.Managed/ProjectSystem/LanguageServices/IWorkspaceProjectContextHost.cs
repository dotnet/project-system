// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts an <see cref="IWorkspaceProjectContext"/> for a <see cref="ConfiguredProject"/> and provides consumers access to it.
    /// </summary>
    internal interface IWorkspaceProjectContextHost
    {
        /// <summary>
        ///     Gets a task that is completed when current <see cref="IWorkspaceProjectContextHost"/> has 
        ///     completed loading.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and the <see cref="ConfiguredProject"/> is unloaded.
        /// </exception>
        Task Loaded
        {
            get;
        }

        /// <summary>
        ///     Opens the <see cref="IWorkspaceProjectContext"/>, passing it to the specified action for writing.
        /// </summary>
        /// <param name="action">
        ///     The <see cref="Func{T, TResult}"/> to run while holding the lock.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and the <see cref="ConfiguredProject"/> is unloaded.
        /// </exception>
        Task OpenContextForWriteAsync(Func<IWorkspaceProjectContext, Task> action);
    }
}
