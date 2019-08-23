// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft
{
    public delegate TResult FuncWithOut<TOut, TResult>(out TOut result);
    public delegate TResult FuncWithOut<in T1, TOut, TResult>(T1 arg1, out TOut result);
    public delegate TResult FuncWithOut<in T1, TOut1, TOut2, TResult>(T1 arg1, out TOut1 result1, out TOut2 result2);
    public delegate TResult FuncWithOut<in T1, in T2, TOut1, TOut2, TResult>(T1 arg1, T2 arg2, out TOut1 result1, out TOut2 result2);
    public delegate TResult FuncWithOutThreeArgs<TOut1, TOut2, TOut3, TResult>(out TOut1 result1, out TOut2 result2, out TOut3 result3);
}
