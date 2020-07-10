// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    public class PublishableProjectConfigProviderTests
    {
        [Fact]
        public async Task IsPublishSupportedAsync_ReturnsFalse()
        {
            var provider = CreateInstance();

            var result = await provider.IsPublishSupportedAsync();

            Assert.False(result);
        }

        [Fact]
        public async Task PublishAsync_ThrowsInvalidOperation()
        {
            var provider = CreateInstance();
            var writer = new StringWriter();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return provider.PublishAsync(CancellationToken.None, writer);
            });
        }

        [Fact]
        public async Task ShowPublishPromptAsync_ThrowsInvalidOperation()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return provider.ShowPublishPromptAsync();
            });
        }

        private static PublishableProjectConfigProvider CreateInstance()
        {
            return new PublishableProjectConfigProvider();
        }
    }
}
