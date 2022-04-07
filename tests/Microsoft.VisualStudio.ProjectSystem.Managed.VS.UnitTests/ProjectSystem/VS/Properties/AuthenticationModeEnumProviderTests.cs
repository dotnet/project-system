// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class AuthenticationModeEnumProviderTests
    {
        [Fact]
        public async Task GetProviderAsync_ReturnsNonNullGenerator()
        {
            var remoteDebuggerAuthenticationService = IRemoteDebuggerAuthenticationServiceFactory.Create();

            var provider = new AuthenticationModeEnumProvider(remoteDebuggerAuthenticationService);
            var generator = await provider.GetProviderAsync(options: null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task TryCreateEnumValueAsync_ReturnsNull()
        {
            var remoteDebuggerAuthenticationService = IRemoteDebuggerAuthenticationServiceFactory.Create();

            var provider = new AuthenticationModeEnumProvider(remoteDebuggerAuthenticationService);
            var generator = await provider.GetProviderAsync(options: null);

            Assert.Null(await generator.TryCreateEnumValueAsync("MyMode"));
        }

        [Fact]
        public async Task GetListValuesAsync_ReturnsPageNamesAndDisplayNames()
        {
            var remoteDebuggerAuthenticationService = IRemoteDebuggerAuthenticationServiceFactory.Create(
                IRemoteAuthenticationProviderFactory.Create("cheetah", "Fast authentication!"),
                IRemoteAuthenticationProviderFactory.Create("tortoise", "Sloooow authentication..."));

            var provider = new AuthenticationModeEnumProvider(remoteDebuggerAuthenticationService);
            var generator = await provider.GetProviderAsync(options: null);

            var values = await generator.GetListedValuesAsync();

            Assert.Collection(values, new Action<IEnumValue>[]
            {
                ev => { Assert.Equal(expected: "cheetah", actual: ev.Name); Assert.Equal(expected: "Fast authentication!", actual: ev.DisplayName); },
                ev => { Assert.Equal(expected: "tortoise", actual: ev.Name); Assert.Equal(expected: "Sloooow authentication...", actual: ev.DisplayName); }
            });
        }
    }
}
