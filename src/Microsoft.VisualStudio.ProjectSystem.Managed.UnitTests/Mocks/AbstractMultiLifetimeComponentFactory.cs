// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
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

            protected override AbstractMultiLifetimeInstance CreateInstance()
            {
                return new MultiLifetimeInstance(_joinableTaskContextNode);
            }

            public new bool IsInitialized
            {
                get { return base.IsInitialized; }
            }

            public class MultiLifetimeInstance : AbstractMultiLifetimeInstance
            {
                public MultiLifetimeInstance(JoinableTaskContextNode joinableTaskContextNode) 
                    : base(joinableTaskContextNode)
                {
                }

                protected override Task DisposeCoreAsync(bool initialized)
                {
                    return Task.CompletedTask;
                }

                protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
                {
                    return Task.CompletedTask;
                }

                public new bool IsInitialized
                {
                    get { return base.IsInitialized; }
                }
            }
        }
    }

}
