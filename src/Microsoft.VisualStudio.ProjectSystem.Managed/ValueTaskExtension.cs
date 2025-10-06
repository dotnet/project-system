// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace System.Threading.Tasks;

internal static partial class RoslynValueTaskExtensions
{
#if NET // binary compatibility
    public static ValueTask<T> FromResult<T>(T result)
        => ValueTask.FromResult(result);

    public static ValueTask CompletedTask
        => ValueTask.CompletedTask;
#else
    extension(ValueTask)
    {
        public static ValueTask<T> FromResult<T>(T result)
            => new(result);

        public static ValueTask CompletedTask
            => new();
    }
#endif
}
