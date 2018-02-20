// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    partial class AbstractProjectDynamicLoadComponent
    {
        /// <summary>
        ///     Represents an instance that is automatically initialized when its parent
        ///     <see cref="IProjectDynamicLoadComponent"/> instance's capabilities requirements are 
        ///     satisfied, or disposed when they are not.
        /// </summary>
        protected abstract class AbstractProjectDynamicLoadInstance : OnceInitializedOnceDisposedAsync
        {
            protected AbstractProjectDynamicLoadInstance(JoinableTaskContextNode joinableTaskContextNode) 
                : base(joinableTaskContextNode)
            {
            }

            /// <summary>
            ///     Initializes the <see cref="AbstractProjectDynamicLoadInstance"/>.
            /// </summary>
            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }
        }
    }
}
