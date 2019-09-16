// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
