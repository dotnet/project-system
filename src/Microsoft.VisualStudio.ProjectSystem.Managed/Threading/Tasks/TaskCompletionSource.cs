// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <inheritdoc cref="TaskCompletionSource{TResult}"/>
    /// <remarks>
    ///     This provides the non-generic version of a <see cref="TaskCompletionSource{TResult}"/>
    /// </remarks>
    internal class TaskCompletionSource : TaskCompletionSource<object?>
    {
        /// <inheritdoc cref="TaskCompletionSource{TResult}.Task"/>
        public new Task Task
        {
            get { return base.Task; }
        }

        public TaskCompletionSource()
        {
        }

        public TaskCompletionSource(TaskCreationOptions taskCreationOptions)
            : base(taskCreationOptions)
        {
        }

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetResult(TResult)"/>
        public void SetResult()
        {
            base.SetResult(null);
        }

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/>
        public bool TrySetResult()
        {
            return base.TrySetResult(null);
        }

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetResult(TResult)"/>
        [Obsolete("Use TaskCompletionSource.SetResult()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new void SetResult(object? result)
        {
            base.SetResult(result);
        }

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetResult(TResult)"/>
        [Obsolete("Use TaskCompletionSource.TrySetResult()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new bool TrySetResult(object? result)
        {
            return base.TrySetResult(result);
        }
    }
}
