using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    /// Undocumented.
    /// </summary>
    [ComVisible(true)]
    public class ConnectionPointContainer : IConnectionPointContainer
    {
        /// <summary>
        /// Undocumented.
        /// </summary>
        private Dictionary<Guid, IConnectionPoint> connectionPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPointContainer"/> class.
        /// </summary>
        internal ConnectionPointContainer()
        {
            connectionPoints = new Dictionary<Guid, IConnectionPoint>();
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        internal void AddEventSource<SinkType>(IEventSource<SinkType> source)
            where SinkType : class
        {
            Requires.NotNull(source, nameof(source));
            Verify.Operation(!connectionPoints.ContainsKey(typeof(SinkType).GUID), "EventSource guid already added to the list of connection points");

            connectionPoints.Add(typeof(SinkType).GUID, new ConnectionPoint<SinkType>(this, source));
        }

        #region IConnectionPointContainer Members

        /// <summary>
        /// Undocumented.
        /// </summary>
        void IConnectionPointContainer.EnumConnectionPoints(out IEnumConnectionPoints ppEnum)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        void IConnectionPointContainer.FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP)
        {
            ppCP = connectionPoints[riid];
        }

        #endregion
    }
}
