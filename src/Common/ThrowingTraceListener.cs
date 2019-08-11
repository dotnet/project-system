// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Diagnostics
{
    // To enable this for a process, add the following to the app.config for the project:
    //
    // <configuration>
    //  <system.diagnostics>
    //    <trace>
    //      <listeners>
    //        <remove name="Default" />
    //        <add name="ThrowingTraceListener" type="Microsoft.VisualStudio.Diagnostics.ThrowingTraceListener, Microsoft.VisualStudio.ProjectSystem.Managed.TestServices" />
    //      </listeners>
    //    </trace>
    //  </system.diagnostics>
    //</configuration>
    public sealed class ThrowingTraceListener : TraceListener
    {
        public override void Fail(string message, string detailMessage)
        {
            throw new DebugAssertFailureException(message + Environment.NewLine + detailMessage);
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        [Serializable]
        public class DebugAssertFailureException : Exception
        {
            public DebugAssertFailureException()
            {
            }

            public DebugAssertFailureException(string message) : base(message)
            {
            }

            public DebugAssertFailureException(string message, Exception inner) : base(message, inner)
            {
            }

            protected DebugAssertFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
