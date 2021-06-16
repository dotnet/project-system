// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    public interface IProjectHotReloadSession
    {
        Task StartSessionAsync(CancellationToken cancellationToken);

        Task StopSessionAsync(CancellationToken cancellationToken);

        Task ApplyChangesAsync(CancellationToken cancellationToken);

        Task<bool> ApplyLaunchVariablesAsync(IDictionary<string, string> envVars, CancellationToken cancellationToken);
    }
}
