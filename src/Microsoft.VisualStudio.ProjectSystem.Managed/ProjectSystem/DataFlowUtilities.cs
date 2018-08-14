// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project flags.
    /// </summary>
    internal static class DataflowUtilities
    {
        /// <summary>
        ///     Returns a new instance of <see cref="DataflowLinkOptions"/> with 
        ///     <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        public static DataflowLinkOptions PropagateCompletion
        {
            get
            {
                // DataflowLinkOptions is mutable, make sure always create
                // a new copy to avoid accidentally currupting state
                return new DataflowLinkOptions()
                {
                    PropagateCompletion = true  //  // Make sure source block completion and faults flow onto the target block.
                };
            }
        }

        /// <summary>
        ///     Links the <see cref="ISourceBlock{TOutput}" /> to the specified <see cref="Action{T}" /> 
        ///     that can process messages, propagating completion and faults.
        /// </summary>
        /// <returns>
        ///     An <see cref="IDisposable"/> that, upon calling Dispose, will unlink the source from the target.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="target"/> is <see langword="null"/>.
        /// </exception>
        public static IDisposable LinkToAction<T>(this ISourceBlock<T> source, Action<T> target)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(target, nameof(target));

            return source.LinkTo(new ActionBlock<T>(target), PropagateCompletion);
        }

        /// <summary>
        ///     Links the <see cref="ISourceBlock{TOutput}" /> to the specified <see cref="Func{T, TResult}" /> 
        ///     that can process messages, propagating completion and faults.
        /// </summary>
        /// <returns>
        ///     An <see cref="IDisposable"/> that, upon calling Dispose, will unlink the source from the target.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="target"/> is <see langword="null"/>.
        /// </exception>
        public static IDisposable LinkToAsyncAction<T>(this ISourceBlock<T> source, Func<T, Task> target)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(target, nameof(target));

            return source.LinkTo(new ActionBlock<T>(target), PropagateCompletion);
        }

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
