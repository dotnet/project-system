// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint
{
    /// <summary>
    /// This implementation is a copy from CPS
    /// </summary>
    [ComVisible(true)]
    public class ConnectionPointContainer : IConnectionPointContainer
    {
        private readonly Dictionary<Guid, IConnectionPoint> _connectionPoints = new();

        internal ConnectionPointContainer()
        {
        }

        internal void AddEventSource<SinkType>(IEventSource<SinkType> source)
            where SinkType : class
        {
            Requires.NotNull(source, nameof(source));
            Verify.Operation(!_connectionPoints.ContainsKey(typeof(SinkType).GUID), "EventSource guid already added to the list of connection points");

            _connectionPoints.Add(typeof(SinkType).GUID, new ConnectionPoint<SinkType>(this, source));
        }

        void IConnectionPointContainer.EnumConnectionPoints(out IEnumConnectionPoints ppEnum)
        {
            throw new NotImplementedException();
        }

        void IConnectionPointContainer.FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP)
        {
            ppCP = _connectionPoints[riid];
        }
    }
}
