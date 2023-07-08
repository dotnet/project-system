// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#if DEBUG

using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;

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
            string typeName = $"Microsoft.VisualStudio.ProjectSystem.TraceUtilities, {assemblyName}";

            var type = Type.GetType(typeName);
            if (type is null)
            {
                Assumes.Fail($"Could not find type '{typeName}'");
            }

            const string sourcePropertyName = "Source";
            PropertyInfo? property = type.GetProperty(sourcePropertyName, BindingFlags.NonPublic | BindingFlags.Static);
            if (property is null)
            {
                Assumes.Fail($"Could not find property '{sourcePropertyName}' in type '{typeName}'");
            }

            var source = (TraceSource)property.GetValue(null);

            source.Switch.Level = SourceLevels.Warning;
            source.Listeners.Add(this);

            return Task.CompletedTask;
        }

        public override void Write(string message)
        {
            if (System.Diagnostics.Debugger.IsLogging())
            {
                System.Diagnostics.Debugger.Log(0, null, message);
            }
        }

        public override void WriteLine(string message)
        {
            if (System.Diagnostics.Debugger.IsLogging())
            {
                System.Diagnostics.Debugger.Log(0, null, message + Environment.NewLine);
            }
        }
    }
}

#endif
