// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    internal interface IWaitContext : IDisposable
    {
        /// <summary>
        ///     Gets a cancellation token that represents the user's request for cancellation.
        /// </summary>
        /// <remarks>
        ///     If the wait operation does not support cancellation, this will be <see cref="CancellationToken.None"/>.
        /// </remarks>
        CancellationToken CancellationToken { get; }

        /// <summary>
        ///     Allows the operation being waited on to update the ongoing operation's status for the user.
        /// </summary>
        /// <param name="message">The message to display, or <see langword="null"/> if no change is required.</param>
        /// <param name="currentStep">The current step's (one-based) index, or <see langword="null"/> if no change is required.</param>
        /// <param name="totalSteps">The total number of steps, or <see langword="null"/> if no change is required.</param>
        /// <param name="progressText">A progress messate display, or <see langword="null"/> if no change is required.</param>
        void Update(
            string? message = null,
            int? currentStep = null,
            int? totalSteps = null,
            string? progressText = null);
    }
}
