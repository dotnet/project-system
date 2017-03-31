// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint
{
    /// <summary>
    /// This implementation is a copy from CPS
    /// </summary>
    [ComVisible(false)]
    internal interface IEventSource<SinkType>
        where SinkType : class
    {
        void OnSinkAdded(SinkType sink);

        void OnSinkRemoved(SinkType sink);
    }
}
