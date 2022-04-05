// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

// Let inspection tools detect that CPS uses a SourceSwitch for tracing messages.
[assembly: Switch("RoslynProjectSystem", typeof(SourceSwitch))]

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// This class contains methods that are useful for logging.
    /// </summary>
    internal static class TraceUtilities
    {
        private const int CriticalTraceBufferSize = 32;

        /// <summary>
        /// The CPS trace source.
        /// </summary>
        internal static readonly TraceSource Source = new("CPS");

        /// <summary>
        /// Buffer to preserve latest set of error messages to help diagnosing Watson bugs.
        /// </summary>
        private static readonly string[] s_criticalTraceBuffer = new string[CriticalTraceBufferSize];
        private static volatile int s_currentTraceIndex;

        /// <summary>
        /// Gives the current Travel Level setting for the CPS tracing
        /// </summary>
        internal static SourceLevels CurrentLevel
        {
            get { return Source.Switch.Level; }
        }

        #region Tracing - Verbose

        /// <summary>
        /// Requests a verbose trace message to be written out to the listeners.
        /// </summary>
        /// <param name="formattedMessage">The message to be traced.</param>
        internal static void TraceVerbose(string formattedMessage)
        {
            Source.TraceEvent(TraceEventType.Verbose, 0, formattedMessage);
        }

        /// <summary>
        /// Requests a verbose trace message to be written out to the listeners.
        /// </summary>
        /// <param name="unformattedMessage">The unformatted message to be traced.</param>
        /// <param name="args">The arguments to be formatted into the message</param>
        internal static void TraceVerbose(string unformattedMessage, params object[] args)
        {
            Source.TraceEvent(TraceEventType.Verbose, 0, unformattedMessage, args);
        }

        #endregion

        #region Tracing - Warning

        /// <summary>
        /// Requests a warning trace message to be written out to the listeners.
        /// </summary>
        /// <param name="formattedMessage">The message to be traced.</param>
        internal static void TraceWarning(string formattedMessage)
        {
            RecordCriticalMessage(formattedMessage);
            Source.TraceEvent(TraceEventType.Warning, 0, formattedMessage);
        }

        /// <summary>
        /// Requests a warning trace message to be written out to the listeners.
        /// </summary>
        /// <param name="unformattedMessage">The unformatted message to be traced.</param>
        /// <param name="args">The arguments to be formatted into the message</param>
        internal static void TraceWarning(string unformattedMessage, params object[] args)
        {
            RecordCriticalMessage(unformattedMessage);
            Source.TraceEvent(TraceEventType.Warning, 0, unformattedMessage, args);
        }

        #endregion

        #region Tracing - Error

        /// <summary>
        /// Requests an error trace message to be written out to the listeners.
        /// </summary>
        /// <param name="formattedMessage">The message to be traced.</param>
        internal static void TraceError(string formattedMessage)
        {
            RecordCriticalMessage(formattedMessage);
            Source.TraceEvent(TraceEventType.Error, 0, formattedMessage);
        }

        /// <summary>
        /// Requests an error trace message to be written out to the listeners.
        /// </summary>
        /// <param name="unformattedMessage">The unformatted message to be traced.</param>
        /// <param name="args">The arguments to be formatted into the message</param>
        internal static void TraceError(string unformattedMessage, params object[] args)
        {
            RecordCriticalMessage(unformattedMessage);
            Source.TraceEvent(TraceEventType.Error, 0, unformattedMessage, args);
        }

        /// <summary>
        /// Requests an error trace message to be written out to the listeners
        /// </summary>
        internal static void TraceException(string formattedMessage, Exception e)
        {
            string message = e.ToString();

            if (e is AggregateException aggregateException)
            {
                message = aggregateException.Flatten().ToString();
            }

            if (!string.IsNullOrEmpty(formattedMessage))
            {
                TraceError(formattedMessage + ":" + message);
            }
            else
            {
                TraceError("Traced Exception:" + message);
            }
        }
        #endregion

        private static void RecordCriticalMessage(string message)
        {
            int currentValue;

            // Allocate the next index.  We use CompareExchange here to prevent the race condition between two threads.
            do
            {
                currentValue = s_currentTraceIndex;
            }
            while (Interlocked.CompareExchange(ref s_currentTraceIndex, currentValue, (currentValue + 1) % CriticalTraceBufferSize) != currentValue);

            // possible to override, if the buffer is written heavily
            // but this is just to help us to gather information, so performance is more important here.
            s_criticalTraceBuffer[currentValue] = message;
        }
    }
}
