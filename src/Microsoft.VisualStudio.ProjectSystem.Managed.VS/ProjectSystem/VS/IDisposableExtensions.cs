// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IDisposableExtensions
    {
        /// <summary>
        /// Calls the <see cref="IDisposable.Dispose"/> method on an object, allowing the object to be null.
        /// </summary>
        internal static void DisposeIfNotNull(this IDisposable value)
        {
            if (value != null)
            {
                value.Dispose();
            }
        }

        /// <summary>
        /// Calls the <see cref="IAsyncDisposable.DisposeAsync"/> method on an object, allowing the object to be null.
        /// </summary>
        internal static async Task DisposeIfNotNullAsync(this IAsyncDisposable value)
        {
            if (value != null)
            {
                await value.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
