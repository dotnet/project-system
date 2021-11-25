// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class NullCommand : IProjectSystemUpdateReferenceOperation
    {
        public Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            return TaskResult.False;
        }

        public Task<bool> RevertAsync(CancellationToken cancellationToken)
        {
            return TaskResult.False;
        }
    }
}
