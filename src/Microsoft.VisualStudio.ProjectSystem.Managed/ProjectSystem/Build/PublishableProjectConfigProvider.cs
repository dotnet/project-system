// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    ///     Provides publishable project config for projects that support click once publishing.
    /// </summary>
    [Export(typeof(IPublishProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class PublishableProjectConfigProvider : IPublishProvider
    {
        public Task<bool> IsPublishSupportedAsync()
        {
            // No support for ClickOnce publishing for now.
            return TaskResult.False;
        }

        public Task PublishAsync(CancellationToken cancellationToken, TextWriter outputPaneWriter)
        {
            throw new InvalidOperationException();
        }

        public Task<bool> ShowPublishPromptAsync()
        {
            throw new InvalidOperationException();
        }
    }
}
