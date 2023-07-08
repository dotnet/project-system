// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class AbstractMultiLifetimeComponentFactory
    {
        public static MultiLifetimeComponent Create()
        {
            var joinableTaskContextNode = JoinableTaskContextNodeFactory.Create();

            return new MultiLifetimeComponent(joinableTaskContextNode);
        }

        public class MultiLifetimeComponent : AbstractMultiLifetimeComponent<MultiLifetimeComponent.MultiLifetimeInstance>
        {
            public MultiLifetimeComponent(JoinableTaskContextNode joinableTaskContextNode)
                : base(joinableTaskContextNode)
            {
            }

            protected override MultiLifetimeInstance CreateInstance()
            {
                return new MultiLifetimeInstance();
            }

            public new bool IsInitialized
            {
                get { return base.IsInitialized; }
            }

            public new Task<MultiLifetimeInstance> WaitForLoadedAsync(CancellationToken cancellationToken = default)
            {
                return base.WaitForLoadedAsync(cancellationToken);
            }

            public class MultiLifetimeInstance : IMultiLifetimeInstance
            {
                public bool IsInitialized { get; private set; }

                public bool IsDisposed { get; private set; }

                public Task InitializeAsync()
                {
                    IsInitialized = true;

                    return Task.CompletedTask;
                }

                public Task DisposeAsync()
                {
                    IsDisposed = true;

                    return Task.CompletedTask;
                }
            }
        }
    }
}
