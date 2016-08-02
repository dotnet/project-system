// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Diagnostics
{
    partial class DogfoodProjectSystemPackage
    {
        private class DebuggerTraceListener : TraceListener
        {
            internal DebuggerTraceListener()
            {
            }

            public override void Write(string message)
            {
                if (Debugger.IsLogging())
                {
                    Debugger.Log(0, null, message);
                }
            }

            public override void WriteLine(string message)
            {
                Write(message + Environment.NewLine);
            }
        }
    }
}
