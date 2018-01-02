// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Packaging
{
    internal partial class ManagedProjectSystemPackage
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

            public static void RegisterTraceListener()
            {
                // There's no public API registering a trace listener for a 
                // non-public trace source, so we need to use reflection
                string assemblyName = typeof(AppliesToAttribute).Assembly.FullName;

                var type = Type.GetType($"Microsoft.VisualStudio.ProjectSystem.TraceUtilities, {assemblyName}");
                FieldInfo field = type.GetField("Source", BindingFlags.NonPublic | BindingFlags.Static);

                var source = (TraceSource)field.GetValue(null);

                source.Switch.Level = SourceLevels.Warning;
                source.Listeners.Add(new DebuggerTraceListener());
            }
        }
    }
}

#endif
