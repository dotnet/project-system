// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.Test.Apex;
using Microsoft.Test.Apex.Services;
using Microsoft.Test.Apex.Services.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.Diagnostics
{
    /// <summary>
    ///     Avoids logging "known" warnings that only make investigating real errors harder.
    /// </summary>
    [Export(typeof(ITestLoggerSink))]
    [Export(typeof(WarningIgnorerLogger))]
    [Export(typeof(TestContextLogger))]
    [Export(typeof(IExecutionScopeSink))]
    public class WarningIgnorerLogger : TestContextLogger, ITestLoggerSink
    {
        // Other than WriteEntry(SinkEntryType, String), everything else is delegated to the base
        public WarningIgnorerLogger()
        {
        }

        public new void WriteEntry(SinkEntryType entryType, string message)
        {
            if (IsKnownLifeTimeActionWarning(entryType, message))
                return;

            WriteEntry(entryType, LogMessageHelpers.EscapeCurlyBraces(message), string.Empty);
        }

        internal void SetTestContext(TestContext context)
        {
            // Set the base TestContext
            var property = typeof(TestContextLogger).GetProperty("TestContext", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property is null)
                throw new InvalidOperationException("Unable to find TestContextLogger.TestContext. Has it been renamed?");

            property.SetValue(this, context);
        }

        private bool IsKnownLifeTimeActionWarning(SinkEntryType entryType, string message)
        {
            // We have lifetime actions that we have no control over that fail to run due to the lack of
            // elevated process, we don't output these warnings to the log as it only confuses investigations.

            if (entryType == SinkEntryType.Warning)
            {
                if (message.Contains("Could not find path to the Apex dependency libraries to install 'Microsoft.Test.Apex.ElevateClient'"))
                    return true;

                if (message.Contains("CodeMarker Libraries Installation Failure"))
                    return true;

                if (message.Contains("to 'Microsoft.Internal.Performance.CodeMarkers.dll' to enable CodeMarkers"))
                    return true;

                if (message.Contains("Unable to locate a file named 'Microsoft.Test.Apex.ElevateClient.exe' within the avaliable probing directories"))
                    return true;
            }

            return false;
        }
    }
}
