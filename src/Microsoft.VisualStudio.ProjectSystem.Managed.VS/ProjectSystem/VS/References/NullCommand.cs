// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    class NullCommand : IProjectSystemUpdateReferenceOperation
    {
        public Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> RevertAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
