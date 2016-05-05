// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectThreadingServiceFactory
    {
        public static IProjectThreadingService Create()
        {
            return new MockProjectThreadingService();
        }

        private class MockProjectThreadingService : IProjectThreadingService
        {
            private readonly JoinableTaskContextNode _context;
            private readonly JoinableTaskFactory _joinableTaskFactory;

            public MockProjectThreadingService()
            {
                _context = new JoinableTaskContextNode(new JoinableTaskContext());
                _joinableTaskFactory = _context.Factory;
            }

            public bool IsOnMainThread
            {
                get { return Thread.CurrentThread == _context.MainThread; }
            }

            public JoinableTaskContextNode JoinableTaskContext
            {
                get { return _context; }
            }

            public JoinableTaskFactory JoinableTaskFactory
            {
                get { return _joinableTaskFactory; }
            }

            public JoinableTaskFactory.MainThreadAwaitable SwitchToUIThread(StrongBox<bool> yielded)
            {
                return JoinableTaskContext.SwitchToMainThreadAsync();
            }

            public void ExecuteSynchronously(Func<Task> asyncAction)
            {
                var task = asyncAction();
                task.GetAwaiter().GetResult();
            }

            public T ExecuteSynchronously<T>(Func<Task<T>> asyncAction)
            {
                var task = asyncAction();
                return task.GetAwaiter().GetResult();
            }

            public void VerifyOnUIThread()
            {
                throw new NotImplementedException();
            }

            public void Fork(Func<Task> asyncAction, JoinableTaskFactory factory = null, UnconfiguredProject unconfiguredProject = null, ConfiguredProject configuredProject = null, ErrorReportSettings watsonReportSettings = null, ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable, ForkOptions options = ForkOptions.Default)
            {
                throw new NotImplementedException();
            }

            public IDisposable SuppressProjectExecutionContext()
            {
                throw new NotImplementedException();
            }
        }
    }
}
