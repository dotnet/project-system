// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Threading
{
    internal static class JoinableTaskContextNodeFactory
    {
        public static JoinableTaskContextNode Create()
        {
            var context = new JoinableTaskContext();

            return new JoinableTaskContextNode(context);
        }
    }
}
