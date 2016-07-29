// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Diagnostics
{
    /// <summary>
    ///     Registers a trace listener to the CPS trace source that writes to the debugger.
    /// </summary>
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    internal partial class DogfoodProjectSystemPackage : AsyncPackage
    {
        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            RegisterTraceListener();
            return Task.CompletedTask;
        }

        private void RegisterTraceListener()
        {
            // There's no public API registering a trace listener for a 
            // non-public trace source, so we need to use reflection
            string assemblyName = typeof(AppliesToAttribute).Assembly.FullName;

            Type type = Type.GetType($"Microsoft.VisualStudio.ProjectSystem.TraceUtilities, {assemblyName}");
            FieldInfo field = type.GetField("Source", BindingFlags.NonPublic | BindingFlags.Static);

            TraceSource source = (TraceSource)field.GetValue(null);

            source.Switch.Level = SourceLevels.Warning;
            source.Listeners.Add(new DebuggerTraceListener());
        }
    }
}
