// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint
{
    /// <summary>
    /// This implementation is a copy from CPS
    /// </summary>
    internal interface IEventSource<TSinkType>
        where TSinkType : class
    {
        void OnSinkAdded(TSinkType sink);

        void OnSinkRemoved(TSinkType sink);
    }
}
