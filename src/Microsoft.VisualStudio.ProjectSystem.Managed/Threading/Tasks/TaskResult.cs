// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading
{
    /// <summary>
    ///     Provides sentinel Tasks that represent commonly returned values.
    /// </summary>
    internal static class TaskResult
    {
        /// <summary>
        ///     Represents a <see cref="Task{TResult}"/> that's completed successfully with the result of <see langword="false"/>.
        /// </summary>
        public static Task<bool> False => TplExtensions.FalseTask;

        /// <summary>
        ///     Represents a <see cref="Task{TResult}"/> that's completed successfully with the result of <see langword="true"/>.
        /// </summary>
        public static Task<bool> True => TplExtensions.TrueTask;
        
        /// <summary>
        ///     Represents a <see cref="Task{TResult}"/> that's completed successfully with result of the empty string.
        /// </summary>
        public static Task<string> EmptyString => Task.FromResult("");

        /// <summary>
        ///     Returns a <see cref="Task{TResult}"/> of type <typeparamref name="T" /> that's completed successfully with the result of <see langword="null"/>.
        /// </summary>
        public static Task<T?> Null<T>() where T : class => NullTaskResult<T>.Instance;

        private static class NullTaskResult<T> where T : class
        {
            public static readonly Task<T?> Instance = Task.FromResult<T?>(null);
        }

        /// <summary>
        ///     Returns a <see cref="Task{TResult}"/> whose value is an empty enumerable of type <typeparamref name="T" />.
        /// </summary>
        public static Task<IEnumerable<T>> EmptyEnumerable<T>() => EmptyEnumerableTaskResult<T>.Instance;

        private static class EmptyEnumerableTaskResult<T>
        {
            public static readonly Task<IEnumerable<T>> Instance = Task.FromResult(System.Linq.Enumerable.Empty<T>());
        }
    }
}
