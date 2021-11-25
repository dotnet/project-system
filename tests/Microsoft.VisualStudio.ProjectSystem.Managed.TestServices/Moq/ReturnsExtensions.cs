// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq.Language;
using Moq.Language.Flow;

namespace Moq
{
    internal static class ReturnsExtensions
    {
        public static IReturnsResult<TMock> ReturnsAsync<TMock>(this IReturns<TMock, Task> mock, Action action) where TMock : class
        {
            return mock.Returns(() => { action(); return Task.CompletedTask; });
        }

        public static IReturnsResult<TMock> ReturnsAsync<TMock, T1>(this IReturns<TMock, Task> mock, Action<T1> action) where TMock : class
        {
            return mock.Returns((T1 arg1) => { action(arg1); return Task.CompletedTask; });
        }

        public static IReturnsResult<TMock> ReturnsAsync<TMock, T1, T2>(this IReturns<TMock, Task> mock, Action<T1, T2> action) where TMock : class
        {
            return mock.Returns((T1 arg1, T2 arg2) => { action(arg1, arg2); return Task.CompletedTask; });
        }

        public static IReturnsResult<TMock> ReturnsAsync<TMock, T1, T2, T3>(this IReturns<TMock, Task> mock, Action<T1, T2, T3> action) where TMock : class
        {
            return mock.Returns((T1 arg1, T2 arg2, T3 arg3) => { action(arg1, arg2, arg3); return Task.CompletedTask; });
        }

        public static IReturnsResult<TMock> ReturnsAsync<TMock, T1, T2, T3, T4>(this IReturns<TMock, Task> mock, Action<T1, T2, T3, T4> action) where TMock : class
        {
            return mock.Returns((T1 arg1, T2 arg2, T3 arg3, T4 arg4) => { action(arg1, arg2, arg3, arg4); return Task.CompletedTask; });
        }

        public static IReturnsResult<TMock> ReturnsAsync<TMock, T1, T2, T3, T4, T5>(this IReturns<TMock, Task> mock, Action<T1, T2, T3, T4, T5> action) where TMock : class
        {
            return mock.Returns((T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => { action(arg1, arg2, arg3, arg4, arg5); return Task.CompletedTask; });
        }
    }
}
