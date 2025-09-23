// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal interface IDeltaApplierInternal : IDeltaApplier
{
    /// <summary>
    /// Initiates connection to the agent in the application.
    /// Called before the process is started.
    /// </summary>
    ValueTask InitiateConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Initializes the application process after it has started.
    /// </summary>
    ValueTask InitializeApplicationAsync(CancellationToken cancellationToken);
}
