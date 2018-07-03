// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class AbstractProjectDynamicLoadComponentFactory
    {
        public static ProjectDynamicLoadComponent Create()
        {
            var joinableTaskContextNode = JoinableTaskContextNodeFactory.Create();

            return new ProjectDynamicLoadComponent(joinableTaskContextNode);
        }

        public class ProjectDynamicLoadComponent : AbstractProjectDynamicLoadComponent
        {
            private readonly JoinableTaskContextNode _joinableTaskContextNode;

            public ProjectDynamicLoadComponent(JoinableTaskContextNode joinableTaskContextNode) 
                : base(joinableTaskContextNode)
            {
                _joinableTaskContextNode = joinableTaskContextNode;
            }

            protected override AbstractProjectDynamicLoadInstance CreateInstance()
            {
                return new ProjectDynamicLoadComponentInstance(_joinableTaskContextNode);
            }

            public new bool IsInitialized
            {
                get { return base.IsInitialized; }
            }

            public class ProjectDynamicLoadComponentInstance : AbstractProjectDynamicLoadInstance
            {
                public ProjectDynamicLoadComponentInstance(JoinableTaskContextNode joinableTaskContextNode) 
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
