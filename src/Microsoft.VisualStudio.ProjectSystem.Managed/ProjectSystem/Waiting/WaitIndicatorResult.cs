// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    /// <summary>
    ///     Represents the result of <see cref="IWaitIndicator.Run(string, string, bool, Func{IWaitContext, System.Threading.Tasks.Task}, int)"/>.
    /// </summary>
    internal readonly struct WaitIndicatorResult
    {
        public static readonly WaitIndicatorResult Cancelled = new(isCancelled: true);
        public static readonly WaitIndicatorResult Completed = new(isCancelled: false);

        private readonly bool _isCancelled;

        private WaitIndicatorResult(bool isCancelled)
        {
            _isCancelled = isCancelled;
        }

        /// <summary>
        ///     Gets a value indicating whether the operation was cancelled, either 
        ///     by the user or the operation itself.
        /// </summary>
        public bool IsCancelled => _isCancelled;
    }

    /// <summary>
    ///     Represents the result of <see cref="IWaitIndicator.Run{T}(string, string, bool, Func{IWaitContext, System.Threading.Tasks.Task{T}}, int)"/>.
    /// </summary>
    internal readonly struct WaitIndicatorResult<T>
    {
        public static readonly WaitIndicatorResult<T> Cancelled = new(isCancelled: true, result: default!);

        private readonly bool _isCancelled;
        private readonly T _result;

        private WaitIndicatorResult(bool isCancelled, T result)
        {
            _isCancelled = isCancelled;
            _result = result;
        }

        /// <summary>
        ///     Gets a value indicating whether the operation was cancelled, either 
        ///     by the user or the operation itself.
        /// </summary>
        public bool IsCancelled => _isCancelled;

        /// <summary>
        ///     Gets the result of the operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="IsCancelled"/> is <see langword="true"/>.
        /// </exception>
        public T Result
        {
            get
            {
                Verify.Operation(!_isCancelled, "Cannot get the result of a cancelled operation.");

                return _result;
            }
        }

        public static WaitIndicatorResult<T> FromResult(T result) => new(isCancelled: false, result: result);
    }
}
