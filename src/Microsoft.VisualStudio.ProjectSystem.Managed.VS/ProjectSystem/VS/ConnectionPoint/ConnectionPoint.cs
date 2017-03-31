// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint
{
    /// <summary>
    /// This implementation is a copy from CPS
    /// </summary>
    internal class ConnectionPoint<SinkType> : IConnectionPoint
            where SinkType : class
    {
        private Dictionary<uint, SinkType> sinks;

        private uint nextCookie;

        private ConnectionPointContainer container;

        private IEventSource<SinkType> source;

        internal ConnectionPoint(ConnectionPointContainer container, IEventSource<SinkType> source)
        {
            Requires.NotNull(container, nameof(container));
            Requires.NotNull(source, nameof(source));

            this.container = container;
            this.source = source;
            sinks = new Dictionary<uint, SinkType>();
            nextCookie = 1;
        }

        public void Advise(object pUnkSink, out uint pdwCookie)
        {
            SinkType sink = pUnkSink as SinkType;
            if (null == sink)
            {
                Marshal.ThrowExceptionForHR(VSConstants.E_NOINTERFACE);
            }

            sinks.Add(nextCookie, sink);
            pdwCookie = nextCookie;
            source.OnSinkAdded(sink);
            nextCookie += 1;
        }

        public void EnumConnections(out IEnumConnections ppEnum)
        {
            throw new NotImplementedException();
        }

        public void GetConnectionInterface(out Guid pIID)
        {
            pIID = typeof(SinkType).GUID;
        }

        public void GetConnectionPointContainer(out IConnectionPointContainer ppCPC)
        {
            ppCPC = container;
        }

        public void Unadvise(uint dwCookie)
        {
            // This will throw if the cookie is not in the list.
            SinkType sink = sinks[dwCookie];
            sinks.Remove(dwCookie);
            source.OnSinkRemoved(sink);
        }
    }
}
