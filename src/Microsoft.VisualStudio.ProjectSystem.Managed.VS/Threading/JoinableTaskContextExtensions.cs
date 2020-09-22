// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.Threading
{
    internal static class JoinableTaskContextExtensions
    {
        /// <summary>
        ///     Verifies that this method is called on the main ("UI") thread,
        ///     and throws an exception if not.
        /// </summary>
        public static void VerifyIsOnMainThread(this JoinableTaskContext joinableTaskContext)
        {
            Requires.NotNull(joinableTaskContext, nameof(joinableTaskContext));

            if (!joinableTaskContext.IsOnMainThread)
            {
                throw new COMException("This method must be called on the UI thread.", HResult.WrongThread);
            }
        }
    }
}
