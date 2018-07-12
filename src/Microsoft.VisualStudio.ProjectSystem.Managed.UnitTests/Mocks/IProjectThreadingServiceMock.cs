// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class IProjectThreadingServiceMock : IProjectThreadingService
    {

        internal static DispatchThread DispatchThread { get; private set; }

        static IProjectThreadingServiceMock()
        {
            // ThreadHelper in VS requires a persistent dispatcher thread.  Because
            // each unit test executes on a new thread, we create our own
            // persistent thread that acts like a UI thread. This will be invoked just
            // once for the module.
            DispatchThread = new DispatchThread();
        }

        public void ExecuteSynchronously(Func<Task> asyncAction)
        {
            asyncAction().GetAwaiter().GetResult();
        }

        public T ExecuteSynchronously<T>(Func<Task<T>> asyncAction)
        {
            return asyncAction().GetAwaiter().GetResult();
        }

        public void VerifyOnUIThread()
        {
            if (!IsOnMainThread)
            {
                throw new InvalidOperationException();
            }
        }

        public void Fork(Func<Task> asyncAction,
                  JoinableTaskFactory factory = null,
                  UnconfiguredProject unconfiguredProject = null,
                  ConfiguredProject configuredProject = null,
                  ErrorReportSettings watsonReportSettings = null,
                  ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable,
                  ForkOptions options = ForkOptions.Default)
        {
            Task.Run(asyncAction).Wait();
        }

        public JoinableTaskContextNode JoinableTaskContext { get; private set; } = new JoinableTaskContextNode(
            new JoinableTaskContext(DispatchThread.Thread, DispatchThread.SyncContext));

        public JoinableTaskFactory JoinableTaskFactory
        {
            get
            {
                return JoinableTaskContext.Factory;
            }
        }

        public bool IsOnMainThread
        {
            get
            {
                return System.Threading.Thread.CurrentThread == DispatchThread.Thread;
            }
        }

        public IDisposable SuppressProjectExecutionContext()
        {
            throw new NotImplementedException();
        }
    }
}
