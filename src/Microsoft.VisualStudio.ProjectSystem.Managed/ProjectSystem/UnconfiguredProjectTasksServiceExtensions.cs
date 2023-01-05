// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem;

internal static class UnconfiguredProjectTasksServiceExtensions
{
    public static CancellationToken LinkUnload(this IUnconfiguredProjectTasksService service, CancellationToken token)
    {
        return CancellationTokenExtensions.CombineWith(token, service.UnloadCancellationToken).Token;
    }
}
