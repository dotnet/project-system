// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

internal static class IWorkspaceWriterFactory
{
    public static IWorkspaceWriter Create()
    {
        return Mock.Of<IWorkspaceWriter>();
    }

    public static IWorkspaceWriter ImplementProjectContextAccessor(IWorkspace workspace)
    {
        return new WorkspaceWriter(workspace);
    }

    private class WorkspaceWriter(IWorkspace workspace) : IWorkspaceWriter
    {
        public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        {
            return TaskResult.True;
        }

        public Task WhenInitialized(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task WriteAsync(Func<IWorkspace, Task> action, CancellationToken cancellationToken = default)
        {
            return action(workspace);
        }

        public Task<T> WriteAsync<T>(Func<IWorkspace, Task<T>> func, CancellationToken cancellationToken = default)
        {
            return func(workspace)!;
        }
    }
}
