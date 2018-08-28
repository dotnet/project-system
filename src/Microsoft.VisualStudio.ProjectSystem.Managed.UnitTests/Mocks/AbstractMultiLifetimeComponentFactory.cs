// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class AbstractMultiLifetimeComponentFactory
    {
        public static MultiLifetimeComponent Create()
        {
            var joinableTaskContextNode = JoinableTaskContextNodeFactory.Create();

            return new MultiLifetimeComponent(joinableTaskContextNode);
        }

        public class MultiLifetimeComponent : AbstractMultiLifetimeComponent
        {
            private readonly JoinableTaskContextNode _joinableTaskContextNode;

            public MultiLifetimeComponent(JoinableTaskContextNode joinableTaskContextNode) 
                : base(joinableTaskContextNode)
            {
                _joinableTaskContextNode = joinableTaskContextNode;
            }

            protected override IMultiLifetimeInstance CreateInstance()
            {
                return new MultiLifetimeInstance();
            }

            public new bool IsInitialized
            {
                get { return base.IsInitialized; }
            }

            public class MultiLifetimeInstance : IMultiLifetimeInstance
            {
                public bool IsInitialized
                {
                    get;
                    private set;
                }

                public bool IsDisposed
                {
                    get;
                    private set;
                }

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
