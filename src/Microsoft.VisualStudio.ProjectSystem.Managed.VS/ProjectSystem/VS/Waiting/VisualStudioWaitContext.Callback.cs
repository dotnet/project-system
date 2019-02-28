// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    internal partial class VisualStudioWaitContext
    {
        private class Callback : IVsThreadedWaitDialogCallback
        {
            private readonly VisualStudioWaitContext _waitContext;

            public Callback(VisualStudioWaitContext waitContext)
            {
                _waitContext = waitContext;
            }

            public void OnCanceled()
            {
                _waitContext.OnCanceled();
            }
        }
    }
}
