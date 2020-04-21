// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    public class LaunchProfilesDebugLaunchProviderTests
    {
        private readonly Mock<IDebugProfileLaunchTargetsProvider> _mockWebProvider = new Mock<IDebugProfileLaunchTargetsProvider>();
        private readonly Mock<IDebugProfileLaunchTargetsProvider> _mockDockerProvider = new Mock<IDebugProfileLaunchTargetsProvider>();
        private readonly Mock<IDebugProfileLaunchTargetsProvider> _mockExeProvider = new Mock<IDebugProfileLaunchTargetsProvider>();
        private readonly OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> _launchProviders =
            new OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);
        private readonly Mock<ConfiguredProject> _configuredProjectMoq = new Mock<ConfiguredProject>();
        private readonly Mock<ILaunchSettingsProvider> _LaunchSettingsProviderMoq = new Mock<ILaunchSettingsProvider>();
        private readonly List<IDebugLaunchSettings> _webProviderSettings = new List<IDebugLaunchSettings>();
        private readonly List<IDebugLaunchSettings> _dockerProviderSettings = new List<IDebugLaunchSettings>();
        private readonly List<IDebugLaunchSettings> _exeProviderSettings = new List<IDebugLaunchSettings>();

        // Set this to have ILaunchSettingsProvider return this profile (null by default)
        private ILaunchProfile? _activeProfile;

        public LaunchProfilesDebugLaunchProviderTests()
        {
            _mockWebProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => p.CommandName == "IISExpress");
            _mockWebProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => { return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_webProviderSettings); });
            _mockDockerProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => p.CommandName == "Docker");
            _mockDockerProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => { return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_dockerProviderSettings); });
            _mockExeProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => string.IsNullOrEmpty(p.CommandName) || p.CommandName == "Project");
            _mockExeProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => { return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_exeProviderSettings); });

            var mockMetadata = new Mock<IOrderPrecedenceMetadataView>();
            _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockWebProvider.Object, mockMetadata.Object));
            _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockDockerProvider.Object, mockMetadata.Object));
            _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockExeProvider.Object, mockMetadata.Object));

            _LaunchSettingsProviderMoq.Setup(x => x.ActiveProfile).Returns(() => _activeProfile);
            _LaunchSettingsProviderMoq.Setup(x => x.WaitForFirstSnapshot(It.IsAny<int>())).Returns(() =>
            {
                if (_activeProfile != null)
                {
                    return Task.FromResult((ILaunchSettings)new LaunchSettings(new List<ILaunchProfile>() { _activeProfile }, null, _activeProfile.Name));
                }

                return Task.FromResult((ILaunchSettings)new LaunchSettings());
            });
        }

        [Fact]
        public async Task CanLaunchAsyncTests()
        {
            var provider = CreateInstance();

            bool result = await provider.CanLaunchAsync(DebugLaunchOptions.NoDebug);
            Assert.True(result);
            result = await provider.CanLaunchAsync(0);
            Assert.True(result);
        }

        [Fact]
        public void GetLaunchTargetsProviderForProfileTests()
        {
            var provider = CreateInstance();
            Assert.Equal(_mockWebProvider.Object, provider.GetLaunchTargetsProvider(new LaunchProfile() { Name = "test", CommandName = "IISExpress" }));
            Assert.Equal(_mockDockerProvider.Object, provider.GetLaunchTargetsProvider(new LaunchProfile() { Name = "test", CommandName = "Docker" }));
            Assert.Equal(_mockExeProvider.Object, provider.GetLaunchTargetsProvider(new LaunchProfile() { Name = "test", CommandName = "Project" }));
            Assert.Null(provider.GetLaunchTargetsProvider(new LaunchProfile() { Name = "test", CommandName = "IIS" }));
        }

        [Fact]
        public async Task QueryDebugTargetsAsyncCorrectProvider()
        {
            var provider = CreateInstance();

            _activeProfile = new LaunchProfile() { Name = "test", CommandName = "IISExpress" };
            var result = await provider.QueryDebugTargetsAsync(0);
            Assert.Equal(_webProviderSettings, result);

            _activeProfile = new LaunchProfile() { Name = "test", CommandName = "Docker" };
            result = await provider.QueryDebugTargetsAsync(0);
            Assert.Equal(_dockerProviderSettings, result);

            _activeProfile = new LaunchProfile() { Name = "test", CommandName = "Project" };
            result = await provider.QueryDebugTargetsAsync(0);
            Assert.Equal(_exeProviderSettings, result);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_WhenNoLaunchProfile_Throws()
        {
            var provider = CreateInstance();
            _activeProfile = null;

            await Assert.ThrowsAsync<Exception>(() =>
            {
                return provider.QueryDebugTargetsAsync(0);
            });
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_WhenNoInstalledProvider_Throws()
        {
            var provider = CreateInstance();
            _activeProfile = new LaunchProfile() { Name = "NoActionProfile", CommandName = "SomeOtherExtension" };

            await Assert.ThrowsAsync<Exception>(() =>
            {
                return provider.QueryDebugTargetsAsync(0);
            });
        }

        private LaunchProfilesDebugLaunchProvider CreateInstance()
        {
            var provider = new LaunchProfilesDebugLaunchProvider(_configuredProjectMoq.Object, _LaunchSettingsProviderMoq.Object, vsDebuggerService: null!);

            provider.LaunchTargetsProviders.Add(_mockWebProvider.Object);
            provider.LaunchTargetsProviders.Add(_mockDockerProvider.Object);
            provider.LaunchTargetsProviders.Add(_mockExeProvider.Object);

            return provider;
        }
    }
}
