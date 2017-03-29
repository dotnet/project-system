// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    ///     Provides publishable project config for projects that support click once publishing.
    /// </summary>
    [Export(typeof(IPublishProvider))]
    [AppliesTo(ProjectCapability.Managed)]
    internal class PublishableProjectConfigProvider : IPublishProvider
    {
        public Task<bool> IsPublishSupportedAsync()
        {
            // No support for ClickOnce publishing for now.
            return Task.FromResult(false);
        }

        public Task PublishAsync(CancellationToken cancellationToken, TextWriter outputPaneWriter)
        {
            // No-op for now.
            return Task.CompletedTask;
        }

        public Task<bool> ShowPublishPromptAsync()
        {
            return Task.FromResult(false);
        }
    }
}
