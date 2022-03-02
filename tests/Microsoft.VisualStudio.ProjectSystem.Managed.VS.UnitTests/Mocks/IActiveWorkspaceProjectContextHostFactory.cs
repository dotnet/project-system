// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IActiveWorkspaceProjectContextHostFactory
    {
        public static IActiveWorkspaceProjectContextHost Create()
        {
            return Mock.Of<IActiveWorkspaceProjectContextHost>();
        }

        public static IActiveWorkspaceProjectContextHost ImplementContextId(string contextId)
        {
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementContextId(contextId);

            return new ActiveWorkspaceProjectContextHost(accessor);
        }

        public static IActiveWorkspaceProjectContextHost ImplementHostSpecificErrorReporter(Func<object> action)
        {
            var accessor = IWorkspaceProjectContextAccessorFactory.ImplementHostSpecificErrorReporter(action);

            return new ActiveWorkspaceProjectContextHost(accessor);
        }

        public static IActiveWorkspaceProjectContextHost ImplementProjectContextAccessor(IWorkspaceProjectContextAccessor accessor)
        {
            return new ActiveWorkspaceProjectContextHost(accessor);
        }

        private class ActiveWorkspaceProjectContextHost : IActiveWorkspaceProjectContextHost
        {
            private readonly IWorkspaceProjectContextAccessor _accessor;

            public ActiveWorkspaceProjectContextHost(IWorkspaceProjectContextAccessor accessor)
            {
                _accessor = accessor;
            }

            public Task PublishAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
            {
                return action(_accessor);
            }

            public Task<T?> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
            {
                return action(_accessor)!;
            }
        }
    }
}
