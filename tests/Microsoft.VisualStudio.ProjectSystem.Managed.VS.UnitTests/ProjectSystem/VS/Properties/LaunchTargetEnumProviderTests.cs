// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class LaunchTargetEnumProviderTests
    {
        [Fact]
        public async Task GetProviderAsync_ReturnsNonNullGenerator()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchTargetEnumProvider(project, GetJoinableTaskContext());
            var generator = await provider.GetProviderAsync(options: null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task TryCreateEnumValueAsync_Throws()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchTargetEnumProvider(project, GetJoinableTaskContext());
            var generator = await provider.GetProviderAsync(options: null);

            await Assert.ThrowsAsync<NotImplementedException>(() => generator.TryCreateEnumValueAsync("MyTarget"));
        }

        [Fact]
        public async Task GetListValuesAsync_ReturnsCommandsAndFriendlyNames()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchTargetEnumProvider(project, GetJoinableTaskContext());
            provider.UIProviders.Add(ILaunchSettingsUIProviderFactory.Create(commandName: "alpha", friendlyName: "Command One"));
            provider.UIProviders.Add(ILaunchSettingsUIProviderFactory.Create(commandName: "beta", friendlyName: "Command Two"));
            var generator = await provider.GetProviderAsync(options: null);

            var values = await generator.GetListedValuesAsync();

            Assert.Collection(values, new Action<IEnumValue>[]
            {
                ev => { Assert.Equal(expected: "alpha", actual: ev.Name); Assert.Equal(expected: "Command One", actual: ev.DisplayName); },
                ev => {Assert.Equal(expected: "beta", actual: ev.Name); Assert.Equal(expected: "Command Two", actual: ev.DisplayName); }
            });
        }

        [Fact]
        public async Task GetListValuesAsync_ItemsWithDuplicatedCommandsAreFilteredOut()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchTargetEnumProvider(project, GetJoinableTaskContext());
            provider.UIProviders.Add(ILaunchSettingsUIProviderFactory.Create(commandName: "alpha", friendlyName: "Command One"));
            provider.UIProviders.Add(ILaunchSettingsUIProviderFactory.Create(commandName: "beta", friendlyName: "Command Two"));
            provider.UIProviders.Add(ILaunchSettingsUIProviderFactory.Create(commandName: "alpha", friendlyName: "Duplicate"));
            var generator = await provider.GetProviderAsync(options: null);

            var values = await generator.GetListedValuesAsync();

            Assert.Collection(values, new Action<IEnumValue>[]
            {
                ev => { Assert.Equal(expected: "alpha", actual: ev.Name); Assert.Equal(expected: "Command One", actual: ev.DisplayName); },
                ev => {Assert.Equal(expected: "beta", actual: ev.Name); Assert.Equal(expected: "Command Two", actual: ev.DisplayName); }
            });
        }

        private static JoinableTaskContext GetJoinableTaskContext()
        {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            return new Threading.JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
        }
    }
}
