// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft;

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

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, TOut, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOut<TOut, TResult> action)
              where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, T1, TOut, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOut<T1, TOut, TResult> action)
            where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, T1, TOut1, TOut2, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOut<T1, TOut1, TOut2, TResult> action)
            where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, T1, T2, TOut1, TOut2, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOut<T1, T2, TOut1, TOut2, TResult> action)
            where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        public static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn, TOut1, TOut2, TOut3, TResult>(this IReturns<TMock, TReturn> valueFunction, FuncWithOutThreeArgs<TOut1, TOut2, TOut3, TResult> action)
            where TMock : class
        {
            return Returns(valueFunction, (object)action);
        }

        private static IReturnsThrows<TMock, TReturn> Returns<TMock, TReturn>(IReturns<TMock, TReturn> valueFunction, object action)
            where TMock : class
        {
            valueFunction.GetType()
                         .InvokeMember("SetReturnDelegate", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, valueFunction, new[] { action });

            return (IReturnsThrows<TMock, TReturn>)valueFunction;
        }
    }
}
