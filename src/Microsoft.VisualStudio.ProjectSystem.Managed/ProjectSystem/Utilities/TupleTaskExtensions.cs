// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Security;

namespace Microsoft.VisualStudio.ProjectSystem;

internal static class TupleTaskExtensions
{
    /// <summary>
    /// Runs the tasks in the tuple concurrently, returning their values as a tuple.
    /// </summary>
    /// <typeparam name="T1">Return type of the first task's result.</typeparam>
    /// <typeparam name="T2">Return type of the second task's result.</typeparam>
    /// <param name="tasks">A tuple of tasks to return an awaiter for.</param>
    /// <returns>An awaiter for the tasks in the tuple.</returns>
    public static TupleTaskAwaiter<T1, T2> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tasks)
    {
        return new(tasks);
    }

    public readonly struct TupleTaskAwaiter<T1, T2> : ICriticalNotifyCompletion
    {
        private readonly (Task<T1>, Task<T2>) _tasks;

        private readonly TaskAwaiter _whenAllAwaiter;

        public TupleTaskAwaiter((Task<T1>, Task<T2>) tasks)
        {
            _tasks = tasks;
            _whenAllAwaiter = Task.WhenAll(tasks.Item1, tasks.Item2).GetAwaiter();
        }

        public bool IsCompleted => _whenAllAwaiter.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            _whenAllAwaiter.OnCompleted(continuation);
        }

        [SecurityCritical]
        public void UnsafeOnCompleted(Action continuation)
        {
            _whenAllAwaiter.UnsafeOnCompleted(continuation);
        }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        public (T1, T2) GetResult()
        {
            _whenAllAwaiter.GetResult();

            return (_tasks.Item1.Result, _tasks.Item2.Result);
        }
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }
}
