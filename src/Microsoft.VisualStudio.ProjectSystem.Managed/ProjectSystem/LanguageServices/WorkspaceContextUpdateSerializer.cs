// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IWorkspaceContextUpdateSerializer))]
    internal sealed class WorkspaceContextUpdateSerializer : IWorkspaceContextUpdateSerializer, IDisposable
    {
        private readonly SequentialTaskExecutor _sequentialTaskExecutor;

        [ImportingConstructor]
        public WorkspaceContextUpdateSerializer(IProjectThreadingService threadingService)
        {
            _sequentialTaskExecutor = new SequentialTaskExecutor(threadingService.JoinableTaskContext, nameof(WorkspaceContextUpdateSerializer));
        }

        public Task ApplyUpdateAsync(Func<Task> updateFunc)
        {
            return _sequentialTaskExecutor.ExecuteTask(updateFunc);
        }

        public void Dispose()
        {
            _sequentialTaskExecutor.Dispose();
        }
    }
}
