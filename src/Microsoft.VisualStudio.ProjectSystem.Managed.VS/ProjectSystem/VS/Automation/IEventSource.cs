using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ComVisible(false)]
    internal interface IEventSource<SinkType>
        where SinkType : class
    {
        /// <summary>
        /// Undocumented.
        /// </summary>
        void OnSinkAdded(SinkType sink);

        /// <summary>
        /// Undocumented.
        /// </summary>
        void OnSinkRemoved(SinkType sink);
    }
}
