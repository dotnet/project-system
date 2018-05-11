// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project flags.
    /// </summary>
    internal static class DataflowUtilities
    {
        /// <summary>
        /// Wraps a delegate in a repeatably executable delegate that runs within an ExecutionContext captured at the time of *this* method call.
        /// </summary>
        /// <typeparam name="TInput">The type of input parameter that is taken by the delegate.</typeparam>
        /// <param name="function">The delegate to invoke when the returned delegate is invoked.</param>
        /// <returns>The wrapper delegate.</returns>
        /// <remarks>
        /// This is useful because Dataflow doesn't capture or apply ExecutionContext for its delegates,
        /// so the delegate runs in whatever ExecutionContext happened to call ITargetBlock.Post, which is
        /// never the behavior we actually want. We've been bitten several times by bugs due to this.
        /// Ironically, in Dataflow's early days it *did* have the desired behavior but they removed it
        /// when they pulled it out of the Framework so it could be 'security transparent'.
        /// By passing block delegates through this wrapper, we can reattain the old behavior.
        /// </remarks>
        internal static Func<TInput, Task> CaptureAndApplyExecutionContext<TInput>(Func<TInput, Task> function)
        {
            var context = ExecutionContext.Capture();
            return input =>
            {
                SynchronizationContext currentSynchronizationContext = SynchronizationContext.Current;
                using (ExecutionContext copy = context.CreateCopy())
                {
                    Task result = null;
                    ExecutionContext.Run(
                        copy,
                        state =>
                        {
                            SynchronizationContext.SetSynchronizationContext(currentSynchronizationContext);
                            result = function(input);
                        },
                        null);
                    return result;
                }
            };
        }
    }
}
