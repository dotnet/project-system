using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    internal class ConnectionPoint<SinkType> : IConnectionPoint
            where SinkType : class
    {
        /// <summary>
        /// Undocumented.
        /// </summary>
        private Dictionary<uint, SinkType> sinks;

        /// <summary>
        /// Undocumented.
        /// </summary>
        private uint nextCookie;

        /// <summary>
        /// Undocumented.
        /// </summary>
        private ConnectionPointContainer container;

        /// <summary>
        /// Undocumented.
        /// </summary>
        private IEventSource<SinkType> source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoint{SinkType}"/> class.
        /// </summary>
        internal ConnectionPoint(ConnectionPointContainer container, IEventSource<SinkType> source)
        {
            Requires.NotNull(container, nameof(container));
            Requires.NotNull(source, nameof(source));

            this.container = container;
            this.source = source;
            sinks = new Dictionary<uint, SinkType>();
            nextCookie = 1;
        }

        #region IConnectionPoint Members

        /// <summary>
        /// Undocumented.
        /// </summary>
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

        /// <summary>
        /// Undocumented.
        /// </summary>
        public void EnumConnections(out IEnumConnections ppEnum)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        public void GetConnectionInterface(out Guid pIID)
        {
            pIID = typeof(SinkType).GUID;
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        public void GetConnectionPointContainer(out IConnectionPointContainer ppCPC)
        {
            ppCPC = container;
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        public void Unadvise(uint dwCookie)
        {
            // This will throw if the cookie is not in the list.
            SinkType sink = sinks[dwCookie];
            sinks.Remove(dwCookie);
            source.OnSinkRemoved(sink);
        }

        #endregion
    }
}
