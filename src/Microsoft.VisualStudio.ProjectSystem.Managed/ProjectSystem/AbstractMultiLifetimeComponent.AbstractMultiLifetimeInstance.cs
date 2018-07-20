// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class AbstractMultiLifetimeComponent
    {
        /// <summary>
        ///     Represents an instance that is automatically initialized when its parent <see cref="AbstractMultiLifetimeComponent"/>
        ///     is loaded, or disposed when it is unloaded.
        /// </summary>
        public abstract class AbstractMultiLifetimeInstance : OnceInitializedOnceDisposedAsync
        {
            protected AbstractMultiLifetimeInstance(JoinableTaskContextNode joinableTaskContextNode)
                : base(joinableTaskContextNode)
            {
            }

            /// <summary>
            ///     Initializes the <see cref="AbstractMultiLifetimeInstance"/>.
            /// </summary>
            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }
        }
    }
}
