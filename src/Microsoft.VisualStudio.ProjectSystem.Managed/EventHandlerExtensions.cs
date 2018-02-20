// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Provides extension methods for event handlers.
    /// </summary>
    internal static class EventHandlerExtensions
    {
        /// <summary>
        ///     Invokes any event handlers that are hooked to the specified event.
        /// </summary>
        /// <param name="handler">
        ///     The event. Can be <see langword="null"/>.
        /// </param>
        /// <param name="sender">
        ///     The value to pass as the sender of the event. 
        /// </param>
        /// <param name="e">
        ///     The <see cref="EventArgs"/>.
        /// </param>
        public static Task RaiseAsync(this AsyncEventHandler handler, object sender, EventArgs e)
        {
            if (handler != null)
            {
                Delegate[] invocationList = handler.GetInvocationList();
                if (invocationList.Length > 0)
                {
                    var tasks = new Task[invocationList.Length];

                    for (int i = 0;  i < invocationList.Length; i++)
                    {
                        var asyncHandler = (AsyncEventHandler)invocationList[i];
                        tasks[i] = asyncHandler(sender, e);
                    }

                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

    }
}
