// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    /// <summary>
    /// A dispose-able context object that considers the operation on which it is
    /// waiting to be complete once dispose it called.
    /// </summary>
    internal interface IWaitContext : IDisposable
    {
        /// <summary>
        /// A cancellation token that can indicates whether the underlying operation has been canceled.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Allow or disallow canceling the underlying operation.
        /// </summary>
        bool AllowCancel { get; set; }

        /// <summary>
        /// The message explaining what the underlying operation is doing.
        /// </summary>
        string Message { get; set; }
    }
}
