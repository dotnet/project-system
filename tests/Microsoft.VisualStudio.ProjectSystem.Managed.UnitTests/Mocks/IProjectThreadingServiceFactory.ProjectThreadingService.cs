// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class IProjectThreadingServiceFactory
    {
        private class ProjectThreadingService : IProjectThreadingService
        {
            private readonly bool _verifyOnUIThread;

            public ProjectThreadingService(bool verifyOnUIThread = true) => _verifyOnUIThread = verifyOnUIThread;

            public JoinableTaskContextNode JoinableTaskContext { get; } = new JoinableTaskContextNode(new JoinableTaskContext());

            public JoinableTaskFactory JoinableTaskFactory
            {
                get { return JoinableTaskContext.Factory; }
            }

            public bool IsOnMainThread
            {
                get
                {
                    if (!_verifyOnUIThread)
                        return true;

                    return JoinableTaskContext.IsOnMainThread;
                }
            }

            public void ExecuteSynchronously(Func<Task> asyncAction)
            {
                JoinableTaskFactory.Run(asyncAction);
            }

            public T ExecuteSynchronously<T>(Func<Task<T>> asyncAction)
            {
                return JoinableTaskFactory.Run(asyncAction);
            }

            public void VerifyOnUIThread()
            {
                if (!_verifyOnUIThread)
                    return;

                if (!IsOnMainThread)
                    throw new InvalidOperationException();
            }

            public IDisposable SuppressProjectExecutionContext()
            {
                throw new NotImplementedException();
            }

            public void Fork(Func<Task> asyncAction,
                      JoinableTaskFactory? factory = null,
                      UnconfiguredProject? project = null,
                      ConfiguredProject? configuredProject = null,
                      ErrorReportSettings? watsonReportSettings = null,
                      ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable,
                      ForkOptions options = ForkOptions.Default)
            {
                JoinableTaskFactory.Run(asyncAction);
            }
        }
    }
}
