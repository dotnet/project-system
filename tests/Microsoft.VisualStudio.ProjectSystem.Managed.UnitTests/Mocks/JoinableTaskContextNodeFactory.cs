// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Threading
{
    internal static class JoinableTaskContextNodeFactory
    {
        public static JoinableTaskContextNode Create()
        {
#pragma warning disable VSSDK005
            var context = new JoinableTaskContext();
#pragma warning restore VSSDK005

            return new JoinableTaskContextNode(context);
        }
    }
}
