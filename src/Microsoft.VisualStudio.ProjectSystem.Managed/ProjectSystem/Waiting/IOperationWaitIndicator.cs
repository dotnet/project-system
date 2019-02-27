// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    /// <summary>
    /// Wait for an operation to complete, showing a message to the user
    /// allowing them to cancel the operation after a short delay.
    /// </summary>
    internal interface IOperationWaitIndicator
    {
        /// <summary>
        ///     Synchronously wait for the specified operation to complete.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="action">
        ///     The <see cref="Action"/> to run that represents the operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the action
        ///     and it is expected that the <see cref="Action"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        void WaitForOperation(string title, string message, bool allowCancel, Action<CancellationToken> action);

        /// <summary>
        ///     Synchronously wait for the specified operation to complete. Returning the result of the operation.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <typeparam name="T">
        ///     The type the operation returns.
        /// </typeparam>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="function">
        ///     The <see cref="Func{CancellationToken, T}"/> to run that represents the operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the action
        ///     and it is expected that the <see cref="Func{CancellationToken, T}"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="function"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>The result of the operation of type <see typeparam="T"/></returns>
        T WaitForOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function);

        /// <summary>
        ///     Synchronously wait for the specified <see langword="async"/> operation to complete.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="asyncFunction">
        ///     The <see cref="Func{CancellationToken, Task}"/> to run that represents the <see langword="async"/> operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the <see langword="async"/>
        ///     function and it is expected that the <see cref="Func{CancellationToken, Task}"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="asyncFunction"/> is <see langword="null"/>.
        /// </exception>
        void WaitForAsyncOperation(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction);

        /// <summary>
        ///     Synchronously wait for the specified <see langword="async"/> operation to complete.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <typeparam name="T">
        ///     The type the operation returns.
        /// </typeparam>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="asyncFunction">
        ///     The <see cref="Func{CancellationToken, Task}"/> to run that represents the <see langword="async"/> operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the <see langword="async"/>
        ///     function and it is expected that the <see cref="Func{CancellationToken, Task}"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="asyncFunction"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>The result of the operation of type <see typeparam="T"/></returns>
        T WaitForAsyncOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction);

        /// <summary>
        ///     Synchronously wait for the specified operation to complete.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="action">
        ///     The <see cref="Action"/> to run that represents the operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the action
        ///     and it is expected that the <see cref="Action"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>A <see cref="WaitIndicatorResult"/> that indicates if the operation completed or was canceled.</returns>
        WaitIndicatorResult WaitForOperationWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action);

        /// <summary>
        ///     Synchronously wait for the specified operation to complete. Returning the result of the operation.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <typeparam name="T">
        ///     The type the operation returns.
        /// </typeparam>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="function">
        ///     The <see cref="Func{CancellationToken, T}"/> to run that represents the operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the action
        ///     and it is expected that the <see cref="Func{CancellationToken, T}"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="function"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>A <see cref="ValueTuple{WaitIndicatorResult, T}"/> that contains both the result of the operation and whether the wait was canceled or completed.</returns>
        (WaitIndicatorResult, T) WaitForOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function);

        /// <summary>
        ///     Synchronously wait for the specified <see langword="async"/> operation to complete.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="asyncFunction">
        ///     The <see cref="Func{CancellationToken, Task}"/> to run that represents the <see langword="async"/> operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the <see langword="async"/>
        ///     function and it is expected that the <see cref="Func{CancellationToken, Task}"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="asyncFunction"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>A <see cref="WaitIndicatorResult"/> that indicates if the operation completed or was canceled.</returns>
        WaitIndicatorResult WaitForAsyncOperationWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction);

        /// <summary>
        ///     Synchronously wait for the specified <see langword="async"/> operation to complete.
        ///     Showing a message to the user explaining that an operation is in progress.
        /// </summary>
        /// <typeparam name="T">
        ///     The type the operation returns.
        /// </typeparam>
        /// <param name="title">
        ///     The title of the operation that the user is waiting on.
        /// </param>
        /// <param name="message">
        ///     The message to display to the user explaining what they are waiting on.
        /// </param>
        /// <param name="allowCancel">
        ///     Allow the user to cancel the operation if set to true, else the operation is uncancelable.
        /// </param>
        /// <param name="asyncFunction">
        ///     The <see cref="Func{CancellationToken, Task}"/> to run that represents the <see langword="async"/> operation
        ///     on which the user is waiting. A <see cref="CancellationToken"/> is passed to the <see langword="async"/>
        ///     function and it is expected that the <see cref="Func{CancellationToken, Task}"/> passed in respects cancellation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="title"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="message"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="asyncFunction"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>A <see cref="ValueTuple{WaitIndicatorResult, T}"/> that contains both the result of the operation and whether the wait was canceled or completed.</returns>
        (WaitIndicatorResult, T) WaitForAsyncOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction);
    }
}
