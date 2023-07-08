// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class LaunchSettingsProviderExtensions
    {
        /// <summary>
        /// Blocks until at least one snapshot has been generated.
        /// </summary>
        /// <param name="provider">The underlying provider to satisfy this request.</param>
        /// <param name="token">An optional token to signal cancellation of the request.</param>
        /// <returns>
        /// The current <see cref="ILaunchSettings"/> snapshot.
        /// </returns>
        public static async Task<ILaunchSettings> WaitForFirstSnapshot(this ILaunchSettingsProvider provider, CancellationToken token = default)
        {
            // With an infinite timeout, the provider is contractually obligated to return a non-null value.
            Task<ILaunchSettings?> task = provider.WaitForFirstSnapshot(Timeout.Infinite);

            if (token.CanBeCanceled)
            {
                task = task.WithCancellation(token);
            }

            ILaunchSettings? launchSettings = await task;

            Assumes.NotNull(launchSettings);

            return launchSettings;
        }
    }
}
