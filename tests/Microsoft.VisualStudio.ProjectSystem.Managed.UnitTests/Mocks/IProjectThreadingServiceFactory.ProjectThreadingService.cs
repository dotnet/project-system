// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class IProjectThreadingServiceFactory
    {
        private class ProjectThreadingService(bool verifyOnUIThread = true) : IProjectThreadingService
        {
            public JoinableTaskContextNode JoinableTaskContext { get; } = new JoinableTaskContextNode(new JoinableTaskContext());

            public JoinableTaskFactory JoinableTaskFactory => JoinableTaskContext.Factory;

            public bool IsOnMainThread => !verifyOnUIThread || JoinableTaskContext.IsOnMainThread;

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
                if (verifyOnUIThread && !IsOnMainThread)
                {
                    throw new InvalidOperationException();
                }
            }

            public IDisposable SuppressProjectExecutionContext()
            {
                return DisposableObject.Instance;
            }

            public void Fork(
                Func<Task> asyncAction,
                JoinableTaskFactory? factory = null,
                UnconfiguredProject? project = null,
                ConfiguredProject? configuredProject = null,
                ErrorReportSettings? watsonReportSettings = null,
                ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable,
                ForkOptions options = ForkOptions.Default)
            {
                JoinableTaskFactory.Run(asyncAction);
            }

            private class DisposableObject : IDisposable
            {
                public static IDisposable Instance { get; } = new DisposableObject();

                public void Dispose()
                {
                }
            }
        }
    }
}
