// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Export(typeof(IPackageService))]
    internal sealed class DebuggerTraceListener : TraceListener, IPackageService
    {
        public Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            // There's no public API registering a trace listener for a 
            // non-public trace source, so we need to use reflection
            string assemblyName = typeof(AppliesToAttribute).Assembly.FullName;

            var type = Type.GetType($"Microsoft.VisualStudio.ProjectSystem.TraceUtilities, {assemblyName}");
            Assumes.NotNull(type);

            FieldInfo field = type.GetField("Source", BindingFlags.NonPublic | BindingFlags.Static);
            Assumes.NotNull(field);

            var source = (TraceSource)field.GetValue(null);

            source.Switch.Level = SourceLevels.Warning;
            source.Listeners.Add(this);

            return Task.CompletedTask;
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
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, message + Environment.NewLine);
            }
        }
    }
}

#endif
