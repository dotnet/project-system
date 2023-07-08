// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Notifications;

/// <summary>
///     Provides methods for displaying non-blocking notifications to the user.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface INonModalNotificationService
{
    /// <summary>
    /// Displays a message to the user.
    /// </summary>
    Task ShowMessageAsync(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Displays a warning to the user.
    /// </summary>
    Task ShowWarningAsync(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Displays an error to the user.
    /// </summary>
    Task ShowErrorAsync(string message, CancellationToken cancellationToken);
}
